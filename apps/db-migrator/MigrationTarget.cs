namespace SmartSolutionsLab.Roomy.DbMigrator;

// A context database the runner migrates. Each context registers one (via AddMigrationTarget) so the
// runner knows which DbContext to resolve and roll forward; the type identifies the registered context.
public sealed record MigrationTarget(Type ContextType)
{
    public string Name => ContextType.Name;
}
