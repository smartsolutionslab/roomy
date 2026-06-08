using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

public sealed class EventTypeRegistryTests
{
    [Fact]
    public void Resolves_name_for_a_registered_type()
    {
        var registry = EventTypeRegistry.Create()
            .Register<DeskBooked>("desk-booked")
            .Build();

        Assert.Equal("desk-booked", registry.GetName(typeof(DeskBooked)));
    }

    [Fact]
    public void Resolves_type_for_a_registered_name()
    {
        var registry = EventTypeRegistry.Create()
            .Register<DeskBooked>("desk-booked")
            .Build();

        Assert.Equal(typeof(DeskBooked), registry.Resolve("desk-booked"));
    }

    [Fact]
    public void GetName_throws_for_an_unregistered_type()
    {
        var registry = EventTypeRegistry.Create().Build();

        Assert.Throws<UnknownEventTypeException>(() => registry.GetName(typeof(DeskBooked)));
    }

    [Fact]
    public void Resolve_throws_for_an_unregistered_name()
    {
        var registry = EventTypeRegistry.Create().Build();

        Assert.Throws<UnknownEventTypeException>(() => registry.Resolve("unknown"));
    }

    [Fact]
    public void Registering_the_same_type_twice_throws()
    {
        var builder = EventTypeRegistry.Create().Register<DeskBooked>("desk-booked");

        Assert.Throws<ArgumentException>(() => builder.Register<DeskBooked>("other-name"));
    }

    [Fact]
    public void Registering_the_same_name_twice_throws()
    {
        var builder = EventTypeRegistry.Create().Register<DeskBooked>("desk-booked");

        Assert.Throws<ArgumentException>(() => builder.Register<DeskReleased>("desk-booked"));
    }
}
