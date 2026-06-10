using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Lists a page of an employee's own reservations (FR-004, ADR-0042): a straight read of the local
// Reservations read model through the port. There is nothing to decide here — an empty page is not
// "not found" — so the handler returns what the read model holds (a malformed cursor surfaces as the
// read model's validation failure).
public sealed class ViewMyReservationsHandler(IMyReservationsReadModel readModel)
    : IQueryHandler<ViewMyReservations, Page<MyReservationView>>
{
    public Task<Result<Page<MyReservationView>>> HandleAsync(
        ViewMyReservations query,
        CancellationToken cancellationToken) =>
        readModel.GetAsync(query.Employee, query.Page, cancellationToken);
}
