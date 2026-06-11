using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;

public sealed class ReservePlaceHandler(IAttendanceDayRepository attendanceDays, IRoomDirectory rooms, IBusinessClock clock)
    : ICommandHandler<ReservePlace, ReservationIdentifier>
{
    private const int MaxAttempts = 3;

    public async Task<Result<ReservationIdentifier>> HandleAsync(ReservePlace command, CancellationToken cancellationToken)
    {
        var (company, employee, actor, office, roomIdentifier, bookingDate, actorIsAdmin) = command;

        if (employee != actor && !actorIsAdmin)
            return Error.Forbidden("not_authorized", "Only an administrator may reserve on behalf of another employee.");

        var capacity = await rooms.FindCapacityAsync(roomIdentifier, cancellationToken);
        if (capacity.IsFailure) return capacity.Error;

        var occurredAt = clock.Now;
        var today = clock.Today;
        var room = RoomReference.From(office, roomIdentifier);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var attendanceDay = await attendanceDays.LoadAsync(company, bookingDate, cancellationToken);

            var decision = attendanceDay.Reserve(employee, room, capacity.Value, today, occurredAt);
            if (decision.IsFailure) return decision.Error;

            var saved = await attendanceDays.SaveAsync(attendanceDay, cancellationToken);
            if (saved.IsSuccess) return decision.Value;
        }

        return Error.Conflict("concurrency_retry_exhausted", "The day was changed concurrently too many times; please retry.");
    }
}
