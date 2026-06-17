using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

internal static class AttendanceReadModelErrors
{
    public static Error UnknownRoom() =>
        Error.NotFound("unknown_room", "The room is not known to the attendance service yet.");
}
