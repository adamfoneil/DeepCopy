using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DeepCopy.Abstractions;

/// <summary>
/// Performs a multi-step data copy operation within a single connection and transaction.
/// Intended for short, low-volume operations that rollback on error rather than support resuming.
/// </summary>
public abstract class LocalDeepCopy<TKey, TInputParams>(ILogger<LocalDeepCopy<TKey, TInputParams>> logger)
	where TInputParams : new()
	where TKey : notnull
{
	protected Dictionary<(string, TKey), TKey> KeyMap = [];
	protected readonly ILogger<LocalDeepCopy<TKey, TInputParams>> Logger = logger;

	public async Task<TKey> ExecuteAsync(IDbConnection connection, TInputParams parameters)
	{
		if (connection.State != ConnectionState.Open) connection.Open();

		using var txn = connection.BeginTransaction();

		TKey result;

		result = await OnExecuteAsync(connection, txn, parameters);
		txn.Commit();

		return result;
	}

	protected async Task RunStepAsync<TStep, TEntity>(IDbConnection connection, IDbTransaction transaction, TInputParams parameters)
		where TStep : Step<TEntity>, new()
	{
		var step = new TStep()
		{
			KeyMap = KeyMap,
			Logger = Logger
		};
		await step.ExecuteAsync(connection, transaction, parameters);
	}

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>
	protected abstract Task<TKey> OnExecuteAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters);

	/// <summary>
	/// defines an individual copy step as part of a larger operation
	/// </summary>   
	protected abstract class Step<TEntity>
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		protected Step()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		{
		}

		public ILogger<LocalDeepCopy<TKey, TInputParams>> Logger { get; init; }
		public Dictionary<(string, TKey), TKey> KeyMap { get; init; }

		protected abstract string Name { get; }
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters);
		protected abstract TEntity CreateNewRow(TInputParams parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, TEntity entity, TInputParams parameters);
		protected virtual async Task OnStepCompletedAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters) => await Task.CompletedTask;

		public async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, TInputParams parameters)
		{
			Debug.Assert(Logger is not null);
			Debug.Assert(KeyMap is not null);

			Logger!.LogDebug("Querying source rows for step {StepName}", Name);
			var sourceRows = await QuerySourceRowsAsync(connection, transaction, parameters);

			foreach (var sourceRow in sourceRows)
			{
				var sourceKey = GetKey(sourceRow);
				var newRow = CreateNewRow(parameters, sourceRow);
				var newKey = await InsertNewRowAsync(connection, transaction, newRow, parameters);
				KeyMap![(Name, sourceKey)] = newKey;
			}

			await OnStepCompletedAsync(connection, transaction, parameters);
		}
	}
}
