using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

/// <summary>
/// A minimal concrete <see cref="EventStoreDbContext"/> for the SQLite-backed tests — it adds no
/// aggregates of its own, so it exercises exactly the shared events + outbox mapping and the
/// snake_case policy from the baseline.
/// </summary>
internal sealed class TestEventStoreDbContext(DbContextOptions<TestEventStoreDbContext> options)
    : EventStoreDbContext(options);
