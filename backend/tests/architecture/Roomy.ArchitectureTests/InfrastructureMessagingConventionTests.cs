using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// ENFORCED NOW. The messaging adapter (#20, ADR-0005/0015) — the Wolverine-backed
/// <c>IIntegrationEventPublisher</c> and the composition-root transport wiring — is
/// <em>infrastructure</em>. The architecture rules treat it like the persistence baseline:
/// <list type="bullet">
/// <item>The repo-wide <strong>no-MediatR</strong> rule still applies (checked here explicitly, so it
/// is not vacuous).</item>
/// <item>The <strong>"no framework in the core"</strong> rules do <em>not</em> apply: this assembly
/// legitimately depends on Wolverine — it is the deferred messaging adapter the core never sees. Those
/// rules target only the <c>.Domain</c>/<c>.Application</c> namespace segments (see
/// <see cref="LayerDependencyConventionTests"/>), which this assembly's <c>.Infrastructure.Messaging</c>
/// namespace does not match — so Wolverine is allowed here without weakening the core rule that keeps
/// <c>domain</c>/<c>application</c> Wolverine-free.</item>
/// </list>
/// </summary>
public sealed class InfrastructureMessagingConventionTests
{
    [Fact]
    public void InfrastructureMessaging_is_discovered_by_the_convention_rule_set()
    {
        RoomyAssemblies.All.ShouldContain(ArchitectureConventions.InfrastructureMessagingAssembly);
    }

    [Fact]
    public void InfrastructureMessaging_does_not_depend_on_MediatR()
    {
        var result = Types.InAssembly(ArchitectureConventions.InfrastructureMessagingAssembly)
            .Should()
            .NotHaveDependencyOn("MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            SharedKernelPurityTests.FailureMessage("messaging infrastructure must not depend on MediatR (ADR-0005)", result));
    }

    [Fact]
    public void InfrastructureMessaging_is_allowed_to_depend_on_wolverine()
    {
        var dependsOnWolverine = Types.InAssembly(ArchitectureConventions.InfrastructureMessagingAssembly)
            .That()
            .HaveDependencyOn("Wolverine")
            .GetTypes();

        dependsOnWolverine.ShouldNotBeEmpty();
    }
}
