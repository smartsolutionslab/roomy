using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

/// <summary>
/// The transactional-publish seam for state-based contexts (ADR-0037). A context's unit of work hands
/// its <see cref="DbContext"/> together with the integration events drained from the aggregates'
/// domain events; the implementation stages them in the Wolverine outbox enrolled on that context and
/// saves, so the aggregate rows and the outbox rows commit in one transaction and relay at-least-once
/// thereafter (ADR-0012). This keeps Wolverine confined to the messaging library (ADR-0005): callers in
/// other contexts depend only on this port and EF Core, never on Wolverine.
/// </summary>
public interface IIntegrationEventOutbox
{
    /// <summary>
    /// Saves <paramref name="context"/> and publishes <paramref name="integrationEvents"/> atomically:
    /// the events are staged in the outbox on the same transaction as the save, then flushed for relay.
    /// An empty event set is a plain save.
    /// </summary>
    Task SaveAndPublishAsync(DbContext context, IReadOnlyCollection<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken);
}
