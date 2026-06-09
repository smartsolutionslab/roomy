using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// One bookable room in the catalogue the reserve flow picks from (007 US1): a room in an office, with
// the master data needed to render the office → room picker — the office and room names and the room's
// capacity. Sourced from attendance's own Offices/Rooms read models, never a cross-service join
// (ADR-0014). The flat shape carries its office on every row; the client groups by office.
public sealed record BookableRoomView(
    OfficeIdentifier Office,
    string OfficeName,
    RoomIdentifier Room,
    string RoomName,
    RoomCapacity Capacity);
