using System.Reflection;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

internal static class ArchitectureConventions
{
    internal const string RootNamespace = "SmartSolutionsLab.Roomy";

    internal const string SharedKernelNamespace = RootNamespace + ".SharedKernel";

    internal const string DomainSegment = ".Domain";

    internal const string ApplicationSegment = ".Application";

    internal const string InfrastructureSegment = ".Infrastructure";

    internal static readonly string[] ForbiddenFrameworkNamespaces =
    [
        "MediatR",
        "Wolverine",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Yarp",
    ];

    internal static Assembly SharedKernelAssembly => typeof(Ensure).Assembly;

    internal static Assembly ApplicationContractsAssembly => typeof(IIntegrationEventPublisher).Assembly;

    internal static Assembly InfrastructurePersistenceAssembly => typeof(IEventStore).Assembly;

    internal static Assembly InfrastructureMessagingAssembly =>
        typeof(WolverineIntegrationEventPublisher).Assembly;
}
