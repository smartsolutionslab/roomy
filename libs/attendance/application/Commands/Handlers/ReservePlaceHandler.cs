using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;

// Reserves a place (FR-001..007). It reads the room's capacity, then runs the aggregate's decision
// inside a bounded optimistic-retry loop (research R2 / ADR-0039): load → decide → save. A domain
// rejection (room full, duplicate day, not bookable) returns immediately; only a save-time concurrency
// conflict retries — reloading so the loser of the last-place race (scenario 12) re-decides against
// fresh state. "Today" is the Europe/Berlin calendar day and OccurredAt the instant, both from the
// injected TimeProvider, so the domain stays clock-free (research R4).
public sealed class ReservePlaceHandler(IAttendanceDayRepository attendanceDays, IRoomDirectory rooms, TimeProvider timeProvider)
    : ICommandHandler<ReservePlace, ReservationIdentifier>
{
    private const int MaxAttempts = 3;

    private static readonly TimeZoneInfo berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public async Task<Result<ReservationIdentifier>> HandleAsync(ReservePlace command, CancellationToken cancellationToken)
    {
        var (company, employee, office, roomIdentifier, bookingDate) = command;
        var capacity = await rooms.FindCapacityAsync(roomIdentifier, cancellationToken);
        if (capacity.IsFailure) return capacity.Error;

        var occurredAt = timeProvider.GetUtcNow();
        var today = BookingDate.From(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(occurredAt, berlinZone).DateTime));
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
