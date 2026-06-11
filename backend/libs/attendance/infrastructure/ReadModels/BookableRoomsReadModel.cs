using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

public sealed class BookableRoomsReadModel(AttendanceDbContext context) : IBookableRoomsReadModel
{
    public async Task<IReadOnlyList<BookableRoomView>> GetAsync(CompanyIdentifier company, CancellationToken cancellationToken)
    {
        var companyId = company.Value;

        var rows = await context.Rooms.AsNoTracking()
            .Where(room => room.CompanyId == companyId)
            .GroupJoin(
                context.Offices.AsNoTracking(),
                room => room.OfficeId,
                office => office.OfficeId,
                (room, offices) => new { room, offices })
            .SelectMany(
                joined => joined.offices.DefaultIfEmpty(),
                (joined, office) => new { joined.room, office })
            .OrderBy(row => row.office != null ? row.office.Name : string.Empty)
            .ThenBy(row => row.room.Name)
            .Select(row => new
            {
                row.room.OfficeId,
                OfficeName = row.office != null ? row.office.Name : string.Empty,
                row.room.RoomId,
                RoomName = row.room.Name,
                row.room.Capacity,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new BookableRoomView(
                OfficeIdentifier.From(row.OfficeId),
                row.OfficeName,
                RoomIdentifier.From(row.RoomId),
                row.RoomName,
                RoomCapacity.From(row.Capacity)))
            .ToList();
    }
}
