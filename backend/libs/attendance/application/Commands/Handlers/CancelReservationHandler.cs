using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Commands.Handlers;

public sealed class CancelReservationHandler(IAttendanceDayRepository attendanceDays, IBusinessClock clock)
    : ICommandHandler<CancelReservation>
{
    public async Task<Result> HandleAsync(CancelReservation command, CancellationToken cancellationToken)
    {
        var occurredAt = clock.Now;
        var today = clock.Today;

        var (company, reservation, bookingDate, actor, actorIsAdmin) = command;

        return await attendanceDays.MutateAsync(
            company,
            bookingDate,
            attendanceDay => attendanceDay.Cancel(reservation, actor, actorIsAdmin, today, occurredAt),
            cancellationToken);
    }
}
