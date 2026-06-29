using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

public sealed class ViewOccupancyHandler(IOccupancyReadModel readModel, IBusinessClock clock)
    : IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>>
{
    public async Task<Result<IReadOnlyList<OccupancyView>>> HandleAsync(ViewOccupancy query, CancellationToken cancellationToken)
    {
        var (company, scope, range) = query;
        var data = await readModel.GetAsync(company, scope, range, cancellationToken);

        if (data.IsFailure) return data.Error;

        var today = clock.Today.Value;
        var tomorrow = today.AddDays(1);

        var bookings = data.Value.Occupants.ToLookup(occupant => (occupant.Room, occupant.Date));

        var views = new List<OccupancyView>();
        foreach (var date in range.Days())
        {
            var showNames = date.Value == today || date.Value == tomorrow;
            views.Add(BuildDay(date, data.Value, bookings, showNames));
        }

        return Result.Success<IReadOnlyList<OccupancyView>>(views);
    }

    private static OccupancyView BuildDay(BookingDate date, OccupancyData data, ILookup<(RoomIdentifier Room, BookingDate Date), OccupantRecord> bookings, bool showNames)
    {
        var rooms = data.Rooms.Select(room => BuildRoom(date, room, bookings, showNames)).ToList();

        var occupiedTotal = rooms.Sum(room => room.Occupied);
        var capacityTotal = rooms.Sum(room => room.Capacity);
        var office = new OfficeOccupancy(
            data.Office,
            data.OfficeName,
            occupiedTotal,
            capacityTotal,
            IsFull: capacityTotal > 0 && occupiedTotal >= capacityTotal);

        return new OccupancyView(date, office, rooms);
    }

    private static RoomOccupancy BuildRoom(BookingDate date, RoomDescriptor room, ILookup<(RoomIdentifier Room, BookingDate Date), OccupantRecord> bookings, bool showNames)
    {
        var booked = bookings[(room.Room, date)].ToList();
        var capacity = room.Capacity.Value;

        // Zero capacity means not bookable, so never full — the same rule the office rollup applies. A
        // real room is always >= 1 (RoomCapacity), so the guard is defensive symmetry, not a live branch.
        return new RoomOccupancy(
            room.Room,
            room.RoomName,
            booked.Count,
            capacity,
            IsFull: capacity > 0 && booked.Count >= capacity,
            Occupants: showNames
                ? booked.Select(occupant => new Occupant(occupant.Employee, occupant.EmployeeName)).ToList()
                : null);
    }
}
