using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// ENFORCED NOW. The owned application contracts (#18) — command/query and handler abstractions,
/// the dispatch ports, and the integration-event publisher port — are the seam that keeps the
/// messaging framework at the edge (ADR-0005, constitution Principle IV). They must therefore stay
/// free of any framework or infrastructure dependency. These rules anchor the assembly explicitly
/// so the check has real content to inspect rather than passing vacuously.
/// </summary>
public sealed class ApplicationContractsPurityTests
{
    [Fact]
    public void ApplicationContracts_does_not_depend_on_any_framework_or_infrastructure()
    {
        var result = Types.InAssembly(ArchitectureConventions.ApplicationContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConventions.ForbiddenFrameworkNamespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            SharedKernelPurityTests.FailureMessage("application contracts must not depend on MediatR, Wolverine, EF Core, ASP.NET Core, or YARP", result)
            );
    }

    [Fact]
    public void ApplicationContracts_is_discovered_by_the_convention_rule_set()
    {
        RoomyAssemblies.All.ShouldContain(ArchitectureConventions.ApplicationContractsAssembly);
    }
}
