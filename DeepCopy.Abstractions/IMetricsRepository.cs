namespace DeepCopy.Abstractions;

public interface IMetricsRepository
{
	Task LogAsync(string stepName, int successRows, int errorRows, int skippedRows, int createErrors, TimeSpan duration);
}
