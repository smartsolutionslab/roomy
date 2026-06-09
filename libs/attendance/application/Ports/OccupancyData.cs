using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// The raw occupancy data for a scope and date range: the office in scope (named for the rollup), the
// rooms in scope (with capacity and name), and the live reservations across the range. The query handler
// composes these into the per-day figures — counts, the office rollup, the full flag, and the
// today/tomorrow name policy — so this carries data, not decisions. An office with no rooms yet still
// names itself here, so its rollup reads 0/0 rather than blank.
public sealed record OccupancyData(
    OfficeIdentifier Office,
    string OfficeName,
    IReadOnlyList<RoomDescriptor> Rooms,
    IReadOnlyList<OccupantRecord> Occupants);
