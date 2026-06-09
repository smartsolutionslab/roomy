using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The persistence port for the AttendanceDay aggregate, owned by the domain (CLAUDE.md). The
// infrastructure implements it over the event store (ADR-0012): Load replays the stream, Save appends
// the uncommitted events at the aggregate's expected version.
public interface IAttendanceDayRepository
{
    // A company-day always exists conceptually — an unbooked one is simply empty — so Load never
    // reports "not found": it returns a fresh aggregate for an empty stream (research R2).
    Task<AttendanceDay> LoadAsync(CompanyIdentifier company, BookingDate date, CancellationToken cancellationToken);

    // Appends the aggregate's uncommitted events, asserting its expected version. The only modelled
    // failure is an optimistic-concurrency conflict, surfaced as Error.Conflict so the application can
    // reload and re-decide (research R2) — the infrastructure's concurrency exception never escapes.
    Task<Result> SaveAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken);
}
