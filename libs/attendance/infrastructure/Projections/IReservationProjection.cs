namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;

public interface IReservationProjection
{
    Task ApplyAsync(IReadOnlyList<object> events, CancellationToken cancellationToken);
}
