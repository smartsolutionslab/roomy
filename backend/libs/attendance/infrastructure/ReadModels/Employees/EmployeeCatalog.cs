using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

public sealed class EmployeeCatalog(AttendanceDbContext context) : IEmployeeCatalog
{
    // The word-similarity floor for the `<%` pre-filter, tuned so a single-typo fragment keeps the intended
    // name on the first page (SC-002) — pinned by the integration test. pg_trgm's default (0.6) is too strict
    // for short fragments (research R5). Set per request as a transaction-local GUC so it never leaks across
    // pooled connections.
    private const double WordSimilarityThreshold = 0.3;

    // SET LOCAL is a utility statement and cannot be parameterized; the value is a constant we control, so it
    // is formatted once (invariant culture) into a plain command string with no injection surface.
    private static readonly string setThresholdSql =
        FormattableString.Invariant($"SET LOCAL pg_trgm.word_similarity_threshold = {WordSimilarityThreshold}");

    public Task<Page<EmployeeView>> GetAsync(
        SearchTerm term,
        PageRequest request,
        CancellationToken cancellationToken) =>
        term.IsEmpty
            ? ListAsync(request, cancellationToken)
            : SearchAsync(term.Value, request, cancellationToken);

    private async Task<Page<EmployeeView>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var after = request.DecodeCursor<EmployeeCursor>();

        var probeLimit = request.Limit + 1;
        var rows = after is { } cursor
            ? await context.Employees
                .FromSql(
                    $@"SELECT * FROM ""employees"" WHERE (""display_name"", ""employee_id"") > ({cursor.Name}, {cursor.EmployeeId}) ORDER BY ""display_name"", ""employee_id"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : await context.Employees
                .FromSql($@"SELECT * FROM ""employees"" ORDER BY ""display_name"", ""employee_id"" LIMIT {probeLimit}")
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        return Page<EmployeeView>.FromProbe(
            rows,
            request.Limit,
            row => new EmployeeView(EmployeeIdentifier.From(row.EmployeeId), row.DisplayName),
            row => new EmployeeCursor(row.DisplayName, row.EmployeeId));
    }

    private async Task<Page<EmployeeView>> SearchAsync(
        string query,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var after = request.DecodeCursor<EmployeeSearchCursor>();
        var probeLimit = request.Limit + 1;

        // SET LOCAL needs a transaction to scope the threshold to this query; the row-value keyset then pages
        // within the same connection. No execution strategy is configured (ADR-0041), so an explicit
        // transaction is safe here.
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await context.Database
            .ExecuteSqlRawAsync(setThresholdSql, cancellationToken)
            .ConfigureAwait(false);

        var sql = after is { } cursor
            ? (FormattableString)
                $@"SELECT * FROM (
                       SELECT ""employee_id"" AS ""EmployeeId"", ""display_name"" AS ""DisplayName"",
                              word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name""))::double precision AS ""Similarity""
                       FROM ""employees""
                       WHERE immutable_unaccent({query}) <% immutable_unaccent(""display_name"")
                   ) AS ""ranked""
                   WHERE ""Similarity"" < {cursor.Similarity}
                      OR ( ""Similarity"" = {cursor.Similarity}
                           AND (""DisplayName"", ""EmployeeId"") > ({cursor.Name}, {cursor.EmployeeId}) )
                   ORDER BY ""Similarity"" DESC, ""DisplayName"", ""EmployeeId""
                   LIMIT {probeLimit}"
            : $@"SELECT * FROM (
                     SELECT ""employee_id"" AS ""EmployeeId"", ""display_name"" AS ""DisplayName"",
                            word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name""))::double precision AS ""Similarity""
                     FROM ""employees""
                     WHERE immutable_unaccent({query}) <% immutable_unaccent(""display_name"")
                 ) AS ""ranked""
                 ORDER BY ""Similarity"" DESC, ""DisplayName"", ""EmployeeId""
                 LIMIT {probeLimit}";

        var rows = await context.Database
            .SqlQuery<EmployeeSearchRow>(sql)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Page<EmployeeView>.FromProbe(
            rows,
            request.Limit,
            row => new EmployeeView(EmployeeIdentifier.From(row.EmployeeId), row.DisplayName),
            row => new EmployeeSearchCursor(row.Similarity, row.DisplayName, row.EmployeeId));
    }
}
