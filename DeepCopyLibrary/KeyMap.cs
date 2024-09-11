namespace DeepCopyLibrary;

public abstract class KeyMap<TKey> where TKey : notnull
{
	private Dictionary<(string StepName, TKey SourceKey), TKey> _keyMap = [];

	protected abstract Task LoadAsync();
}
