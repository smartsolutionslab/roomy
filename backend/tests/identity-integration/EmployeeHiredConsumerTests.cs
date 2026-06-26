using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class EmployeeHiredConsumerTests
{
    [Fact]
    public async Task Maps_an_administrator_hire_onto_the_register_user_command()
    {
        var capturing = new CapturingHandler();
        var consumer = new EmployeeHiredConsumer(capturing);
        var employeeHired = new EmployeeHired(
            EmployeeId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            Email: "grace@example.com",
            DisplayName: "Grace Hopper",
            Role: HiredRole.Administrator,
            InitialPassword: "correct horse",
            OccurredAt: DateTimeOffset.UtcNow);

        await consumer.Handle(employeeHired, TestContext.Current.CancellationToken);

        var command = capturing.Command.ShouldNotBeNull();
        command.UserIdentifier.Value.ShouldBe(employeeHired.UserId);
        command.EmployeeId.ShouldBe(employeeHired.EmployeeId);
        command.Email.Value.ShouldBe("grace@example.com");
        command.DisplayName.Value.ShouldBe("Grace Hopper");
        command.Role.IsAdministrator.ShouldBeTrue();
        command.InitialPassword.ShouldBe("correct horse");
    }

    [Fact]
    public async Task Maps_an_employee_hire_without_administrator_elevation()
    {
        var capturing = new CapturingHandler();
        var consumer = new EmployeeHiredConsumer(capturing);
        var employeeHired = new EmployeeHired(
            EmployeeId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            Email: "ada@example.com",
            DisplayName: "Ada Lovelace",
            Role: HiredRole.Employee,
            InitialPassword: "correct horse",
            OccurredAt: DateTimeOffset.UtcNow);

        await consumer.Handle(employeeHired, TestContext.Current.CancellationToken);

        capturing.Command.ShouldNotBeNull().Role.IsAdministrator.ShouldBeFalse();
    }

    [Fact]
    public async Task Surfaces_a_failed_registration_so_the_message_is_retried()
    {
        var consumer = new EmployeeHiredConsumer(new FailingHandler(new Error("provider_error", "the identity provider is unavailable")));
        var employeeHired = new EmployeeHired(
            EmployeeId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            Email: "ada@example.com",
            DisplayName: "Ada Lovelace",
            Role: HiredRole.Employee,
            InitialPassword: "correct horse",
            OccurredAt: DateTimeOffset.UtcNow);

        await Should.ThrowAsync<Exception>(() => consumer.Handle(employeeHired, TestContext.Current.CancellationToken));
    }

    private sealed class CapturingHandler : ICommandHandler<RegisterUser>
    {
        public RegisterUser? Command { get; private set; }

        public Task<Result> HandleAsync(RegisterUser command, CancellationToken cancellationToken)
        {
            Command = command;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class FailingHandler(Error error) : ICommandHandler<RegisterUser>
    {
        public Task<Result> HandleAsync(RegisterUser command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure(error));
    }
}
