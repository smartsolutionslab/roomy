using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class OfficePersistenceTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();

    private async Task PersistAsync(Office office)
    {
        await using var context = fixture.CreateContext();
        await new OfficeRepository(context).AddAsync(office, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Round_trips_an_office_with_its_rooms_and_derived_capacity()
    {
        var office = Office.Create(company, OfficeName.From("HQ Berlin"), Location.From("Berlin"));
        office.AddRoom(RoomName.From("Aurora"), Capacity.From(8));
        office.AddRoom(RoomName.From("Borealis"), Capacity.From(4));
        await PersistAsync(office);

        await using var context = fixture.CreateContext();
        var found = await new OfficeRepository(context).GetByIdentifierAsync(office.Identifier, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.Name.ShouldBe(OfficeName.From("HQ Berlin"));
        found.Value.Location.ShouldBe(Location.From("Berlin"));
        found.Value.Rooms.Count.ShouldBe(2);
        found.Value.Capacity.ShouldBe(12);
    }

    [Fact]
    public async Task ExistsByName_reflects_whether_the_office_name_is_taken_in_the_company()
    {
        await PersistAsync(Office.Create(company, OfficeName.From("Taken Office"), Location.From("Munich")));

        await using var context = fixture.CreateContext();
        var offices = new OfficeRepository(context);

        (await offices.ExistsByNameAsync(company, OfficeName.From("Taken Office"), TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await offices.ExistsByNameAsync(company, OfficeName.From("Absent Office"), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdentifier_returns_NotFound_for_an_unknown_office()
    {
        await using var context = fixture.CreateContext();
        var found = await new OfficeRepository(context).GetByIdentifierAsync(OfficeIdentifier.New(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Rejects_two_offices_with_the_same_name_in_the_company()
    {
        await PersistAsync(Office.Create(company, OfficeName.From("Duplicate"), Location.From("Berlin")));

        await Should.ThrowAsync<DbUpdateException>(() => PersistAsync(Office.Create(company, OfficeName.From("Duplicate"), Location.From("Munich"))));
    }
}
