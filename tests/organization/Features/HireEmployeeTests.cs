using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Application;
using SmartSolutionsLab.Roomy.Organization.Application.UseCases;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Features;

// The hire use case and the two saga-ack use cases (ADR-0025), driven against in-memory fakes — the
// publish/consume is exercised by the integration tests. Hiring pre-allocates the login id, records the
// employee in Provisioning, and commits; the acks transition it to Active / Failed (the compensation).
public sealed class HireEmployeeTests
{
    private static readonly Company company = Company.Create(CompanyName.From("Acme"));

    [Fact]
    public async Task Hiring_records_a_provisioning_employee_under_the_seeded_company_and_commits()
    {
        var employees = new InMemoryEmployeeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new HireEmployeeHandler(new SeededCompanyRepository(company), employees, unitOfWork);

        var result = await handler.HandleAsync(
            new HireEmployee(EmployeeName.From("Ada"), WorkEmail.From("ada@example.com"), EmployeeRole.Employee, "pw"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Employee.Value.ShouldNotBe(Guid.Empty);
        result.Value.User.Value.ShouldNotBe(Guid.Empty);
        var saved = employees.Saved.ShouldHaveSingleItem();
        saved.CompanyIdentifier.ShouldBe(company.Identifier);
        saved.UserIdentifier.ShouldBe(result.Value.User);
        saved.State.ShouldBe(ProvisioningState.Provisioning);
        unitOfWork.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task The_user_registered_ack_activates_the_employee()
    {
        var employee = Hire();
        var employees = new InMemoryEmployeeRepository();
        employees.Saved.Add(employee);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new CompleteEmployeeProvisioningHandler(employees, unitOfWork);

        var result = await handler.HandleAsync(new CompleteEmployeeProvisioning(employee.Identifier), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        employee.State.ShouldBe(ProvisioningState.Active);
        unitOfWork.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task The_provisioning_failed_ack_compensates_the_employee()
    {
        var employee = Hire();
        var employees = new InMemoryEmployeeRepository();
        employees.Saved.Add(employee);
        var unitOfWork = new RecordingUnitOfWork();
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
        var handler = new CompleteEmployeeProvisioningHandler(new InMemoryEmployeeRepository(), new RecordingUnitOfWork());

        var result = await handler.HandleAsync(new CompleteEmployeeProvisioning(EmployeeIdentifier.New()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    private static Employee Hire() =>
        Employee.Hire(company.Identifier, UserIdentifier.New(), EmployeeName.From("Ada"), WorkEmail.From("ada@example.com"), EmployeeRole.Employee, "pw");

    private sealed class SeededCompanyRepository(Company seeded) : ICompanyRepository
    {
        public Task<bool> ExistsAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public Task AddAsync(Company company, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Result<Company>> GetSeededAsync(CancellationToken cancellationToken) =>
            Task.FromResult<Result<Company>>(seeded);
    }

    private sealed class InMemoryEmployeeRepository : IEmployeeRepository
    {
        public List<Employee> Saved { get; } = [];

        public Task AddAsync(Employee employee, CancellationToken cancellationToken)
        {
            Saved.Add(employee);
            return Task.CompletedTask;
        }

        public Task<Result<Employee>> GetByIdentifierAsync(EmployeeIdentifier identifier, CancellationToken cancellationToken) =>
            Task.FromResult<Result<Employee>>(
                Saved.SingleOrDefault(employee => employee.Identifier == identifier) is { } found
                    ? found
                    : Error.NotFound("employee.not_found", "No employee has that identifier."));
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
