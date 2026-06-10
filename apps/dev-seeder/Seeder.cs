using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using AttCompanyId = SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.CompanyIdentifier;
using IdentityUserId = SmartSolutionsLab.Roomy.Identity.Domain.Users.UserIdentifier;
using OrgCompanyId = SmartSolutionsLab.Roomy.Organization.Domain.Companies.CompanyIdentifier;
using OrgEmployee = SmartSolutionsLab.Roomy.Organization.Domain.Employees.Employee;
using OrgEmployeeName = SmartSolutionsLab.Roomy.Organization.Domain.Employees.EmployeeName;
using OrgEmployeeRole = SmartSolutionsLab.Roomy.Organization.Domain.Employees.EmployeeRole;
using OrgUserId = SmartSolutionsLab.Roomy.Organization.Domain.Employees.UserIdentifier;
using OrgWorkEmail = SmartSolutionsLab.Roomy.Organization.Domain.Employees.WorkEmail;
using ReadModelEmployee = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees.Employee;
using ReadModelOffice = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices.Office;
using ReadModelRoom = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms.Room;

namespace SmartSolutionsLab.Roomy.DevSeeder;

// Writes the Obex Labs demo dataset directly into the dev databases. Organization aggregates use their
// domain factories; each colleague is provisioned in Keycloak + identity so they can actually log in
// (ADR-0025 done synchronously here, not via the saga). Historical reservations are appended straight to
// the attendance event store — the booking-window rule lives in the aggregate, not the store, so seeding
// past dates is exactly why this bypasses the API — then the Reservations read model is rebuilt.
internal sealed class Seeder(
    OrganizationDbContext organizationDb,
    IdentityDbContext identityDb,
    AttendanceDbContext attendanceDb,
    IIdentityProviderPort identityProvider,
    IEventStore eventStore,
    ReservationsReadModelRebuilder rebuilder,
    SeedOptions options,
    ILogger<Seeder> logger)
{
    private static readonly DateOnly start = new(2025, 1, 1);
    private static readonly DateOnly end = new(2026, 6, 15);

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var companyId = options.CompanyId;

        if (await organizationDb.Offices.AnyAsync(cancellationToken))
        {
            logger.LogWarning("Demo offices already exist — the Obex Labs dataset looks seeded. Aborting.");
            return;
        }

        // Drop the placeholder company the org host auto-seeds at startup, so Obex Labs owns the configured
        // CompanyId that attendance is wired to. At this point it has no offices/employees, so it deletes clean.
        await organizationDb.Companies.ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Seeding company {Company} ({CompanyId}).", SeedData.CompanyName, companyId);
        organizationDb.Companies.Add(Company.Create(OrgCompanyId.From(companyId), CompanyName.From(SeedData.CompanyName)));

        var offices = SeedOffices(companyId);
        await organizationDb.SaveChangesAsync(cancellationToken);

        var employees = await SeedEmployeesAsync(companyId, offices, cancellationToken);
        SeedAttendanceReadModels(companyId, offices, employees);
        await attendanceDb.SaveChangesAsync(cancellationToken);

        await SeedReservationsAsync(companyId, offices, employees, cancellationToken);
        await rebuilder.RebuildAsync(cancellationToken);

        logger.LogInformation("Seed complete: {Offices} offices, {Employees} employees.", offices.Count, employees.Count);
    }

    private List<OfficeData> SeedOffices(Guid companyId)
    {
        var company = OrgCompanyId.From(companyId);
        var result = new List<OfficeData>();
        foreach (var seed in SeedData.Offices)
        {
            var office = Office.Create(company, OfficeName.From(seed.Name), Location.From(seed.Location));
            var rooms = new List<RoomData>();
            foreach (var room in seed.Rooms)
            {
                var added = office.AddRoom(RoomName.From(room.Name), Capacity.From(room.Capacity));
                if (added.IsFailure)
                {
                    throw new InvalidOperationException($"Could not add room {room.Name} to {seed.Name}: {added.Error.Message}");
                }

                rooms.Add(new RoomData(added.Value.Identifier.Value, room.Name, room.Capacity));
            }

            organizationDb.Offices.Add(office);
            result.Add(new OfficeData(seed.Name, office.Identifier.Value, rooms));
        }

        return result;
    }

    private async Task<List<EmployeeData>> SeedEmployeesAsync(
        Guid companyId, List<OfficeData> offices, CancellationToken cancellationToken)
    {
        var company = OrgCompanyId.From(companyId);
        var result = new List<EmployeeData>();
        foreach (var seed in SeedData.Employees)
        {
            var userId = Guid.CreateVersion7();
            var email = $"{Slug(seed.DisplayName)}@{SeedData.EmailDomain}";

            var provisioned = await identityProvider.ProvisionUserAsync(
                Email.From(email), DisplayName.From(seed.DisplayName), options.EmployeePassword, Role.Employee, cancellationToken);
            if (provisioned.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Keycloak provisioning failed for {email}: {provisioned.Error.Code} {provisioned.Error.Message}");
            }

            var user = User.Register(IdentityUserId.From(userId), Email.From(email), DisplayName.From(seed.DisplayName), Role.Employee);
            user.Activate(provisioned.Value);
            identityDb.Users.Add(user);

            var employee = OrgEmployee.Hire(
                company, OrgUserId.From(userId), OrgEmployeeName.From(seed.DisplayName), OrgWorkEmail.From(email),
                OrgEmployeeRole.Employee, options.EmployeePassword);
            employee.CompleteProvisioning();
            organizationDb.Employees.Add(employee);

            result.Add(new EmployeeData(employee.Identifier.Value, userId, seed.DisplayName, seed.Office));
        }

        await identityDb.SaveChangesAsync(cancellationToken);
        await organizationDb.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Provisioned {Count} employees in Keycloak + identity.", result.Count);
        return result;
    }

    private void SeedAttendanceReadModels(Guid companyId, List<OfficeData> offices, List<EmployeeData> employees)
    {
        foreach (var office in offices)
        {
            attendanceDb.Offices.Add(new ReadModelOffice { OfficeId = office.OfficeId, CompanyId = companyId, Name = office.Name });
            foreach (var room in office.Rooms)
            {
                attendanceDb.Rooms.Add(new ReadModelRoom
                {
                    RoomId = room.RoomId,
                    OfficeId = office.OfficeId,
                    CompanyId = companyId,
                    Capacity = room.Capacity,
                    Name = room.Name,
                });
            }
        }

        foreach (var employee in employees)
        {
            attendanceDb.Employees.Add(new ReadModelEmployee
            {
                EmployeeId = employee.EmployeeId,
                UserId = employee.UserId,
                DisplayName = employee.DisplayName,
            });
        }
    }

    private async Task SeedReservationsAsync(
        Guid companyId, List<OfficeData> offices, List<EmployeeData> employees, CancellationToken cancellationToken)
    {
        var company = AttCompanyId.From(companyId);
        var random = new Random(20260610);
        var byOffice = employees.GroupBy(employee => employee.Office).ToDictionary(group => group.Key, group => group.ToList());
        var total = (end.DayNumber - start.DayNumber) + 1;
        var placed = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            var dayEvents = new List<object>();
            foreach (var office in offices)
            {
                if (!byOffice.TryGetValue(office.Name, out var staff) || staff.Count == 0)
                {
                    continue;
                }

                var target = TargetReservations(office, staff.Count, date, total, random);
                AssignDay(companyId, office, staff, date, target, random, dayEvents);
            }

            if (dayEvents.Count == 0)
            {
                continue;
            }

            var streamId = AttendanceDayStreamId.For(company, BookingDate.From(date));
            await eventStore.AppendAsync(streamId, StreamVersion.None, dayEvents, EventMetadata.None, cancellationToken);
            placed += dayEvents.Count;
        }

        logger.LogInformation("Appended {Count} reservations across {Days} days.", placed, total);
    }

    // Daily occupancy as a fraction of office capacity (5-40%), midweek-heavy with a gentle upward trend.
    // Hamburg is "more or less not present" — only the odd colleague, the odd day.
    private static int TargetReservations(OfficeData office, int staffCount, DateOnly date, int totalDays, Random random)
    {
        var capacity = office.Rooms.Sum(room => room.Capacity);
        if (office.Name == "Hamburg")
        {
            return random.NextDouble() < 0.15 ? Math.Min(random.Next(1, 3), Math.Min(staffCount, capacity)) : 0;
        }

        var weekday = date.DayOfWeek switch
        {
            DayOfWeek.Monday => 0.7,
            DayOfWeek.Tuesday => 1.0,
            DayOfWeek.Wednesday => 1.0,
            DayOfWeek.Thursday => 0.9,
            _ => 0.5,
        };
        var trend = 0.6 + (0.4 * ((date.DayNumber - start.DayNumber) / (double)totalDays));
        var jitter = 0.75 + (random.NextDouble() * 0.5);
        var fraction = Math.Clamp(0.40 * weekday * trend * jitter, 0.05, 0.40);
        return Math.Min((int)Math.Round(capacity * fraction), Math.Min(staffCount, capacity));
    }

    private static void AssignDay(
        Guid companyId, OfficeData office, List<EmployeeData> staff, DateOnly date,
        int target, Random random, List<object> dayEvents)
    {
        if (target <= 0)
        {
            return;
        }

        var chosen = staff.OrderBy(_ => random.Next()).Take(target).ToList();
        var remaining = office.Rooms.ToDictionary(room => room.RoomId, room => room.Capacity);
        var occurredAt = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(1));

        foreach (var employee in chosen)
        {
            var room = office.Rooms.FirstOrDefault(room => remaining[room.RoomId] > 0);
            if (room is null)
            {
                break;
            }

            remaining[room.RoomId]--;
            dayEvents.Add(new ReservationPlaced(
                Guid.CreateVersion7(), companyId, date, employee.EmployeeId, office.OfficeId, room.RoomId, occurredAt));
        }
    }

    private static string Slug(string displayName)
    {
        var lowered = displayName.Trim().ToLowerInvariant().Replace("'", string.Empty);
        var slug = string.Concat(lowered.Select(character => character == ' ' ? '.' : character))
            .Where(character => char.IsLetterOrDigit(character) || character is '.' or '-');
        return new string([.. slug]);
    }

    private sealed record OfficeData(string Name, Guid OfficeId, List<RoomData> Rooms);

    private sealed record RoomData(Guid RoomId, string Name, int Capacity);

    private sealed record EmployeeData(Guid EmployeeId, Guid UserId, string DisplayName, string Office);
}
