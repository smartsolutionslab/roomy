using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

public sealed class ViewOccupancyHandler(IOccupancyReadModel readModel, TimeProvider timeProvider)
    : IQueryHandler<ViewOccupancy, IReadOnlyList<OccupancyView>>
{
    private static readonly TimeZoneInfo berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public async Task<Result<IReadOnlyList<OccupancyView>>> HandleAsync(ViewOccupancy query, CancellationToken cancellationToken)
    {
        var (company, scope, range) = query;
        var data = await readModel.GetAsync(company, scope, range, cancellationToken);

        if (data.IsFailure) return data.Error;

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), berlinZone).DateTime);
        var tomorrow = today.AddDays(1);

        var views = new List<OccupancyView>();
        foreach (var date in range.Days())
        {
            var showNames = date.Value == today || date.Value == tomorrow;
            views.Add(BuildDay(date, data.Value, showNames));
        }

        return Result.Success<IReadOnlyList<OccupancyView>>(views);
    }

    private static OccupancyView BuildDay(BookingDate date, OccupancyData data, bool showNames)
    {
        var rooms = data.Rooms.Select(room => BuildRoom(date, room, data.Occupants, showNames)).ToList();

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

    private static RoomOccupancy BuildRoom(BookingDate date, RoomDescriptor room, IReadOnlyList<OccupantRecord> occupants, bool showNames)
    {
        var booked = occupants.Where(occupant => occupant.Room == room.Room && occupant.Date == date)
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
