using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

public sealed class NoMediatRTests
{
    [Fact]
    public void No_Roomy_assembly_depends_on_MediatR()
    {
        // Real assertion today (the shared-kernel is present); not vacuous.
        RoomyAssemblies.All.ShouldNotBeEmpty();

        foreach (var assembly in RoomyAssemblies.All)
        {
            var result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOn("MediatR")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue(
                SharedKernelPurityTests.FailureMessage($"MediatR is forbidden (assembly {assembly.GetName().Name})", result));
        }
    }
}
