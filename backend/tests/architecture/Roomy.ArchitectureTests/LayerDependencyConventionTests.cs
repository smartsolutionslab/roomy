using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

public sealed class LayerDependencyConventionTests
{
    [Fact]
    public void Domain_does_not_depend_on_application_or_infrastructure()
    {
        AssertConvention(
            "domain must not depend on application/infrastructure",
            ArchitectureConventions.DomainSegment,
            predicate => predicate.ShouldNot()
                .HaveDependencyOnAny(
                    ArchitectureConventions.ApplicationSegment,
                    ArchitectureConventions.InfrastructureSegment));
    }

    [Fact]
    public void Domain_does_not_depend_on_any_framework_or_infrastructure()
    {
        AssertConvention(
            "domain must not depend on any framework (MediatR, Wolverine, EF Core, ASP.NET, YARP)",
            ArchitectureConventions.DomainSegment,
            predicate => predicate.ShouldNot().HaveDependencyOnAny(ArchitectureConventions.ForbiddenFrameworkNamespaces));
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure()
    {
        AssertConvention(
            "application must not depend on infrastructure",
            ArchitectureConventions.ApplicationSegment,
            predicate => predicate.ShouldNot().HaveDependencyOn(ArchitectureConventions.InfrastructureSegment));
    }

    [Fact]
    public void Application_does_not_depend_on_any_framework()
    {
        AssertConvention(
            "application must not depend on any framework (MediatR, Wolverine, EF Core, ASP.NET, YARP)",
            ArchitectureConventions.ApplicationSegment,
            predicate => predicate.ShouldNot().HaveDependencyOnAny(ArchitectureConventions.ForbiddenFrameworkNamespaces));
    }

    private static void AssertConvention(
        string description,
        string layerSegment,
        Func<PredicateList, ConditionList> rule)
    {
        var matchedTypes = 0;

        foreach (var assembly in RoomyAssemblies.All)
        {
            var predicate = Types.InAssembly(assembly).That().ResideInNamespaceContaining(layerSegment);

            matchedTypes += predicate.GetTypes().Count();

            var result = rule(predicate).GetResult();

            result.IsSuccessful.ShouldBeTrue(SharedKernelPurityTests.FailureMessage(description, result));
        }

        matchedTypes.ShouldBeGreaterThan(
            0,
            $"Convention '{description}' inspected no types: 0 types match '{layerSegment}' "
            + $"across {RoomyAssemblies.All.Count} Roomy assembly(ies) — assembly discovery is "
            + "broken and the rule is passing vacuously.");
    }
}
