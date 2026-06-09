using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// Derives the AttendanceDay stream id deterministically from its identity (CompanyId + Date, ADR-0026)
// so the same company-day always maps to the same stream without a lookup table (research R5). It is a
// name-based id: a SHA-256 digest of a stable name, truncated to 16 bytes. SHA-256 (not the RFC-4122
// v5 SHA-1) is used deliberately — SHA-1 is flagged as a weak algorithm and this is identity
// derivation, not security, so collision resistance is all that matters.
public static class AttendanceDayStreamId
{
    public static StreamId For(CompanyIdentifier company, BookingDate date)
    {
        var name = string.Create(
            CultureInfo.InvariantCulture,
            $"attendance-day:{company.Value:N}:{date.Value:yyyy-MM-dd}");

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(name));

        return StreamId.From(new Guid(digest.AsSpan(0, 16)));
    }
}
