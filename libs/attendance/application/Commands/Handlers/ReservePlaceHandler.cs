using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;

public sealed class ReservePlaceHandler(IAttendanceDayRepository attendanceDays, IRoomDirectory rooms, IBusinessClock clock)
    : ICommandHandler<ReservePlace, ReservationIdentifier>
{
    public async Task<Result<ReservationIdentifier>> HandleAsync(ReservePlace command, CancellationToken cancellationToken)
    {
        var (company, employee, actor, office, roomIdentifier, bookingDate, actorIsAdmin) = command;

        if (employee != actor && !actorIsAdmin)
        {
            return Error.Forbidden("not_authorized", "Only an administrator may reserve on behalf of another employee.");
        }

        var capacity = await rooms.FindCapacityAsync(roomIdentifier, cancellationToken);
        if (capacity.IsFailure) return capacity.Error;

        var occurredAt = clock.Now;
        var today = clock.Today;
        var room = RoomReference.From(office, roomIdentifier);

        return await attendanceDays.MutateAsync(
            company,
            bookingDate,
            attendanceDay => attendanceDay.Reserve(employee, room, capacity.Value, today, occurredAt),
            cancellationToken);
    }
}
