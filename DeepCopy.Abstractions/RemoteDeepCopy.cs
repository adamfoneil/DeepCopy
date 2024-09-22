using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DeepCopy.Abstractions;

public enum ErrorLocation
{
	Querying,
	Looping,
	Creating,
	Inserting
}

/// <summary>
/// Performs a multi-step copy operation between two connections, allowing for stopping and resuming.
/// </summary>
public abstract class RemoteDeepCopy<TKey>(
	IKeyMapRepository<TKey> keyMapRepository,
	IMetricsRepository metricsRepository,
	ILogger<RemoteDeepCopy<TKey>> logger)	
	where TKey : notnull
{
	private readonly IKeyMapRepository<TKey> _keyMap = keyMapRepository;
	private readonly IMetricsRepository _metrics = metricsRepository;
	private readonly ILogger<RemoteDeepCopy<TKey>> _logger = logger;

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>	
	public abstract Task<TKey> ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, CancellationToken cancellationToken);

    /// <summary>
    /// defines an individual copy step as part of a larger operation
    /// </summary>   
    protected abstract class Step<TEntity>(
        IMetricsRepository metricsRepository,
        ILogger<RemoteDeepCopy<TKey>> logger,
        IKeyMapRepository<TKey> keyMap) where TEntity : new()
    {
        private readonly IKeyMapRepository<TKey> _keyMap = keyMap;
        private readonly ILogger<RemoteDeepCopy<TKey>> _logger = logger;

        private readonly IMetricsRepository _metrics = metricsRepository;

        protected abstract string Name { get; }
        protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection sourceConnection, TKey parameter);
        protected abstract TEntity CreateNewRow(TEntity sourceRow);
        protected abstract TKey GetKey(TEntity sourceRow);
        protected abstract Task<TKey> InsertNewRowAsync(IDbConnection destConnection, TEntity entity);

        protected virtual int MaxErrors => 10;

        public async Task ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TKey parameter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            using var logScope = _logger.BeginScope("Step {StepName}", Name);

            const string logTemplate = "Error in {location}, source key {sourceKey}";

            var sw = Stopwatch.StartNew();
            int successRows = 0;
            int createErrors = 0;
            int insertErrors = 0;
            int skippedRows = 0;            

            try
            {
                _logger.LogDebug("Initializing...");
                await _keyMap.InitializeAsync();

                _logger.LogDebug("Querying...");
                var sourceRows = await QuerySourceRowsAsync(sourceConnection, parameter);

                try
                {
                    foreach (var sourceRow in sourceRows)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var sourceKey = GetKey(sourceRow);
                        if (_keyMap.ContainsKey(Name, sourceKey))
                        {
                            skippedRows++;
                            _logger.LogDebug("Skipping row with key {Key}", sourceKey);
                            continue;
                        }

                        try
                        {
                            var newRow = CreateNewRow(sourceRow);

                            try
                            {
                                var newKey = await InsertNewRowAsync(destConnection, newRow);
                                successRows++;
                                await _keyMap.AddAsync(Name, sourceKey, newKey);
                            }
                            catch (Exception exc)
                            {
                                insertErrors++;
                                _logger.LogError(exc, logTemplate, ErrorLocation.Inserting, sourceKey);
                            }
                        }
                        catch (Exception exc)
                        {
                            createErrors++;
                            _logger.LogError(exc, logTemplate, ErrorLocation.Creating, sourceKey);
                        }
                        if (createErrors + insertErrors >= MaxErrors)
                        {
                            _logger.LogWarning("Too many errors, stopping");
                            break;
                        }
                    }
                }
                finally
                {
                    sw.Stop();
                    await _metrics.LogAsync(Name, successRows, insertErrors, createErrors, skippedRows, sw.Elapsed);
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, logTemplate, ErrorLocation.Querying, default);
            }
        }
    }
}
