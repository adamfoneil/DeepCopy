namespace DeepCopy.Abstractions;

/// <summary>
/// defines operations for durable storage accessible to source 
/// and dest connections in a disconnected copy operation
/// </summary>
public interface IBridgeStore<TKey> where TKey : notnull
{    
	Task SaveChunk<TEntity>(TEntity[] entities);    
    Task<TEntity> GetAsync<TEntity>(TKey key);
    Task ClearAsync(string stepName);
}
