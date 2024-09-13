using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DeepCopyLibrary;

/// <summary>
/// Performs a multi-step copy operation between two connections, allowing for stopping and resuming.
/// </summary>
public abstract class RemoteDeepCopy<TKey, TInputParams, TOutput>(
	IKeyMapRepository<TKey> keyMapRepository,
	IErrorRepository errorRepository,
	IMetricsRepository metricsRepository,
	ILogger<RemoteDeepCopy<TKey, TInputParams, TOutput>> logger)
	where TInputParams : new()
	where TKey : notnull
{
	private readonly IKeyMapRepository<TKey> _keyMap = keyMapRepository;
	private readonly IErrorRepository _errors = errorRepository;
	private readonly IMetricsRepository _metrics = metricsRepository;
	private readonly ILogger<RemoteDeepCopy<TKey, TInputParams, TOutput>> _logger = logger;

	public async Task<TOutput> ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken) =>
		await OnExecuteAsync(sourceConnection, destConnection, parameters, cancellationToken);

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>
	protected abstract Task<TOutput> OnExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken);

	/// <summary>
	/// defines an individual copy step as part of a larger operation
	/// </summary>   
	protected abstract class Step<TEntity>(
		IKeyMapRepository<TKey> keyMap,
		IErrorRepository errorRepository,
		IMetricsRepository metricsRepository,
		ILogger<Step<TEntity>> logger) where TEntity : new()
	{
		protected readonly IKeyMapRepository<TKey> KeyMap = keyMap;		
		protected readonly ILogger<Step<TEntity>> Logger = logger;

		protected readonly IErrorRepository _errors = errorRepository;
		private readonly IMetricsRepository _metrics = metricsRepository;

		protected abstract string Name { get; }
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection sourceConnection, TInputParams parameters);
		protected abstract TEntity CreateNewRow(TInputParams parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection destConnection, TEntity entity, TInputParams parameters);		

		public async Task ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested) return;

			var sw = Stopwatch.StartNew();			
			int successRows = 0;
			int errorRows = 0;
			int skippedRows = 0;

			try
			{
				Logger.LogDebug("Querying source rows for step {StepName}", Name);
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
							Logger.LogDebug("Skipping row with key {Key} for step {StepName}", sourceKey, Name);
							continue;
						}

						var newRow = CreateNewRow(parameters, sourceRow);
						try
						{
							var newKey = await InsertNewRowAsync(destConnection, newRow, parameters);
							successRows++;
							await KeyMap.AddAsync(Name, sourceKey, newKey);							
						}
						catch (Exception exc)
						{
							errorRows++;
							Logger.LogError(exc, "Error inserting row for step {StepName} source key {key}", Name, sourceKey);
							await _errors.WhenInsertingAsync(Name, exc, sourceRow, newRow);							
						}
					}
				}
				catch (Exception exc)
				{
					await _errors.WhenLoopingAsync(Name, exc);
				}
				finally
				{
					sw.Stop();
					await _metrics.LogAsync(Name, successRows, errorRows, skippedRows, sw.Elapsed);
				}
			}
			catch (Exception exc)
			{
				await _errors.WhenQueryingAsync(Name, exc);
			}
		}
	}
}
