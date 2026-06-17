using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
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
        var companies = SeededCompany(company);
        var saved = new List<Office>();
        var offices = Substitute.For<IOfficeRepository>();
        offices.ExistsByNameAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<OfficeName>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _ = offices.AddAsync(Arg.Do<Office>(saved.Add), Arg.Any<CancellationToken>());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateOfficeHandler(companies, offices, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateOffice(
                OfficeName.From("HQ"),
                Location.From("Berlin")),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        saved.ShouldHaveSingleItem().CompanyIdentifier.ShouldBe(company.Identifier);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_duplicate_office_name_is_rejected_and_nothing_is_persisted()
    {
        var company = Company.Create(CompanyName.From("Acme"));
        var companies = SeededCompany(company);
        var offices = Substitute.For<IOfficeRepository>();
        offices.ExistsByNameAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<OfficeName>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateOfficeHandler(companies, offices, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateOffice(
                OfficeName.From("HQ"),
                Location.From("Munich")),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        await offices.DidNotReceive().AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ICompanyRepository SeededCompany(Company company)
    {
        var companies = Substitute.For<ICompanyRepository>();
        companies.GetSeededAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(company));
        return companies;
    }
}
