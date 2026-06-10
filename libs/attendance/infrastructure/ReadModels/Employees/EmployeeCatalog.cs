using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The IEmployeeCatalog adapter (009): lists the employees from attendance's local Employees read model,
// ordered by display name for the on-behalf picker — attendance's own read model, fed by EmployeeHired
// (ADR-0014/0031), never a cross-context join. Keyset-paginated by (display_name, employee_id) so the
// cursor is a stable total order even when names collide (ADR-0044). The keyset predicate is a
// parameterized PostgreSQL row-value comparison (FromSql): Npgsql does not translate string.Compare,
// but PostgreSQL compares the `(text, uuid)` tuple natively, so the page is one indexed scan.
public sealed class EmployeeCatalog(AttendanceDbContext context) : IEmployeeCatalog
{
    public async Task<Result<Page<EmployeeView>>> GetAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var decoded = request.DecodeCursor<EmployeeCursor>();
        if (decoded.IsFailure)
        {
            return decoded.Error;
        }

        var probeLimit = request.Limit + 1;
        var rows = decoded.Value is { } after
            ? await context.Employees
                .FromSql(
                    $@"SELECT * FROM ""employees"" WHERE (""display_name"", ""employee_id"") > ({after.Name}, {after.EmployeeId}) ORDER BY ""display_name"", ""employee_id"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await context.Employees
                .FromSql($@"SELECT * FROM ""employees"" ORDER BY ""display_name"", ""employee_id"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var hasMore = rows.Count > request.Limit;
        var pageRows = hasMore ? rows.Take(request.Limit).ToList() : rows;
        var items = pageRows
            .Select(row => new EmployeeView(EmployeeIdentifier.From(row.EmployeeId), row.DisplayName))
            .ToList();
        var nextCursor = hasMore
            ? CursorCodec.Encode(new EmployeeCursor(pageRows[^1].DisplayName, pageRows[^1].EmployeeId))
            : null;

        return new Page<EmployeeView>(items, nextCursor);
    }
}

// The opaque cursor for the employee directory: the (name, id) of the last returned employee
// (ADR-0044). The id breaks ties so duplicate display names still page deterministically.
internal sealed record EmployeeCursor(string Name, Guid EmployeeId);
