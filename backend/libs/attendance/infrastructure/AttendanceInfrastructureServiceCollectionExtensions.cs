using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands;
using SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure;

public static class AttendanceInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAttendancePersistence(this IServiceCollection services, string connectionString)
    {
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddRoomyDbContext<AttendanceDbContext>(connectionString);
        services.AddScoped<EventStoreDbContext>(provider => provider.GetRequiredService<AttendanceDbContext>());

        services.AddSingleton(AttendanceEventTypeRegistry.Build());
        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IEventStore, EfCoreEventStore>();

        services.AddScoped<IReservationProjection, ReservationProjection>();
        services.AddScoped<IAttendanceDayRepository, AttendanceDayRepository>();

        services.AddScoped<ReservationsReadModelRebuilder>();

        services.AddScoped<IRoomDirectory, RoomDirectory>();
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        services.AddScoped<IOccupancyReadModel, OccupancyReadModel>();
        services.AddScoped<IMyReservationsReadModel, MyReservationsReadModel>();
        services.AddScoped<IBookableRoomsReadModel, BookableRoomsReadModel>();
        services.AddScoped<IEmployeeCatalog, EmployeeCatalog>();

        return services;
    }

    public static IServiceCollection AddAttendanceUseCases(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICommandHandler<ReservePlace, ReservationIdentifier>, ReservePlaceHandler>();
        services.AddScoped<ICommandHandler<CancelReservation>, CancelReservationHandler>();
        services.AddScoped<IQueryHandler<ViewDayReservations, IReadOnlyList<ReservationView>>, ViewDayReservationsHandler>();
        services.AddScoped<IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>>, ViewOccupancyHandler>();
        services.AddScoped<IQueryHandler<ViewMyReservations, Page<MyReservationView>>, ViewMyReservationsHandler>();
        services.AddScoped<IQueryHandler<ViewBookableRooms, IReadOnlyList<BookableRoomView>>, ViewBookableRoomsHandler>();
        services.AddScoped<IQueryHandler<ViewEmployees, Page<EmployeeView>>, ViewEmployeesHandler>();

        return services;
    }
}
