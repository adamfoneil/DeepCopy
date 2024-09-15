using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DeepCopy.Abstractions;

/// <summary>
/// Performs a multi-step data copy operation within a single connection and transaction.
/// Intended for short, low-volume operations that rollback on error rather than support resuming.
/// </summary>
public abstract class LocalDeepCopy<TKey>(ILogger<LocalDeepCopy<TKey>> logger)
	where TKey : notnull
{
	protected readonly Dictionary<(string, TKey), TKey> KeyMap = [];
	protected readonly ILogger<LocalDeepCopy<TKey>> Logger = logger;

	public async Task<TKey> ExecuteAsync(IDbConnection connection, TKey parameter)
	{
		if (connection.State != ConnectionState.Open) connection.Open();

		using var txn = connection.BeginTransaction();

		TKey result;

		result = await OnExecuteAsync(connection, txn, parameter);
		txn.Commit();

		return result;
	}

	protected async Task RunStepAsync<TStep, TEntity>(IDbConnection connection, IDbTransaction transaction, TKey parameter)
		where TStep : Step<TEntity>, new()
	{
		var step = new TStep()
		{
			KeyMap = KeyMap,
			Logger = Logger
		};
		await step.ExecuteAsync(connection, transaction, parameter);
	}

	/// <summary>
	/// override this to invoke your various Step classes (using Step.ExecuteAsync)
	/// </summary>
	protected abstract Task<TKey> OnExecuteAsync(IDbConnection connection, IDbTransaction transaction, TKey parameter);

	/// <summary>
	/// defines an individual copy step as part of a larger operation
	/// </summary>   
	protected abstract class Step<TEntity>
	{
#pragma warning disable CS8618 // this is created internally, always with Logger and KeyMap set
		protected Step()
#pragma warning restore CS8618
		{
		}

		public ILogger<LocalDeepCopy<TKey>> Logger { get; init; }
		public Dictionary<(string, TKey), TKey> KeyMap { get; init; }

		protected abstract string Name { get; }
		protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, TKey parameters);
		protected abstract TEntity CreateNewRow(TKey parameters, TEntity sourceRow);
		protected abstract TKey GetKey(TEntity sourceRow);
		protected abstract Task<TKey> InsertNewRowAsync(IDbConnection connection, IDbTransaction transaction, TEntity entity, TKey parameter);
		protected virtual async Task OnStepCompletedAsync(IDbConnection connection, IDbTransaction transaction, TKey parameter) => await Task.CompletedTask;

		public async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, TKey parameter)
		{
			Debug.Assert(Logger is not null);
			Debug.Assert(KeyMap is not null);

			Logger!.LogDebug("Querying source rows for step {StepName}", Name);
			var sourceRows = await QuerySourceRowsAsync(connection, transaction, parameter);

			foreach (var sourceRow in sourceRows)
			{
				var sourceKey = GetKey(sourceRow);
				var newRow = CreateNewRow(parameter, sourceRow);
				var newKey = await InsertNewRowAsync(connection, transaction, newRow, parameter);
				KeyMap![(Name, sourceKey)] = newKey;
			}

			await OnStepCompletedAsync(connection, transaction, parameter);
		}
	}
}
