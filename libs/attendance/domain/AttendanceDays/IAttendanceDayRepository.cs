using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

public interface IAttendanceDayRepository
{
    Task<AttendanceDay> LoadAsync(CompanyIdentifier company, BookingDate date, CancellationToken cancellationToken);

    Task<Result> SaveAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken);
}
