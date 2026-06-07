using NetArchTest.Rules;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// ENFORCED NOW (repo-wide). MediatR is forbidden anywhere in Roomy — the application layer
/// owns its own dispatch abstractions instead (ADR-0005, constitution Principle IV). Today
/// this inspects every loaded Roomy assembly (currently just the shared-kernel); it grows to
/// cover each context assembly automatically as they are added, with no edit here.
/// </summary>
public sealed class NoMediatRTests
{
    [Fact]
    public void No_Roomy_assembly_depends_on_MediatR()
    {
        // Real assertion today (the shared-kernel is present); not vacuous.
        Assert.NotEmpty(RoomyAssemblies.All);

        foreach (var assembly in RoomyAssemblies.All)
        {
            var result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOn("MediatR")
                .GetResult();

            Assert.True(
                result.IsSuccessful,
                SharedKernelPurityTests.FailureMessage(
                    $"MediatR is forbidden (assembly {assembly.GetName().Name})",
                    result));
        }
    }
}
