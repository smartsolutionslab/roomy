namespace SmartSolutionsLab.Roomy.SharedKernel;

// Marker for an aggregate root — an entity that is also a consistency boundary (ADR-0003).
public interface IAggregate : IEntity;
