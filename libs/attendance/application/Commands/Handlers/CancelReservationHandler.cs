using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;

// Cancels a reservation (FR-008/009). Like reserve, it runs the aggregate's decision inside a bounded
// optimistic-retry loop (research R2 / ADR-0039): load → cancel → save. A domain rejection (not found,
// past day, not owner) returns immediately; only a save-time concurrency conflict retries, reloading so
// the decision re-runs against fresh state. "Today" is the Europe/Berlin calendar day from the injected
// TimeProvider, so the domain stays clock-free (research R4).
public sealed class CancelReservationHandler(IAttendanceDayRepository attendanceDays, TimeProvider timeProvider)
    : ICommandHandler<CancelReservation>
{
    private const int MaxAttempts = 3;

    private static readonly TimeZoneInfo berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public async Task<Result> HandleAsync(CancelReservation command, CancellationToken cancellationToken)
    {
        var occurredAt = timeProvider.GetUtcNow();
        var today = BookingDate.From(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(occurredAt, berlinZone).DateTime));

        var (company, reservation, bookingDate, actor, actorIsAdmin) = command;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var attendanceDay = await attendanceDays.LoadAsync(company, bookingDate, cancellationToken);

            var decision = attendanceDay.Cancel(reservation, actor, actorIsAdmin, today, occurredAt);
            if (decision.IsFailure) return decision.Error;

            var saved = await attendanceDays.SaveAsync(attendanceDay, cancellationToken).ConfigureAwait(false);
            if (saved.IsSuccess) return Result.Success();
        }

        return Error.Conflict("concurrency_retry_exhausted", "The day was changed concurrently too many times; please retry.");
    }
}
