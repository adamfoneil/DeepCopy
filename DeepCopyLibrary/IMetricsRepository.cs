namespace DeepCopyLibrary;

public interface IMetricsRepository
{
	Task LogAsync(string stepName, int successRows, int errorRows, int skippedRows, TimeSpan duration);
}
