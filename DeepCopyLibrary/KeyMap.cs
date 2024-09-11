namespace DeepCopyLibrary;

public abstract class KeyMap<TKey> where TKey : notnull
{
	private Dictionary<(string, TKey), TKey> _keyMap = [];

	protected abstract Task LoadAsync();
}
