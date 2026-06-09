using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to list the company's bookable rooms for the reserve picker (007 US1). Any authenticated
// employee may book, so there is no actor here; the single-tenant company scopes the catalogue. The
// result lists every known room with its office, both named.
public sealed record ViewBookableRooms(CompanyIdentifier Company)
    : IQuery<IReadOnlyList<BookableRoomView>>;
