namespace SmartSolutionsLab.Roomy.SharedKernel;

// Marker for a domain event: an immutable record of something that happened inside a bounded context,
// raised by an aggregate as it enforces its invariants (ADR-0032). It carries primitives/value objects
// and its own OccurredAt. Intra-context only — cross-context signals are integration events, not these.
public interface IDomainEvent;
