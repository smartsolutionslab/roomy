using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The IEmployeeCatalog adapter (009, 012): lists/searches the employees from attendance's local Employees
// read model — its own read model fed by EmployeeHired (ADR-0014/0031), never a cross-context join. A blank
// term keeps the existing (display_name, employee_id) keyset (ADR-0044). A non-blank term searches by pg_trgm
// word-similarity over the accent-folded trigram index and ranks most-similar first, paging on the
// (word_similarity DESC, display_name, employee_id) keyset (ADR-0047) — a new opaque cursor shape under the
// same ADR-0044 contract. The keyset predicate is a parameterized PostgreSQL row-value comparison: Npgsql does
// not translate string.Compare, but PostgreSQL compares the `(text, uuid)` tuple natively, so each page is one
// indexed scan.
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

    public Task<Result<Page<EmployeeView>>> GetAsync(
        SearchTerm term,
        PageRequest request,
        CancellationToken cancellationToken) =>
        term.IsEmpty
            ? ListAsync(request, cancellationToken)
            : SearchAsync(term.Value, request, cancellationToken);

    private async Task<Result<Page<EmployeeView>>> ListAsync(
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

        return Page<EmployeeView>.FromProbe(
            rows,
            request.Limit,
            row => new EmployeeView(EmployeeIdentifier.From(row.EmployeeId), row.DisplayName),
            row => new EmployeeCursor(row.DisplayName, row.EmployeeId));
    }

    private async Task<Result<Page<EmployeeView>>> SearchAsync(
        string query,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var decoded = request.DecodeCursor<EmployeeSearchCursor>();
        if (decoded.IsFailure)
        {
            return decoded.Error;
        }

        var after = decoded.Value;
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

        // word_similarity is computed once in the ranked subquery and referenced by alias in the outer
        // keyset/order — PostgreSQL flattens this simple derived table, so the `<%` pre-filter still hits
        // the trigram GIN index. Only the outer keyset predicate differs between the first and later pages.
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
