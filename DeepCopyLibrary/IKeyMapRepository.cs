using System.Data;

namespace DeepCopyLibrary;

public interface IKeyMapRepository<TKey> where TKey : notnull
{
	bool ContainsKey(string stepName, TKey key);
	Task AddAsync(string stepName, TKey sourceKey, TKey targetKey, IDbTransaction transaction);
}
