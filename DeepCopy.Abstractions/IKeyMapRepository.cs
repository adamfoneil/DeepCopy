namespace DeepCopy.Abstractions;

/// <summary>
/// defines operations on durable store of source and destination keys
/// </summary>
public interface IKeyMapRepository<TKey> where TKey : notnull
{
    /// <summary>
    /// should load the key map from the durable store
    /// </summary>	
    Task InitializeAsync();
	bool ContainsKey(string stepName, TKey key);
	Task AddAsync(string stepName, TKey sourceKey, TKey targetKey);
    /// <summary>
    /// use this in your Step classes to get the target key for a source key
    /// </summary>
	TKey this[string stepName, TKey key] { get; }
}
