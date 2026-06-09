using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests;

// The event-sourced write-model base (ADR-0036): state is the fold over a stream. A tiny Counter
// aggregate exercises the contract — replay, raise (apply + collect), version tracking — without
// pulling in any context or the event store.
public class EventSourcedAggregateTests
{
    private sealed record Incremented(int By);

    private sealed class Counter : EventSourcedAggregate
    {
        public int Total { get; private set; }

        public void Add(int by) => Raise(new Incremented(by));

        protected override void Apply(object @event)
        {
            if (@event is Incremented incremented)
            {
                Total += incremented.By;
            }
        }
    }

    [Fact]
    public void A_fresh_aggregate_is_at_version_zero_with_no_uncommitted_events()
    {
        var counter = new Counter();

        counter.Version.ShouldBe(0);
        counter.UncommittedEvents.ShouldBeEmpty();
        counter.Total.ShouldBe(0);
    }

    [Fact]
    public void Raise_applies_the_event_immediately_and_collects_it_as_uncommitted()
    {
        var counter = new Counter();

        counter.Add(5);

        counter.Total.ShouldBe(5);
        counter.UncommittedEvents.Count.ShouldBe(1);
        counter.UncommittedEvents[0].ShouldBeOfType<Incremented>().By.ShouldBe(5);
    }

    [Fact]
    public void Raise_does_not_advance_the_persisted_version()
    {
        // Version is the count the store is at — uncommitted events have not been appended yet, so
        // it is what the repository asserts as the expected version on save (optimistic concurrency).
        var counter = new Counter();

        counter.Add(5);

        counter.Version.ShouldBe(0);
    }

    [Fact]
    public void LoadFromHistory_replays_events_through_apply_and_advances_the_version()
    {
        var counter = new Counter();

        counter.LoadFromHistory([new Incremented(2), new Incremented(3)]);

        counter.Total.ShouldBe(5);
        counter.Version.ShouldBe(2);
        counter.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raising_after_a_replay_collects_only_the_new_event_and_keeps_the_loaded_version()
    {
        var counter = new Counter();
        counter.LoadFromHistory([new Incremented(2), new Incremented(3)]);

        counter.Add(1);

        counter.Total.ShouldBe(6);
        counter.Version.ShouldBe(2);
        counter.UncommittedEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Clearing_uncommitted_events_empties_the_buffer_without_touching_state()
    {
        var counter = new Counter();
        counter.Add(5);

        counter.ClearUncommittedEvents();

        counter.UncommittedEvents.ShouldBeEmpty();
        counter.Total.ShouldBe(5);
    }
}
