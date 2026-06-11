using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

internal sealed class TestEventStoreDbContext(DbContextOptions<TestEventStoreDbContext> options)
    : EventStoreDbContext(options);
