using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Features;

public sealed class CreateOfficeTests
{
    [Fact]
    public async Task Creating_an_office_persists_it_under_the_seeded_company_and_commits()
    {
        var company = Company.Create(CompanyName.From("Acme"));
        var offices = new InMemoryOfficeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new CreateOfficeHandler(new SeededCompanyRepository(company), offices, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateOffice(OfficeName.From("HQ"), Location.From("Berlin")),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        offices.Saved.ShouldHaveSingleItem();
        offices.Saved.Single().CompanyIdentifier.ShouldBe(company.Identifier);
        unitOfWork.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task A_duplicate_office_name_is_rejected_and_nothing_is_persisted()
    {
        var company = Company.Create(CompanyName.From("Acme"));
        var offices = new InMemoryOfficeRepository();
        offices.Saved.Add(Office.Create(company.Identifier, OfficeName.From("HQ"), Location.From("Berlin")));
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new CreateOfficeHandler(new SeededCompanyRepository(company), offices, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateOffice(OfficeName.From("HQ"), Location.From("Munich")),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        offices.Saved.Count.ShouldBe(1);
        unitOfWork.Committed.ShouldBeFalse();
    }

    private sealed class SeededCompanyRepository(Company seeded) : ICompanyRepository
    {
        public Task<bool> ExistsAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public Task AddAsync(Company company, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Result<Company>> GetSeededAsync(CancellationToken cancellationToken) =>
            Task.FromResult<Result<Company>>(seeded);
    }

    private sealed class InMemoryOfficeRepository : IOfficeRepository
    {
        public List<Office> Saved { get; } = [];

        public Task<Result<Office>> GetByIdentifierAsync(
            OfficeIdentifier identifier,
            CancellationToken cancellationToken) =>
            Task.FromResult<Result<Office>>(
                Saved.SingleOrDefault(office => office.Identifier == identifier) is { } office
                    ? office
                    : Error.NotFound("office.not_found", "No office has that identifier."));

        public Task<bool> ExistsByNameAsync(
            CompanyIdentifier companyIdentifier,
            OfficeName name,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                Saved.Any(office => office.CompanyIdentifier == companyIdentifier && office.Name == name));

        public Task<IReadOnlyList<Office>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Office>>(Saved);

        public Task AddAsync(Office office, CancellationToken cancellationToken)
        {
            Saved.Add(office);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool Committed { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }
    }
}
