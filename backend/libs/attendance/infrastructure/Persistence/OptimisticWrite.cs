using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

public static class OptimisticWrite
{
    public const int MaxAttempts = 3;

    private const string ConcurrencyConflict = "concurrency_conflict";

    public static async Task<Result<TResult>> ExecuteAsync<TResult>(
        Func<Task<AttendanceDay>> load,
        Func<AttendanceDay, Result<TResult>> decide,
        Func<AttendanceDay, Task<Result>> save,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attendanceDay = await load().ConfigureAwait(false);

            var decision = decide(attendanceDay);
            if (decision.IsFailure) return decision.Error;

            var saved = await save(attendanceDay).ConfigureAwait(false);
            if (saved.IsSuccess) return decision.Value;

            // Retry only a genuine optimistic-concurrency conflict; surface any other save failure
            // verbatim rather than reloading and ultimately relabelling it concurrency_retry_exhausted.
            if (saved.Error.Code != ConcurrencyConflict) return saved.Error;
        }

        return RetryExhausted;
    }

    public static async Task<Result> ExecuteAsync(
        Func<Task<AttendanceDay>> load,
        Func<AttendanceDay, Result> decide,
        Func<AttendanceDay, Task<Result>> save,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            load,
            attendanceDay => decide(attendanceDay).Match<Result<bool>>(() => true, error => error),
            save,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Result.Success() : result.Error;
    }

    private static Error RetryExhausted =>
        Error.Conflict("concurrency_retry_exhausted", "The day was changed concurrently too many times; please retry.");
}
