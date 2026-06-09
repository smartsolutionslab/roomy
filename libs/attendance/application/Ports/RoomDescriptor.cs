using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// A room in an occupancy query's scope, with the master data needed to render its figure: the room name
// (for display) and the room's capacity (the denominator). Sourced from attendance's local Rooms read
// model, never a cross-service join (ADR-0014). The office is carried once on OccupancyData, since every
// room in a scope belongs to the same office.
public sealed record RoomDescriptor(
    RoomIdentifier Room,
    string RoomName,
    RoomCapacity Capacity);
