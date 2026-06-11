using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal static class ResponseMappings
{
    extension(ReservationView reservation)
    {
        public Response.Reservation ToResponse() =>
            new(
                reservation.Reservation.Value,
                reservation.Office.Value,
                reservation.Room.Value,
                reservation.Date.Value,
                reservation.Employee.Value);
    }

    extension(IReadOnlyList<ReservationView> reservations)
    {
        public Response.Page.Reservation ToResponse() =>
            new(reservations.Select(reservation => reservation.ToResponse()).ToList(), NextCursor: null);
    }

    extension(Request.Reserve request)
    {
        public Response.Reservation ToResponse(ReservationIdentifier reservation, EmployeeIdentifier employee) =>
            new(reservation.Value, request.OfficeId, request.RoomId, request.Date, employee.Value);
    }

    extension(MyReservationView reservation)
    {
        public Response.MyReservation ToResponse() =>
            new(
                reservation.Reservation.Value,
                reservation.Office.Value,
                reservation.OfficeName,
                reservation.Room.Value,
                reservation.RoomName,
                reservation.Date.Value);
    }

    extension(Page<MyReservationView> page)
    {
        public Response.Page.MyReservation ToResponse() =>
            new(page.Items.Select(reservation => reservation.ToResponse()).ToList(), page.NextCursor);
    }

    extension(EmployeeView employee)
    {
        public Response.Employee ToResponse() =>
            new(employee.Employee.Value, employee.Name);
    }

    extension(Page<EmployeeView> page)
    {
        public Response.Page.Employee ToResponse() =>
            new(page.Items.Select(employee => employee.ToResponse()).ToList(), page.NextCursor);
    }

    extension(BookableRoomView room)
    {
        public Response.BookableRoom ToResponse() =>
            new(room.Office.Value, room.OfficeName, room.Room.Value, room.RoomName, room.Capacity.Value);
    }

    extension(OccupancyView day)
    {
        public Response.OccupancyDay ToResponse() =>
            new(day.Date.Value, day.Office.ToResponse(), day.Rooms.Select(room => room.ToResponse()).ToList());
    }

    extension(OfficeOccupancy office)
    {
        public Response.OfficeOccupancy ToResponse() =>
            new(office.Office.Value, office.Name, office.Occupied, office.Capacity, office.IsFull);
    }

    extension(RoomOccupancy room)
    {
        public Response.RoomOccupancy ToResponse() =>
            new(
                room.Room.Value,
                room.Name,
                room.Occupied,
                room.Capacity,
                room.IsFull,
                room.Occupants?.Select(occupant => occupant.ToResponse()).ToList());
    }

    extension(Occupant occupant)
    {
        public Response.Occupant ToResponse() =>
            new(occupant.Employee.Value, occupant.Name);
    }
}
