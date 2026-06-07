using System.Reflection;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// Shared constants and helpers for the NetArchTest rule set: the project's namespace
/// conventions and the set of framework/infrastructure assemblies that the core
/// (<c>domain</c>/<c>application</c>) and the pure <c>shared-kernel</c> must never reference.
/// </summary>
internal static class ArchitectureConventions
{
    /// <summary>Root namespace shared by every Roomy assembly.</summary>
    internal const string RootNamespace = "SmartSolutionsLab.Roomy";

    /// <summary>The shared-kernel namespace — a pure primitives library with no infra deps.</summary>
    internal const string SharedKernelNamespace = RootNamespace + ".SharedKernel";

    /// <summary>
    /// Namespace segment that marks a context's domain layer
    /// (e.g. <c>SmartSolutionsLab.Roomy.Attendance.Domain</c>).
    /// </summary>
    internal const string DomainSegment = ".Domain";

    /// <summary>Namespace segment that marks a context's application layer.</summary>
    internal const string ApplicationSegment = ".Application";

    /// <summary>Namespace segment that marks a context's infrastructure layer.</summary>
    internal const string InfrastructureSegment = ".Infrastructure";

    /// <summary>
    /// Mediator and framework dependencies that must never appear in the core. Matched as
    /// namespace prefixes, so e.g. <c>MediatR.Pipeline</c> is caught by <c>MediatR</c>.
    /// </summary>
    internal static readonly string[] ForbiddenFrameworkNamespaces =
    [
        "MediatR",
        "Wolverine",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Yarp",
    ];

    /// <summary>
    /// A live anchor type inside the shared-kernel assembly, used to load that assembly for
    /// inspection without depending on reflection-by-name (which a typo could silently break).
    /// </summary>
    internal static Assembly SharedKernelAssembly => typeof(Ensure).Assembly;
}
