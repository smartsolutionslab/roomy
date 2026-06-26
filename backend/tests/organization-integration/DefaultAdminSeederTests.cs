using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Api.Seeding;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class DefaultAdminSeederTests
{
    private static DefaultAdminOptions Options() => new()
    {
        Email = "admin@roomy.local",
        DisplayName = "Default Admin",
        InitialPassword = "DevAdmin.23456",
    };

    [Fact]
    public async Task Hires_the_default_admin_as_an_administrator_when_absent()
    {
        var hire = new RecordingHire();
        var retry = new RecordingRetry();

        await new DefaultAdminSeeder(new StubEmployees(exists: false), hire, retry, Options())
            .SeedAsync(TestContext.Current.CancellationToken);

        hire.Calls.ShouldBe(1);
        hire.Last.ShouldNotBeNull();
        hire.Last.Role.ShouldBe(EmployeeRole.Administrator);
        hire.Last.Email.Value.ShouldBe("admin@roomy.local");
        hire.Last.Name.Value.ShouldBe("Default Admin");
        retry.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Re_drives_provisioning_when_the_admin_already_exists()
    {
        var hire = new RecordingHire();
        var retry = new RecordingRetry();

        await new DefaultAdminSeeder(new StubEmployees(exists: true), hire, retry, Options())
            .SeedAsync(TestContext.Current.CancellationToken);

        hire.Calls.ShouldBe(0);
        retry.Calls.ShouldBe(1);
        retry.Last.ShouldNotBeNull();
        retry.Last.Email.Value.ShouldBe("admin@roomy.local");
        retry.Last.InitialPassword.ShouldBe("DevAdmin.23456");
    }

    private sealed class StubEmployees(bool exists) : IEmployeeRepository
    {
        public Task AddAsync(Employee employee, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Result<Employee>> GetByIdentifierAsync(EmployeeIdentifier identifier, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<Employee>> GetByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ExistsByWorkEmailAsync(WorkEmail email, CancellationToken cancellationToken) =>
            Task.FromResult(exists);
    }

    private sealed class RecordingHire : ICommandHandler<HireEmployee, HiredEmployee>
    {
        public int Calls { get; private set; }
        public HireEmployee? Last { get; private set; }

        public Task<Result<HiredEmployee>> HandleAsync(HireEmployee command, CancellationToken cancellationToken)
        {
            Calls++;
            Last = command;
            return Task.FromResult(Result.Success(new HiredEmployee(EmployeeIdentifier.New(), UserIdentifier.New())));
        }
    }

    private sealed class RecordingRetry : ICommandHandler<RetryEmployeeProvisioning>
    {
        public int Calls { get; private set; }
        public RetryEmployeeProvisioning? Last { get; private set; }

        public Task<Result> HandleAsync(RetryEmployeeProvisioning command, CancellationToken cancellationToken)
        {
            Calls++;
            Last = command;
            return Task.FromResult(Result.Success());
        }
    }
}
