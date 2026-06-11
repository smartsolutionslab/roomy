using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public interface IAttendanceDayRepository
{
    Task<AttendanceDay> LoadAsync(CompanyIdentifier company, BookingDate date, CancellationToken cancellationToken);

    Task<Result> SaveAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken);

    Task<Result<TResult>> MutateAsync<TResult>(
        CompanyIdentifier company,
        BookingDate date,
        Func<AttendanceDay, Result<TResult>> decide,
        CancellationToken cancellationToken);

    Task<Result> MutateAsync(
        CompanyIdentifier company,
        BookingDate date,
        Func<AttendanceDay, Result> decide,
        CancellationToken cancellationToken);
}
