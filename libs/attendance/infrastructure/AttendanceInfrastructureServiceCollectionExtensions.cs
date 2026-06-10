using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure;

// Composition-root wiring for the attendance context's infrastructure adapters, keeping the EF Core /
// event-store details out of the host's Program.cs (ADR-0003/0012). The IRoomDirectory adapter is not
// wired here: until organization's capacity feed lands (US2) the host supplies a temporary one.
public static class AttendanceInfrastructureServiceCollectionExtensions
{
    // Registers the attendance database (its own Postgres, ADR-0014) as an event store and the
    // event-sourced AttendanceDay repository over it (ADR-0012).
    public static IServiceCollection AddAttendancePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddRoomyDbContext<AttendanceDbContext>(connectionString);
        services.AddScoped<EventStoreDbContext>(provider => provider.GetRequiredService<AttendanceDbContext>());

        services.AddSingleton<IEventTypeRegistry>(AttendanceEventTypeRegistry.Build());
        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IEventStore, EfCoreEventStore>();

        // The occupancy projection is staged inline with the event append (ADR-0038), so it shares the
        // scoped AttendanceDbContext with the event store and commits in the same transaction.
        services.AddScoped<IReservationProjection, ReservationProjection>();
        services.AddScoped<IAttendanceDayRepository, AttendanceDayRepository>();

        return services;
    }

    // Registers the attendance use cases behind their owned command-handler ports (ADR-0005).
    // TimeProvider supplies "today" (Europe/Berlin) and the event timestamps, and is the seam that keeps
    // them testable.
    public static IServiceCollection AddAttendanceUseCases(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICommandHandler<ReservePlace, ReservationIdentifier>, ReservePlaceHandler>();
        services.AddScoped<ICommandHandler<CancelReservation>, CancelReservationHandler>();
        services.AddScoped<IQueryHandler<ViewDayReservations, IReadOnlyList<ReservationView>>, ViewDayReservationsHandler>();
        services.AddScoped<IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>>, ViewOccupancyHandler>();
        services.AddScoped<IQueryHandler<ViewMyReservations, IReadOnlyList<MyReservationView>>, ViewMyReservationsHandler>();
        services.AddScoped<IQueryHandler<ViewBookableRooms, IReadOnlyList<BookableRoomView>>, ViewBookableRoomsHandler>();
        services.AddScoped<IQueryHandler<ViewEmployees, IReadOnlyList<EmployeeView>>, ViewEmployeesHandler>();

        return services;
    }
}
