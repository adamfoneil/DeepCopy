using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DeepCopyLibrary;

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
public abstract class RemoteDeepCopy<TKey, TInputParams, TOutput>(
	IKeyMapRepository<TKey> keyMapRepository,	
	IMetricsRepository metricsRepository,
	ILogger<RemoteDeepCopy<TKey, TInputParams, TOutput>> logger)
	where TInputParams : new()
	where TKey : notnull
{
	private readonly IKeyMapRepository<TKey> _keyMap = keyMapRepository;
	private readonly IMetricsRepository _metrics = metricsRepository;
	private readonly ILogger<RemoteDeepCopy<TKey, TInputParams, TOutput>> _logger = logger;

	public abstract int MaxErrors { get; }

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>
	public abstract Task<TOutput> ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken);

	/// <summary>
	/// defines an individual copy step as part of a larger operation
	/// </summary>   
	protected abstract class Step<TEntity>(
		IKeyMapRepository<TKey> keyMap,
		IMetricsRepository metricsRepository,
		ILogger<Step<TEntity>> logger) where TEntity : new()
	{
		protected readonly IKeyMapRepository<TKey> KeyMap = keyMap;		
		protected readonly ILogger<Step<TEntity>> Logger = logger;
		
		private readonly IMetricsRepository _metrics = metricsRepository;

		protected abstract string Name { get; }
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection sourceConnection, TInputParams parameters);
		protected abstract TEntity CreateNewRow(TInputParams parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection destConnection, TEntity entity, TInputParams parameters);		

		public async Task ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested) return;

			using var logScope = Logger.BeginScope("Step {StepName}, input params {@parameters}", Name, parameters);

			const string logTemplate = "Error in {location}, source key {sourceKey}";

			var sw = Stopwatch.StartNew();			
			int successRows = 0;
			int createErrors = 0;
			int insertErrors = 0;			
			int skippedRows = 0;

			try
			{
				Logger.LogDebug("Querying...");
				var sourceRows = await QuerySourceRowsAsync(sourceConnection, parameters);

				try
				{ 
					foreach (var sourceRow in sourceRows)
					{
						if (cancellationToken.IsCancellationRequested) break;

						var sourceKey = GetKey(sourceRow);
						if (KeyMap.ContainsKey(Name, sourceKey))
						{
							skippedRows++;
							Logger.LogDebug("Skipping row with key {Key}", sourceKey);
							continue;
						}

						try
						{
							var newRow = CreateNewRow(parameters, sourceRow);

							try
							{
								var newKey = await InsertNewRowAsync(destConnection, newRow, parameters);
								successRows++;
								await KeyMap.AddAsync(Name, sourceKey, newKey);
							}
							catch (Exception exc)
							{
								insertErrors++;
								Logger.LogError(exc, logTemplate, ErrorLocation.Inserting, sourceKey);
							}
						}
						catch (Exception exc)
						{
							createErrors++;
							Logger.LogError(exc, logTemplate, ErrorLocation.Creating, sourceKey);
						}
						
					}
				}
				finally
				{
					sw.Stop();
					await _metrics.LogAsync(Name, successRows, insertErrors, skippedRows, sw.Elapsed);
				}
			}
			catch (Exception exc)
			{
				Logger.LogError(exc, logTemplate, ErrorLocation.Querying, default);
			}
		}
	}
}
