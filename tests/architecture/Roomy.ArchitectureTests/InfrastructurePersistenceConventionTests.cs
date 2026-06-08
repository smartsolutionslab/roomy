using NetArchTest.Rules;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// ENFORCED NOW. The shared persistence building blocks (#19, ADR-0012) — the EF Core baseline and
/// the hand-rolled event store — are <em>infrastructure</em>. The architecture rules treat them
/// differently from the core on purpose:
/// <list type="bullet">
/// <item>The repo-wide <strong>no-MediatR</strong> rule still applies (and is checked here against
/// this assembly explicitly, so it is not vacuous).</item>
/// <item>The <strong>"no framework in the core"</strong> rules do <em>not</em> apply: infrastructure
/// legitimately depends on EF Core / Npgsql. Those rules target only the <c>.Domain</c> and
/// <c>.Application</c> namespace segments (see <see cref="LayerDependencyConventionTests"/>), which
/// this assembly's <c>.Infrastructure.Persistence</c> namespace does not match — so it is neither
/// wrongly failed for using EF Core, nor are those rules weakened.</item>
/// </list>
/// </summary>
public sealed class InfrastructurePersistenceConventionTests
{
    [Fact]
    public void InfrastructurePersistence_is_discovered_by_the_convention_rule_set()
    {
        Assert.Contains(
            RoomyAssemblies.All,
            assembly => assembly == ArchitectureConventions.InfrastructurePersistenceAssembly);
    }

    [Fact]
    public void InfrastructurePersistence_does_not_depend_on_MediatR()
    {
        var result = Types.InAssembly(ArchitectureConventions.InfrastructurePersistenceAssembly)
            .Should()
            .NotHaveDependencyOn("MediatR")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            SharedKernelPurityTests.FailureMessage(
                "infrastructure must not depend on MediatR (ADR-0005)",
                result));
    }

    [Fact]
    public void InfrastructurePersistence_is_allowed_to_depend_on_ef_core()
    {
        // Documents the infra-vs-core distinction: this is the one place a dependency on EF Core is
        // legitimate. The assertion confirms the baseline actually uses EF Core (so the allowance is
        // real, not theoretical) and that no core rule has been mis-scoped to forbid it here.
        var dependsOnEfCore = Types.InAssembly(ArchitectureConventions.InfrastructurePersistenceAssembly)
            .That()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetTypes();

        Assert.NotEmpty(dependsOnEfCore);
    }
}
