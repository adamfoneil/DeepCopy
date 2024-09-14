namespace DeepCopyLibrary;

/// <summary>
/// defines operations on durable store of source and destination keys
/// </summary>
public interface IKeyMapRepository<TKey> where TKey : notnull
{
	bool ContainsKey(string stepName, TKey key);
	Task AddAsync(string stepName, TKey sourceKey, TKey targetKey);
	TKey this[string stepName, TKey key] { get; }
}
