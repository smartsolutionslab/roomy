using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Tests;

public class OptimisticWriteTests
{
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();
    private static readonly BookingDate date = BookingDate.From(new DateOnly(2026, 6, 8));

    [Fact]
    public async Task A_successful_write_loads_once_saves_once_and_returns_the_value()
    {
        var loads = 0;
        var saves = 0;
        var value = ReservationIdentifier.New();

        var result = await OptimisticWrite.ExecuteAsync(
            Load(() => loads++),
            _ => Result.Success(value),
            _ => Save(() => saves++, Result.Success()));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
        loads.ShouldBe(1);
        saves.ShouldBe(1);
    }

    [Fact]
    public async Task A_decision_failure_is_returned_without_saving_or_retrying()
    {
        var loads = 0;
        var saves = 0;

        var result = await OptimisticWrite.ExecuteAsync(
            Load(() => loads++),
            _ => Result.Failure<ReservationIdentifier>(Error.Conflict("room_full", "full")),
            _ => Save(() => saves++, Result.Success()));

        result.Error.Code.ShouldBe("room_full");
        loads.ShouldBe(1);
        saves.ShouldBe(0);
    }

    [Fact]
    public async Task A_concurrency_conflict_reloads_re_decides_and_can_then_succeed()
    {
        var loads = 0;
        var saves = 0;
        var decisions = 0;
        var saveResults = new Queue<Result>([Conflict(), Result.Success()]);

        var result = await OptimisticWrite.ExecuteAsync(
            Load(() => loads++),
            _ => { decisions++; return Result.Success(ReservationIdentifier.New()); },
            _ => Save(() => saves++, saveResults.Dequeue()));

        result.IsSuccess.ShouldBeTrue();
        loads.ShouldBe(2);
        saves.ShouldBe(2);
        decisions.ShouldBe(2);
    }

    [Fact]
    public async Task Exhausting_the_attempts_returns_a_retryable_conflict()
    {
        var saves = 0;

        var result = await OptimisticWrite.ExecuteAsync(
            Load(() => { }),
            _ => Result.Success(ReservationIdentifier.New()),
            _ => Save(() => saves++, Conflict()));

        result.Error.Code.ShouldBe("concurrency_retry_exhausted");
        saves.ShouldBe(OptimisticWrite.MaxAttempts);
    }

    [Fact]
    public async Task The_non_generic_overload_returns_success_and_exhausts_alike()
    {
        var ok = await OptimisticWrite.ExecuteAsync(
            Load(() => { }),
            _ => Result.Success(),
            _ => Task.FromResult(Result.Success()));
        ok.IsSuccess.ShouldBeTrue();

        var saves = 0;
        var exhausted = await OptimisticWrite.ExecuteAsync(
            Load(() => { }),
            _ => Result.Success(),
            _ => Save(() => saves++, Conflict()));
        exhausted.Error.Code.ShouldBe("concurrency_retry_exhausted");
        saves.ShouldBe(OptimisticWrite.MaxAttempts);
    }

    private static Func<Task<AttendanceDay>> Load(Action onLoad) =>
        () => { onLoad(); return Task.FromResult(AttendanceDay.For(company, date)); };

    private static Task<Result> Save(Action onSave, Result outcome)
    {
        onSave();
        return Task.FromResult(outcome);
    }

    private static Result Conflict() => Error.Conflict("concurrency_conflict", "The day changed concurrently.");
}
