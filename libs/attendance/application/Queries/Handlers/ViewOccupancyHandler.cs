using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Composes the occupancy figures (research R7): it reads the raw rooms + reservations from the read
// model, then for each day in the range builds every room's occupied/capacity figure and the office
// rollup (sum over rooms), marks a room or office full when occupied == capacity (FR-008), and applies
// the data-minimisation policy — employee names are shown only for today and the following day (FR-007),
// counts only on every other day. "Today" is the Europe/Berlin calendar day from the injected
// TimeProvider, so the policy stays clock-free and testable (research R4).
public sealed class ViewOccupancyHandler(IOccupancyReadModel readModel, TimeProvider timeProvider)
    : IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>>
{
    private static readonly TimeZoneInfo berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public async Task<Result<IReadOnlyList<OccupancyView>>> HandleAsync(
        ViewOccupancy query,
        CancellationToken cancellationToken)
    {
        var data = await readModel
            .GetAsync(query.Company, query.Scope, query.From, query.To, cancellationToken)
            .ConfigureAwait(false);
        if (data.IsFailure)
        {
            return data.Error;
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), berlinZone).DateTime);
        var tomorrow = today.AddDays(1);

        var views = new List<OccupancyView>();
        for (var date = query.From.Value; date <= query.To.Value; date = date.AddDays(1))
        {
            var showNames = date == today || date == tomorrow;
            views.Add(BuildDay(BookingDate.From(date), data.Value, showNames));
        }

        return Result.Success<IReadOnlyList<OccupancyView>>(views);
    }

    private static OccupancyView BuildDay(BookingDate date, OccupancyData data, bool showNames)
    {
        var rooms = data.Rooms
            .Select(room => BuildRoom(date, room, data.Occupants, showNames))
            .ToList();

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

    private static RoomOccupancy BuildRoom(
        BookingDate date,
        RoomDescriptor room,
        IReadOnlyList<OccupantRecord> occupants,
        bool showNames)
    {
        var booked = occupants
            .Where(occupant => occupant.Room == room.Room && occupant.Date == date)
            .ToList();

        return new RoomOccupancy(
            room.Room,
            room.RoomName,
            booked.Count,
            room.Capacity.Value,
            IsFull: booked.Count >= room.Capacity.Value,
            Occupants: showNames
                ? booked.Select(occupant => new Occupant(occupant.Employee, occupant.EmployeeName)).ToList()
                : null);
    }
}
