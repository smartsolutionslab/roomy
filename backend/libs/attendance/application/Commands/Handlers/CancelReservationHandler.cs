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
            attendanceDay =>
            {
                var existing = attendanceDay.Reservations.SingleOrDefault(held => held.Identifier == reservation);
                if (existing is not null && !(actorIsAdmin || existing.Employee == actor))
                {
                    return Error.Forbidden("not_authorized", "Only an administrator may cancel another employee's reservation.");
                }

                return attendanceDay.Cancel(reservation, today, occurredAt);
            },
            cancellationToken);
    }
}
