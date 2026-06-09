namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;

// Stages the read-model row changes for a batch of AttendanceDay stream events onto the shared
// AttendanceDbContext, without saving (ADR-0038). The AttendanceDayRepository calls this immediately
// before the event append, so the append's SaveChanges commits the events and these rows in one
// transaction — keeping the occupancy read model read-your-writes consistent with the write model
// (FR-010). Non-reservation events are ignored, so the mapping is total over the stream.
public interface IReservationProjection
{
    Task ApplyAsync(IReadOnlyList<object> events, CancellationToken cancellationToken);
}
