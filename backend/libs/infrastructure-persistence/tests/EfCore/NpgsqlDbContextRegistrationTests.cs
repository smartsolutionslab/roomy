using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EfCore;

public sealed class NpgsqlDbContextRegistrationTests
{
    [Fact]
    public void AddRoomyDbContext_registers_the_system_time_provider_as_a_singleton()
    {
        var services = new ServiceCollection();

        services.AddRoomyDbContext<TestEventStoreDbContext>("Host=localhost;Database=roomy;Username=postgres;Password=postgres");

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }
}
