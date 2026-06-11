using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

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
        var readModel = Substitute.For<IBookableRoomsReadModel>();
        IReadOnlyList<BookableRoomView> rooms = [room];
        readModel.GetAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<CancellationToken>()).Returns(rooms);
        var handler = new ViewBookableRoomsHandler(readModel);

        var result = await handler.HandleAsync(new ViewBookableRooms(company), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().ShouldBe(room);
        await readModel.Received(1).GetAsync(company, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_company_with_no_rooms_gets_an_empty_list()
    {
        var company = CompanyIdentifier.New();
        var readModel = Substitute.For<IBookableRoomsReadModel>();
        IReadOnlyList<BookableRoomView> rooms = [];
        readModel.GetAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<CancellationToken>()).Returns(rooms);
        var handler = new ViewBookableRoomsHandler(readModel);

        var result = await handler.HandleAsync(new ViewBookableRooms(company), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}
