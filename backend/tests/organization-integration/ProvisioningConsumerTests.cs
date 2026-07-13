using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class ProvisioningConsumerTests
{
    private static readonly Guid employeeId = Guid.CreateVersion7();

    [Fact]
    public async Task UserRegistered_completes_provisioning_for_the_employee()
    {
        var capturing = new Capturing<CompleteEmployeeProvisioning>();
        var consumer = new UserRegisteredConsumer(capturing);

        await consumer.Handle(UserRegistered(), TestContext.Current.CancellationToken);

        capturing.Command.ShouldNotBeNull().Employee.Value.ShouldBe(employeeId);
    }

    [Fact]
    public async Task UserRegistered_surfaces_a_failed_completion_so_the_message_is_retried()
    {
        var consumer = new UserRegisteredConsumer(
            new Failing<CompleteEmployeeProvisioning>(Error.NotFound("employee.not_found", "no such employee")));

        await Should.ThrowAsync<Exception>(() => consumer.Handle(UserRegistered(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UserProvisioningFailed_fails_provisioning_for_the_employee()
    {
        var capturing = new Capturing<FailEmployeeProvisioning>();
        var consumer = new UserProvisioningFailedConsumer(capturing);

        await consumer.Handle(UserProvisioningFailed(), TestContext.Current.CancellationToken);

        var command = capturing.Command.ShouldNotBeNull();
        command.Employee.Value.ShouldBe(employeeId);
        command.Reason.ShouldBe(ProvisioningFailureReason.EmailTaken);
    }

    [Fact]
    public async Task UserProvisioningFailed_surfaces_a_failed_transition_so_the_message_is_retried()
    {
        var consumer = new UserProvisioningFailedConsumer(
            new Failing<FailEmployeeProvisioning>(Error.NotFound("employee.not_found", "no such employee")));

        await Should.ThrowAsync<Exception>(() => consumer.Handle(UserProvisioningFailed(), TestContext.Current.CancellationToken));
    }

    private static UserRegistered UserRegistered() =>
        new(Guid.CreateVersion7(), employeeId, "ada@example.com", AccountRole.Employee, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

    private static UserProvisioningFailed UserProvisioningFailed() =>
        new(Guid.CreateVersion7(), employeeId, UserProvisioningFailureReason.EmailTaken, DateTimeOffset.UtcNow);

    private sealed class Capturing<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public TCommand? Command { get; private set; }

        public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            Command = command;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class Failing<TCommand>(Error error) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure(error));
    }
}
