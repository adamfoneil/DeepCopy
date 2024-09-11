This is a library (in progress) for copying relational data with a lot of structural depth. This is done with a top level container class [DeepCopy](https://github.com/adamfoneil/DeepCopy/blob/master/DeepCopyLibrary/DeepCopy.cs#L5) with one or more nested [Step](https://github.com/adamfoneil/DeepCopy/blob/master/DeepCopyLibrary/DeepCopy.cs#L45) classes within it. It would look something like this in use:

```csharp
public class MyDeepCopy : DeepCopy<int, int, int>
{
    protected override OnExecuteAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
    {
        await new ParentTableStep(KeyMap).ExecuteAsync(connection, transaction, parameters);
        await new ChildTableStep(KeyMap).ExecuteAsync(connection, transaction, parameters);
    }

    private class ParentTableStep(KeyMap<int> keyMap) : Step<ParentTable>(keyMap)
    {
        protected override Task<IEnumerable<ParentTable> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
        {
              // todo: some kind of query that returns the rows I want to copy
        }
    }

    private class ChildTableStep(KeyMap<int> keyMap) : Step<ChildTable>(keyMap)
    {
        protected override Task<IEnumerable<ChildTable> QuerySourceRowsAsync(IDbConnection connection, IDbTransaction transaction, int parameters)
        {
              // todo: some kind of query that returns the rows I want to copy
        }
    }
}
```

This is similar to things I've done in the past, but streamlines it into a more encapsulated library with no particular tool dependencies.

See
- Zinger [DataMigrator](https://github.com/adamfoneil/Postulate.Zinger/wiki/Data-Migrator)
- [SqlMigrator](https://github.com/adamfoneil/SqlServerUtil/wiki/Using-SqlMigrator)
