using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

public static class OptimisticWrite
{
    public const int MaxAttempts = 3;

    public static async Task<Result<TResult>> ExecuteAsync<TResult>(
        Func<Task<AttendanceDay>> load,
        Func<AttendanceDay, Result<TResult>> decide,
        Func<AttendanceDay, Task<Result>> save)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var attendanceDay = await load();

            var decision = decide(attendanceDay);
            if (decision.IsFailure) return decision.Error;

            var saved = await save(attendanceDay);
            if (saved.IsSuccess) return decision.Value;
        }

        return RetryExhausted;
    }

    public static async Task<Result> ExecuteAsync(
        Func<Task<AttendanceDay>> load,
        Func<AttendanceDay, Result> decide,
        Func<AttendanceDay, Task<Result>> save)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var attendanceDay = await load();

            var decision = decide(attendanceDay);
            if (decision.IsFailure) return decision.Error;

            var saved = await save(attendanceDay);
            if (saved.IsSuccess) return Result.Success();
        }

        return RetryExhausted;
    }

    private static Error RetryExhausted =>
        Error.Conflict("concurrency_retry_exhausted", "The day was changed concurrently too many times; please retry.");
}
