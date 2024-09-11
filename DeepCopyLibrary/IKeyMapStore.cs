namespace DeepCopyLibrary;

public interface IKeyMapStore<TKey> where TKey : notnull
{
	Task<Dictionary<(string, TKey), TKey>?> GetAsync();
	Task SaveAsync(Dictionary<(string, TKey), TKey> idMap);
}
