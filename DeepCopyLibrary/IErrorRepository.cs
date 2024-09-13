namespace DeepCopyLibrary;

public interface IErrorRepository
{
	Task WhenInsertingAsync<TEntity>(string stepName, Exception exception, TEntity sourceRow, TEntity newRow);
	Task WhenQueryingAsync(string stepName, Exception exception);
	Task WhenLoopingAsync(string stepName, Exception exception);
}
