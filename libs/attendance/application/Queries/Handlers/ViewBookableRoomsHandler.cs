using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

// Lists the company's bookable rooms (007 US1): a straight read of the catalogue read model through the
// port. There is nothing to decide — a company with no rooms yields an empty list, never "not found" —
// so the handler returns what the read model holds. The SQL (the office/room join) is covered by the
// read-model integration tests.
public sealed class ViewBookableRoomsHandler(IBookableRoomsReadModel readModel)
    : IQueryHandler<ViewBookableRooms, IReadOnlyList<BookableRoomView>>
{
    public async Task<Result<IReadOnlyList<BookableRoomView>>> HandleAsync(
        ViewBookableRooms query,
        CancellationToken cancellationToken)
    {
        var rooms = await readModel.GetAsync(query.Company, cancellationToken).ConfigureAwait(false);
        return Result.Success(rooms);
    }
}
