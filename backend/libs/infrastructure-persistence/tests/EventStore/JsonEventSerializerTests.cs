using Shouldly;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

public sealed class JsonEventSerializerTests
{
    private static JsonEventSerializer CreateSerializer() =>
        new(EventTypeRegistry.Create()
            .Register<DeskBooked>("desk-booked")
            .Register<DeskReleased>("desk-released")
            .Build());

    [Fact]
    public void Serialize_uses_the_registered_type_name()
    {
        var serializer = CreateSerializer();

        var serialized = serializer.Serialize(new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8)));

        serialized.TypeName.ShouldBe("desk-booked");
    }

    [Fact]
    public void Round_trips_an_event_through_serialize_and_deserialize()
    {
        var serializer = CreateSerializer();
        var original = new DeskBooked(Guid.NewGuid(), "ada", new DateOnly(2026, 6, 8));

        var serialized = serializer.Serialize(original);
        var restored = serializer.Deserialize(serialized.TypeName, serialized.Payload);

        restored.ShouldBe(original);
    }

    [Fact]
    public void Deserialize_throws_for_an_unknown_type_name()
    {
        var serializer = CreateSerializer();

        Should.Throw<UnknownEventTypeException>(() => serializer.Deserialize("unknown", "{}"));
    }
}
