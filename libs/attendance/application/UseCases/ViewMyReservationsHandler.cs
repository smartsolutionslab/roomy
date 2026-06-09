using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Lists an employee's own reservations (FR-004): a straight read of the local Reservations read model
// through the port. There is nothing to decide here — an employee with no reservations yields an empty
// list, never "not found" — so the handler simply returns what the read model holds.
public sealed class ViewMyReservationsHandler(IMyReservationsReadModel readModel)
    : IQueryHandler<ViewMyReservations, IReadOnlyList<MyReservationView>>
{
    public async Task<Result<IReadOnlyList<MyReservationView>>> HandleAsync(
        ViewMyReservations query,
        CancellationToken cancellationToken)
    {
        var reservations = await readModel.GetAsync(query.Employee, cancellationToken).ConfigureAwait(false);
        return Result.Success(reservations);
    }
}
