using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public static class OfficeErrors
{
    public static Error NameTaken(OfficeName name) =>
        Error.Conflict("office.name_taken", $"An office named '{name}' already exists.");

    public static Error RoomNameTaken(RoomName name) =>
        Error.Conflict("office.room_name_taken", $"A room named '{name}' already exists in this office.");
}
