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
/// Performs a multi-step copy operation between two open connections, allowing for stopping and resuming.
/// </summary>
public abstract class ConnectedDeepCopy<TKey>(
	IKeyMapRepository<TKey> keyMapRepository,
	IMetricsRepository metricsRepository,
	ILogger<ConnectedDeepCopy<TKey>> logger)	
	where TKey : notnull
{
	protected readonly IKeyMapRepository<TKey> KeyMap = keyMapRepository;
	protected readonly IMetricsRepository Metrics = metricsRepository;
	protected readonly ILogger<ConnectedDeepCopy<TKey>> Logger = logger;

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>	
	public abstract Task<TKey> ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, CancellationToken cancellationToken);

    /// <summary>
    /// defines an individual copy step as part of a larger operation
    /// </summary>   
    protected abstract class Step<TEntity>(
        ConnectedDeepCopy<TKey> operation) where TEntity : new()
    {        
        private readonly ConnectedDeepCopy<TKey> _operation = operation;

        protected abstract string Name { get; }
        protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection sourceConnection, TKey parameter);
        protected abstract TEntity CreateNewRow(TEntity sourceRow);
        protected abstract TKey GetKey(TEntity sourceRow);
        protected abstract Task<TKey> InsertNewRowAsync(IDbConnection destConnection, TEntity entity);

        protected virtual int MaxErrors => 10;

        public async Task ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TKey parameter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            using var logScope = _operation.Logger.BeginScope("Step {StepName}", Name);

            const string logTemplate = "Error in {location}, source key {sourceKey}";

            var sw = Stopwatch.StartNew();
            int successRows = 0;
            int createErrors = 0;
            int insertErrors = 0;
            int skippedRows = 0;            

            try
            {
                _operation.Logger.LogDebug("Initializing...");
                await _operation.KeyMap.InitializeAsync();

                _operation.Logger.LogDebug("Querying...");
                var sourceRows = await QuerySourceRowsAsync(sourceConnection, parameter);

                try
                {
                    foreach (var sourceRow in sourceRows)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var sourceKey = GetKey(sourceRow);
                        if (_operation.KeyMap.ContainsKey(Name, sourceKey))
                        {
                            skippedRows++;
                            _operation.Logger.LogDebug("Skipping row with key {Key}", sourceKey);
                            continue;
                        }

                        try
                        {
                            var newRow = CreateNewRow(sourceRow);

                            try
                            {
                                var newKey = await InsertNewRowAsync(destConnection, newRow);
                                successRows++;
                                await _operation.KeyMap.AddAsync(Name, sourceKey, newKey);
                            }
                            catch (Exception exc)
                            {
                                insertErrors++;
                                _operation.Logger.LogError(exc, logTemplate, ErrorLocation.Inserting, sourceKey);
                            }
                        }
                        catch (Exception exc)
                        {
                            createErrors++;
                            _operation.Logger.LogError(exc, logTemplate, ErrorLocation.Creating, sourceKey);
                        }
                        if (createErrors + insertErrors >= MaxErrors)
                        {
                            _operation.Logger.LogWarning("Too many errors, stopping");
                            break;
                        }
                    }
                }
                finally
                {
                    sw.Stop();
                    await _operation.Metrics.LogAsync(Name, successRows, insertErrors, createErrors, skippedRows, sw.Elapsed);
                }
            }
            catch (Exception exc)
            {
                _operation.Logger.LogError(exc, logTemplate, ErrorLocation.Querying, default);
            }
        }
    }
}
