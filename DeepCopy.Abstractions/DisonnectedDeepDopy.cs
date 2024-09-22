using Microsoft.Extensions.Logging;
using System.Data;

namespace DeepCopy.Abstractions;

/// <summary>
/// performs a multi-step copy operation as a two-step stage + commit model
/// </summary>
public abstract class DisonnectedDeepCopy<TKey>(
    IKeyMapRepository<TKey> keyMapRepository,
    IBridgeStore<TKey> bridgeStore,
    IMetricsRepository metricsRepository,
    ILogger<DisonnectedDeepCopy<TKey>> logger) where TKey : notnull
{
    protected readonly IKeyMapRepository<TKey> KeyMap = keyMapRepository;
    protected readonly IBridgeStore<TKey> BridgeStore = bridgeStore;
    protected readonly ILogger<DisonnectedDeepCopy<TKey>> Logger = logger;

    private readonly IMetricsRepository _metrics = metricsRepository;
    

    public abstract Task StageAsync(IDbConnection sourceConnection, CancellationToken cancellationToken);
    
    public abstract Task CommitAsync(IDbConnection destConnection, CancellationToken cancellationToken);


    /// <summary>
    /// defines an individual copy step as part of a larger operation
    /// </summary>   
    protected abstract class Step<TEntity>(
        DisonnectedDeepCopy<TKey> operation) where TEntity : new()
    {
        private readonly DisonnectedDeepCopy<TKey> _operation = operation;

        protected abstract Task<IEnumerable<TEntity>> QuerySourceRowsAsync(IDbConnection sourceConnection, TKey parameter);

        protected virtual int StageChunkSize { get => 30; }
        protected virtual string StepName { get => GetType().Name; }

        public async Task StageAsync(IDbConnection sourceConnection, TKey parameter)
        {
            var sourceRows = await QuerySourceRowsAsync(sourceConnection, parameter);

            await _operation.BridgeStore.ClearAsync(StepName);

            foreach (var chunk in sourceRows.Chunk(StageChunkSize)) await _operation.BridgeStore.SaveChunk(chunk);
        }

        public async Task CommitAsync(IDbConnection destConnection, TKey parameter)
        {

        }
    }
}
