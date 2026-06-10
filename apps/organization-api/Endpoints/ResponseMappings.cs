using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

internal static class ResponseMappings
{
    extension(Office office)
    {
        public Response.Office ToResponse() =>
            new(
                office.Identifier.Value,
                office.Name.Value,
                office.Location.Value,
                office.Capacity,
                office.Rooms.Select(room => room.ToResponse()).ToList());
    }

    extension(Room room)
    {
        public Response.Room ToResponse() =>
            new(room.Identifier.Value, room.Name.Value, room.Capacity);
    }

    // Hiring is eventually consistent: the response reports the recorded employee and the pre-allocated
    // login id, with provisioning just started — never yet Active (ADR-0025).
    extension(HiredEmployee hired)
    {
        public Response.HiredEmployee ToResponse() =>
            new(hired.Employee.Value, hired.User.Value, ProvisioningState.Provisioning.ToString());
    }
}
