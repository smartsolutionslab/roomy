using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

public sealed class ViewMyReservationsHandler(IMyReservationsReadModel readModel)
    : IQueryHandler<ViewMyReservations, Page<MyReservationView>>
{
    public async Task<Result<Page<MyReservationView>>> HandleAsync(ViewMyReservations query, CancellationToken cancellationToken) =>
        await readModel.GetAsync(query.Employee, query.Page, cancellationToken);
}
