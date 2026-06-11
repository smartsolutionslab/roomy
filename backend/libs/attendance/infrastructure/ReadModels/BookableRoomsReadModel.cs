using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

public sealed class BookableRoomsReadModel(AttendanceDbContext context) : IBookableRoomsReadModel
{
    public async Task<IReadOnlyList<BookableRoomView>> GetAsync(
        CompanyIdentifier company,
        CancellationToken cancellationToken)
    {
        var companyId = company.Value;

        var rows = await (
            from room in context.Rooms.AsNoTracking()
            where room.CompanyId == companyId
            join office in context.Offices.AsNoTracking()
                on room.OfficeId equals office.OfficeId into offices
            from office in offices.DefaultIfEmpty()
            orderby office != null ? office.Name : string.Empty, room.Name
            select new
            {
                room.OfficeId,
                OfficeName = office != null ? office.Name : string.Empty,
                room.RoomId,
                RoomName = room.Name,
                room.Capacity,
            }).ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new BookableRoomView(
                OfficeIdentifier.From(row.OfficeId),
                row.OfficeName,
                RoomIdentifier.From(row.RoomId),
                row.RoomName,
                RoomCapacity.From(row.Capacity)))
            .ToList();
    }
}
