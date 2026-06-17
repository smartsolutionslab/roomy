using NetArchTest.Rules;
using Shouldly;

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
        RoomyAssemblies.All.ShouldContain(ArchitectureConventions.InfrastructurePersistenceAssembly);
    }

    [Fact]
    public void InfrastructurePersistence_does_not_depend_on_MediatR()
    {
        var result = Types.InAssembly(ArchitectureConventions.InfrastructurePersistenceAssembly)
            .Should()
            .NotHaveDependencyOn("MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            SharedKernelPurityTests.FailureMessage("infrastructure must not depend on MediatR (ADR-0005)", result));
    }

    [Fact]
    public void InfrastructurePersistence_is_allowed_to_depend_on_ef_core()
    {
        var dependsOnEfCore = Types.InAssembly(ArchitectureConventions.InfrastructurePersistenceAssembly)
            .That()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetTypes();

        dependsOnEfCore.ShouldNotBeEmpty();
    }
}
