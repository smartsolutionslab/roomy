namespace SmartSolutionsLab.Roomy.DbMigrator;

public sealed record MigrationTarget(Type ContextType)
{
    public string Name => ContextType.Name;
}
