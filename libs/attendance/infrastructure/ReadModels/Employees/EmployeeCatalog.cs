using System.Text.Json.Serialization;
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

        var sql = after is { } cursor
            ? (FormattableString)
                $@"SELECT ""employee_id"" AS ""EmployeeId"", ""display_name"" AS ""DisplayName"",
                          word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name""))::double precision AS ""Similarity""
                   FROM ""employees""
                   WHERE immutable_unaccent({query}) <% immutable_unaccent(""display_name"")
                     AND ( word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name"")) < {cursor.Similarity}
                        OR ( word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name"")) = {cursor.Similarity}
                             AND (""display_name"", ""employee_id"") > ({cursor.Name}, {cursor.EmployeeId}) ) )
                   ORDER BY word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name"")) DESC, ""display_name"", ""employee_id""
                   LIMIT {probeLimit}"
            : $@"SELECT ""employee_id"" AS ""EmployeeId"", ""display_name"" AS ""DisplayName"",
                        word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name""))::double precision AS ""Similarity""
                 FROM ""employees""
                 WHERE immutable_unaccent({query}) <% immutable_unaccent(""display_name"")
                 ORDER BY word_similarity(immutable_unaccent({query}), immutable_unaccent(""display_name"")) DESC, ""display_name"", ""employee_id""
                 LIMIT {probeLimit}";

        var rows = await context.Database
            .SqlQuery<EmployeeSearchRow>(sql)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var hasMore = rows.Count > request.Limit;
        var pageRows = hasMore ? rows.Take(request.Limit).ToList() : rows;
        var items = pageRows
            .Select(row => new EmployeeView(EmployeeIdentifier.From(row.EmployeeId), row.DisplayName))
            .ToList();
        var nextCursor = hasMore
            ? CursorCodec.Encode(new EmployeeSearchCursor(pageRows[^1].Similarity, pageRows[^1].DisplayName, pageRows[^1].EmployeeId))
            : null;

        return new Page<EmployeeView>(items, nextCursor);
    }
}

// The opaque cursor for the unfiltered directory: the (name, id) of the last returned employee (ADR-0044).
// The id breaks ties so duplicate display names still page deterministically. Disallow unmapped members so a
// search cursor (which also carries a similarity) replayed with a blank q fails to decode — a 400, not a
// silent wrong-mode read (ADR-0047 §2).
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EmployeeCursor(string Name, Guid EmployeeId);

// The opaque cursor for a name search: the (similarity, name, id) of the last returned employee (ADR-0047).
// Similarity is the primary descending key; (name, id) breaks the frequent similarity ties into a stable total
// order. Similarity is required and unmapped members are disallowed, so an unfiltered cursor replayed with a
// query (no similarity) — or any cursor of the wrong shape — fails to decode and is rejected as a malformed
// cursor (ADR-0044 path).
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record EmployeeSearchCursor(
    [property: JsonRequired] double Similarity,
    string Name,
    Guid EmployeeId);

// The projected search row: the employee plus its computed word-similarity, used to build the next cursor.
internal sealed record EmployeeSearchRow(Guid EmployeeId, string DisplayName, double Similarity);
