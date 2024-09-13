using Microsoft.Extensions.Logging;
using System.Data;

namespace DeepCopyLibrary;

public abstract class RemoteDeepCopy<TKey, TInputParams, TOutput>(ILogger<RemoteDeepCopy<TKey, TInputParams, TOutput>> logger)
	where TInputParams : new()
	where TKey : notnull
{
	private IKeyMapRepository<TKey>? _keyMap;

	private readonly ILogger<RemoteDeepCopy<TKey, TInputParams, TOutput>> _logger = logger;

	public async Task<TOutput> ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken)
	{
		_keyMap = await LoadKeyMapAsync();		
		
		return await OnExecuteAsync(sourceConnection, destConnection, parameters, cancellationToken);
	}

	protected abstract Task<IKeyMapRepository<TKey>> LoadKeyMapAsync();

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>
	protected abstract Task<TOutput> OnExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken);

	/// <summary>
	/// defines an individual copy step as part of a larger operation
	/// </summary>   
	protected abstract class Step<TEntity>(
		IKeyMapRepository<TKey> keyMap,
		ILogger<Step<TEntity>> logger) where TEntity : new()
	{
		private readonly IKeyMapRepository<TKey> _keyMap = keyMap;
		private readonly ILogger<Step<TEntity>> _logger = logger;

		protected abstract string Name { get; }
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection sourceConnection, TInputParams parameters);
		protected abstract TEntity CreateNewRow(TInputParams parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection destConnection, TEntity entity, TInputParams parameters);
		protected virtual async Task OnStepCompletedAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters) => await Task.CompletedTask;

		public async Task ExecuteAsync(IDbConnection sourceConnection, IDbConnection destConnection, TInputParams parameters, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested) return;

			_logger.LogDebug("Querying source rows for step {StepName}", Name);
			var sourceRows = await QuerySourceRowsAsync(sourceConnection, parameters);

			foreach (var sourceRow in sourceRows)
			{
				if (cancellationToken.IsCancellationRequested) break;

				var sourceKey = GetKey(sourceRow);
				if (_keyMap.ContainsKey(Name, sourceKey))
				{
					_logger.LogDebug("Skipping row with key {Key} for step {StepName}", sourceKey, Name);
					continue;
				}

				var newRow = CreateNewRow(parameters, sourceRow);
				var newKey = await InsertNewRowAsync(destConnection, newRow, parameters);
				await _keyMap.AddAsync(Name, sourceKey, newKey);
			}

			await OnStepCompletedAsync(sourceConnection, destConnection, parameters);
		}
	}

}
