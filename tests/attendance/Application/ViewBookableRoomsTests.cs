using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The bookable-rooms use case is a straight read of the catalogue read model (007 US1): the handler
// returns exactly what the port holds, wrapped in a success Result, with no decision of its own. The SQL
// — the office/room join and the company scope — is covered by the read-model integration tests.
public class ViewBookableRoomsTests
{
    [Fact]
    public async Task It_returns_the_read_models_bookable_rooms_for_the_company()
    {
        var company = CompanyIdentifier.New();
        var room = new BookableRoomView(
            OfficeIdentifier.New(),
            "Munich",
            RoomIdentifier.New(),
            "A1",
            RoomCapacity.From(8));
        var handler = new ViewBookableRoomsHandler(new StubReadModel(company, [room]));

        var result = await handler.HandleAsync(new ViewBookableRooms(company), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().ShouldBe(room);
    }

    [Fact]
    public async Task A_company_with_no_rooms_gets_an_empty_list()
    {
        var company = CompanyIdentifier.New();
        var handler = new ViewBookableRoomsHandler(new StubReadModel(company, []));

        var result = await handler.HandleAsync(new ViewBookableRooms(company), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    private sealed class StubReadModel(CompanyIdentifier expected, IReadOnlyList<BookableRoomView> rooms)
        : IBookableRoomsReadModel
    {
        public Task<IReadOnlyList<BookableRoomView>> GetAsync(CompanyIdentifier company, CancellationToken cancellationToken)
        {
            company.ShouldBe(expected);
            return Task.FromResult(rooms);
        }
    }
}
