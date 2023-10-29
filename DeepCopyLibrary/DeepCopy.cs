using System.Data;

namespace DeepCopyLibrary;

public abstract class DeepCopy<TKey, TInputParams, TOutput> where TInputParams : new()
{
	public Dictionary<(string, TKey), TKey> IdMap { get; private set; } = new();

	public async Task<TOutput> ExecuteAsync(IDbConnection connection, TInputParams parameters)
	{
		if (connection.State != ConnectionState.Open) connection.Open();

		using var txn = connection.BeginTransaction();

		TOutput result;

		try
		{
			result = await OnExecuteAsync(connection, txn, IdMap, parameters);
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
	protected abstract Task<TOutput> OnExecuteAsync(IDbConnection connection, IDbTransaction transaction, Dictionary<(string, TKey), TKey> idMap, TInputParams parameters);

	/// <summary>
	/// defines an individual copy step as part of a larger operation
	/// </summary>   
	protected abstract class Step<TEntity> where TEntity : new()
	{
		protected readonly Dictionary<(string, TKey), TKey> IdMap = new();

		public Step(Dictionary<(string, TKey), TKey> idMap)
		{
			IdMap = idMap;
		}

		protected abstract string Name { get; }
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters);
		protected abstract TEntity CreateNewRow(TInputParams parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, TEntity entity, TInputParams parameters);
		protected virtual async Task OnStepCompletedAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters) => await Task.CompletedTask;

		public async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters)
		{
			var sourceRows = await QuerySourceRowsAsync(connection, transaction, parameters);

			foreach (var sourceRow in sourceRows)
			{
				var newRow = CreateNewRow(parameters, sourceRow);
				var newKey = await InsertNewRowAsync(connection, transaction, newRow, parameters);
				IdMap.Add((Name, GetKey(sourceRow)), newKey);
			}

			await OnStepCompletedAsync(connection, transaction, parameters);
		}
	}
}
