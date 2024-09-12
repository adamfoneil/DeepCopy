using Microsoft.Extensions.Logging;
using System.Data;

namespace DeepCopyLibrary;

public abstract class DeepCopy<TKey, TInputParams, TOutput>(ILogger<DeepCopy<TKey, TInputParams, TOutput>> logger) 
	where TInputParams : new() 
	where TKey : notnull
{
	private IKeyMapRepository<TKey>? _keyMap;

	private readonly ILogger<DeepCopy<TKey, TInputParams, TOutput>> _logger = logger;
	
	public async Task<TOutput> ExecuteAsync(IDbConnection connection, TInputParams parameters)
	{
		_keyMap = await LoadKeyMapAsync();

		if (connection.State != ConnectionState.Open) connection.Open();		

		using var txn = connection.BeginTransaction();

		TOutput result;

		try
		{
			result = await OnExecuteAsync(connection, txn, parameters);
			txn.Commit();
		}
		catch
		{
			txn.Rollback();
			throw;
		}

		return result;
	}

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>
	protected abstract Task<TOutput> OnExecuteAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters);

	protected abstract Task<IKeyMapRepository<TKey>> LoadKeyMapAsync();

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
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters);
		protected abstract TEntity CreateNewRow(TInputParams parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, TEntity entity, TInputParams parameters);
		protected virtual async Task OnStepCompletedAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters) => await Task.CompletedTask;

		public async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters)
		{
			_logger.LogDebug("Querying source rows for step {StepName}", Name);
			var sourceRows = await QuerySourceRowsAsync(connection, transaction, parameters);

			foreach (var sourceRow in sourceRows)
			{
				var sourceKey = GetKey(sourceRow);
				if (_keyMap.ContainsKey(Name, sourceKey))
				{
					_logger.LogDebug("Skipping row with key {Key} for step {StepName}", sourceKey, Name);
					continue;
				}

				var newRow = CreateNewRow(parameters, sourceRow);
				var newKey = await InsertNewRowAsync(connection, transaction, newRow, parameters);
				await _keyMap.AddAsync(Name, sourceKey, newKey, transaction);				
			}

			await OnStepCompletedAsync(connection, transaction, parameters);
		}
	}
}
