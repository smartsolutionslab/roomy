using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Features;

public sealed class HireEmployeeTests
{
    private static readonly Company company = Company.Create(CompanyName.From("Acme"));

    [Fact]
    public async Task Hiring_records_a_provisioning_employee_under_the_seeded_company_and_commits()
    {
        var saved = new List<Employee>();
        var employees = Substitute.For<IEmployeeRepository>();
        _ = employees.AddAsync(Arg.Do<Employee>(saved.Add), Arg.Any<CancellationToken>());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new HireEmployeeHandler(SeededCompany(), employees, unitOfWork);

        var result = await handler.HandleAsync(
            new HireEmployee(
                EmployeeName.From("Ada"),
                WorkEmail.From("ada@example.com"),
                EmployeeRole.Employee, "pw"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Employee.Value.ShouldNotBe(Guid.Empty);
        result.Value.User.Value.ShouldNotBe(Guid.Empty);
        var employee = saved.ShouldHaveSingleItem();
        employee.CompanyIdentifier.ShouldBe(company.Identifier);
        employee.UserIdentifier.ShouldBe(result.Value.User);
        employee.State.ShouldBe(ProvisioningState.Provisioning);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_user_registered_ack_activates_the_employee()
    {
        var employee = Hire();
        var employees = Substitute.For<IEmployeeRepository>();
        employees.GetByIdentifierAsync(Arg.Any<EmployeeIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(employee));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CompleteEmployeeProvisioningHandler(employees, unitOfWork);

        var result = await handler.HandleAsync(new CompleteEmployeeProvisioning(employee.Identifier), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.State.ShouldBe(ProvisioningState.Active);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_provisioning_failed_ack_compensates_the_employee()
    {
        var employee = Hire();
        var employees = Substitute.For<IEmployeeRepository>();
        employees.GetByIdentifierAsync(Arg.Any<EmployeeIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(employee));
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new FailEmployeeProvisioningHandler(employees, unitOfWork);

        var result = await handler.HandleAsync(
            new FailEmployeeProvisioning(employee.Identifier, ProvisioningFailureReason.EmailTaken),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.State.ShouldBe(ProvisioningState.Failed);
        employee.FailureReason.ShouldBe(ProvisioningFailureReason.EmailTaken);
    }

    [Fact]
    public async Task An_ack_for_an_unknown_employee_is_not_found()
    {
        var employees = Substitute.For<IEmployeeRepository>();
        employees.GetByIdentifierAsync(Arg.Any<EmployeeIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Employee>(Error.NotFound("employee.not_found", "No employee has that identifier.")));
        var handler = new CompleteEmployeeProvisioningHandler(employees, Substitute.For<IUnitOfWork>());

        var result = await handler.HandleAsync(new CompleteEmployeeProvisioning(EmployeeIdentifier.New()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    private static Employee Hire() =>
        Employee.Hire(
            company.Identifier,
            UserIdentifier.New(),
            EmployeeName.From("Ada"),
            WorkEmail.From("ada@example.com"),
            EmployeeRole.Employee, "pw");

    private static ICompanyRepository SeededCompany()
    {
        var companies = Substitute.For<ICompanyRepository>();
        companies.GetSeededAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(company));
        return companies;
    }
}
