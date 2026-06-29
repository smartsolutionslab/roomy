using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

public sealed class OccupancyReadModel(AttendanceDbContext context) : IOccupancyReadModel
{
    public async Task<Result<OccupancyData>> GetAsync(CompanyIdentifier company, OccupancyScope scope, BookingDateRange range, CancellationToken cancellationToken)
    {
        var scopeResult = await ResolveScopeAsync(scope, cancellationToken).ConfigureAwait(false);
        if (scopeResult.IsFailure) return scopeResult.Error;

        var (officeId, officeName, rooms) = scopeResult.Value;
        var roomIds = rooms.Select(room => room.RoomId).ToList();
        var companyId = company.Value;

        var occupantRows = await context.Reservations.AsNoTracking()
            .Where(reservation => reservation.CompanyId == companyId && roomIds.Contains(reservation.RoomId))
            .WithinRange(range)
            .GroupJoin(
                context.Employees.AsNoTracking(),
                reservation => reservation.EmployeeId,
                employee => employee.EmployeeId,
                (reservation, matched) => new { reservation, matched })
            .SelectMany(
                joined => joined.matched.DefaultIfEmpty(),
                (joined, employee) => new
                {
                    joined.reservation.Date,
                    joined.reservation.RoomId,
                    joined.reservation.EmployeeId,
                    Name = employee != null ? employee.DisplayName : string.Empty,
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var descriptors = rooms.Select(room => new RoomDescriptor(
                RoomIdentifier.From(room.RoomId),
                room.Name,
                RoomCapacity.From(room.Capacity)))
            .ToList();

        var occupants = occupantRows.Select(row => new OccupantRecord(
                BookingDate.From(row.Date),
                RoomIdentifier.From(row.RoomId),
                EmployeeIdentifier.From(row.EmployeeId),
                row.Name))
            .ToList();

        return new OccupancyData(
            OfficeIdentifier.From(officeId),
            officeName,
            descriptors,
            occupants);
    }

    private async Task<Result<(Guid OfficeId, string OfficeName, List<Rooms.Room> Rooms)>> ResolveScopeAsync(OccupancyScope scope, CancellationToken cancellationToken)
    {
        if (scope.Room is { } room)
        {
            var known = await context.Rooms.AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.RoomId == room.Value, cancellationToken)
                .ConfigureAwait(false);
            if (known is null)
            {
                return AttendanceReadModelErrors.UnknownRoom();
            }

            return (known.OfficeId, await OfficeNameAsync(known.OfficeId, cancellationToken).ConfigureAwait(false), [known]);
        }

        var officeId = scope.Office!.Value;
        var office = await context.Offices.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.OfficeId.Equals(officeId), cancellationToken).ConfigureAwait(false);
        if (office is null) return Error.NotFound("unknown_office", "The office is not known to the attendance service yet.");

        var officeRooms = await context.Rooms.AsNoTracking()
            .Where(candidate => candidate.OfficeId.Equals(officeId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (officeId, office.Name, officeRooms);
    }

    private async Task<string> OfficeNameAsync(Guid officeId, CancellationToken cancellationToken)
    {
        var office = await context.Offices.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.OfficeId.Equals(officeId), cancellationToken)
            .ConfigureAwait(false);
        return office?.Name ?? string.Empty;
    }
}
