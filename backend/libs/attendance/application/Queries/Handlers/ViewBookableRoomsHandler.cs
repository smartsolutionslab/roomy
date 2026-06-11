using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

public sealed class ViewBookableRoomsHandler(IBookableRoomsReadModel readModel)
    : IQueryHandler<ViewBookableRooms, IReadOnlyList<BookableRoomView>>
{
    public async Task<Result<IReadOnlyList<BookableRoomView>>> HandleAsync(ViewBookableRooms query, CancellationToken cancellationToken)
    {
        var rooms = await readModel.GetAsync(query.Company, cancellationToken);
        return Result.Success(rooms);
    }
}
