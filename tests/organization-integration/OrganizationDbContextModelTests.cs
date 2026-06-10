using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class OrganizationDbContextModelTests
{
    private static OrganizationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql("Host=localhost;Database=organization;Username=postgres;Password=postgres")
            .Options;

        return new OrganizationDbContext(options);
    }

    [Fact]
    public void Maps_companies_offices_and_owned_rooms_to_snake_case_tables()
    {
        using var context = BuildContext();

        var tables = context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToList();

        tables.ShouldContain("companies");
        tables.ShouldContain("offices");
        tables.ShouldContain("rooms");
    }

    [Fact]
    public void Enforces_unique_office_and_room_names_with_indexes()
    {
        using var context = BuildContext();

        var uniqueIndexNames = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes())
            .Where(index => index.IsUnique)
            .Select(index => index.GetDatabaseName())
            .ToList();

        uniqueIndexNames.ShouldContain("ux_offices_company_identifier_name");
        uniqueIndexNames.ShouldContain("ux_rooms_office_identifier_name");
    }

    [Fact]
    public void Does_not_store_the_derived_office_capacity()
    {
        using var context = BuildContext();

        var officeProperties = context.Model.FindEntityType(typeof(Office))
            ?.GetProperties()
            .Select(property => property.Name) ?? [];

        officeProperties.ShouldNotContain(nameof(Office.Capacity));
    }
}
