using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// The attendance context's database (ADR-0014, owned per context). It is event-sourced (ADR-0026),
// so it derives EventStoreDbContext and gains only the append-only events table for now; the Rooms and
// Employees read models are added with the integration-event consumers (US2/US4).
public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
    : EventStoreDbContext(options);
