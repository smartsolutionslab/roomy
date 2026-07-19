using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

public sealed class RoomyAssembliesTests
{
    private static readonly string[] expectedAssemblyNames =
    [
        "SmartSolutionsLab.Roomy.Application.Contracts",
        "SmartSolutionsLab.Roomy.Attendance.Application",
        "SmartSolutionsLab.Roomy.Attendance.Domain",
        "SmartSolutionsLab.Roomy.Attendance.Infrastructure",
        "SmartSolutionsLab.Roomy.Contracts.Identity",
        "SmartSolutionsLab.Roomy.Contracts.Organization",
        "SmartSolutionsLab.Roomy.Identity.Application",
        "SmartSolutionsLab.Roomy.Identity.Domain",
        "SmartSolutionsLab.Roomy.Identity.Infrastructure",
        "SmartSolutionsLab.Roomy.Infrastructure.Authentication",
        "SmartSolutionsLab.Roomy.Infrastructure.Cryptography",
        "SmartSolutionsLab.Roomy.Infrastructure.Messaging",
        "SmartSolutionsLab.Roomy.Infrastructure.Persistence",
        "SmartSolutionsLab.Roomy.Organization.Application",
        "SmartSolutionsLab.Roomy.Organization.Domain",
        "SmartSolutionsLab.Roomy.Organization.Infrastructure",
        "SmartSolutionsLab.Roomy.SharedKernel",
        "SmartSolutionsLab.Roomy.Web.Http",
    ];

    [Fact]
    public void Discovery_finds_every_expected_roomy_assembly()
    {
        var discoveredNames = RoomyAssemblies.All
            .Select(assembly => assembly.GetName().Name)
            .ToArray();

        var missingNames = expectedAssemblyNames.Except(discoveredNames, StringComparer.Ordinal).ToArray();

        missingNames.ShouldBeEmpty(
            "Assembly discovery silently dropped expected assemblies — their architecture "
            + "rules are passing vacuously. Missing: " + string.Join(", ", missingNames));
    }

    [Fact]
    public void Discovery_does_not_inspect_the_test_assembly_itself()
    {
        RoomyAssemblies.All
            .Select(assembly => assembly.GetName().Name)
            .ShouldNotContain("SmartSolutionsLab.Roomy.ArchitectureTests");
    }
}
