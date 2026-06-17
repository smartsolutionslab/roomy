using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// ENFORCED NOW. These rules constrain the only Roomy code assembly that exists today —
/// the shared-kernel — and therefore have real content to inspect rather than passing
/// vacuously. The shared-kernel is a pure primitives library (ADR-0006/#6); per ADR-0003
/// and ADR-0005 it must stay free of any framework or infrastructure dependency so that
/// every layer above it can take a dependency on it without inheriting one.
/// </summary>
public sealed class SharedKernelPurityTests
{
    [Fact]
    public void SharedKernel_does_not_depend_on_any_framework_or_infrastructure()
    {
        var result = Types.InAssembly(ArchitectureConventions.SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(ArchitectureConventions.ForbiddenFrameworkNamespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            FailureMessage("shared-kernel must not depend on MediatR, Wolverine, EF Core, ASP.NET Core, or YARP", result));
    }

    [Fact]
    public void SharedKernel_does_not_depend_on_MediatR()
    {
        var result = Types.InAssembly(ArchitectureConventions.SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOn("MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            FailureMessage("MediatR is forbidden in the core (ADR-0005)", result));
    }

    internal static string FailureMessage(string rule, NetArchTest.Rules.TestResult result)
    {
        var offenders = result.FailingTypeNames is { Count: > 0 }
            ? string.Join(", ", result.FailingTypeNames)
            : "(none reported)";

        return $"Architecture rule violated: {rule}. Offending types: {offenders}.";
    }
}
