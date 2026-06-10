using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

// The IBookableRoomsReadModel adapter (007 US1): it lists the company's rooms from the local Rooms read
// model, joined to the local Offices read model for their names — all attendance's own read models, fed
// by organization's RoomAdded/OfficeOpened (ADR-0014/0031), never a cross-service join. Ordered by
// office then room so the picker reads naturally; the office name defaults to empty if its feed has not
// arrived. A room whose office is unknown is still listed (it is bookable) under an empty office name.
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

        return rows
            .Select(row => new BookableRoomView(
                OfficeIdentifier.From(row.OfficeId),
                row.OfficeName,
                RoomIdentifier.From(row.RoomId),
                row.RoomName,
                RoomCapacity.From(row.Capacity)))
            .ToList();
    }
}
