using Microsoft.Extensions.Logging;
using System.Data;

namespace DeepCopyLibrary;

/// <summary>
/// Performs a multi-step data copy operation within a single connection and transaction.
/// Intended for short, low-volume operations that rollback on error rather than support resuming.
/// </summary>
public abstract class LocalDeepCopy<TKey, TInputParams, TOutput>(ILogger<LocalDeepCopy<TKey, TInputParams, TOutput>> logger) 
	where TInputParams : new() 
	where TKey : notnull
{
	private Dictionary<(string, TKey), TKey> _keyMap = [];

	private readonly ILogger<LocalDeepCopy<TKey, TInputParams, TOutput>> _logger = logger;
	
	public async Task<TOutput> ExecuteAsync(IDbConnection connection, TInputParams parameters)
	{
		if (connection.State != ConnectionState.Open) connection.Open();		

		using var txn = connection.BeginTransaction();

		TOutput result;

		result = await OnExecuteAsync(connection, txn, parameters);
		txn.Commit();

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
		Dictionary<(string, TKey), TKey> keyMap, 
		ILogger<Step<TEntity>> logger) where TEntity : new()
	{
		private readonly ILogger<Step<TEntity>> _logger = logger;

		protected readonly Dictionary<(string, TKey), TKey> KeyMap = keyMap;
		
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
				var newRow = CreateNewRow(parameters, sourceRow);
				var newKey = await InsertNewRowAsync(connection, transaction, newRow, parameters);
				KeyMap[(Name, sourceKey)] = newKey;
			}

			await OnStepCompletedAsync(connection, transaction, parameters);
		}
	}
}
