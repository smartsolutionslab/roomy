using System.Security.Cryptography;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Infrastructure.Cryptography;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class EmployeeHiredConsumerTests
{
    private static readonly ICredentialCipher cipher = new AesGcmCredentialCipher(new CredentialEncryptionOptions
    {
        ActiveKeyId = "test",
        Keys = new Dictionary<string, string> { ["test"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) },
    });

    [Fact]
    public async Task Decrypts_and_maps_an_administrator_hire_onto_the_register_user_command()
    {
        var capturing = new CapturingHandler();
        var consumer = new EmployeeHiredConsumer(capturing, cipher);
        var employeeHired = Hired("grace@example.com", "Grace Hopper", HiredRole.Administrator, "correct horse");

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
        var consumer = new EmployeeHiredConsumer(capturing, cipher);
        var employeeHired = Hired("ada@example.com", "Ada Lovelace", HiredRole.Employee, "correct horse");

        await consumer.Handle(employeeHired, TestContext.Current.CancellationToken);

        capturing.Command.ShouldNotBeNull().Role.IsAdministrator.ShouldBeFalse();
    }

    [Fact]
    public async Task Surfaces_a_failed_registration_so_the_message_is_retried()
    {
        var consumer = new EmployeeHiredConsumer(
            new FailingHandler(new Error("provider_error", "the identity provider is unavailable")),
            cipher);
        var employeeHired = Hired("ada@example.com", "Ada Lovelace", HiredRole.Employee, "correct horse");

        await Should.ThrowAsync<Exception>(() => consumer.Handle(employeeHired, TestContext.Current.CancellationToken));
    }

    private static EmployeeHired Hired(string email, string displayName, HiredRole role, string initialPassword) =>
        new(
            EmployeeId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            Email: email,
            DisplayName: displayName,
            Role: role,
            EncryptedInitialPassword: cipher.Encrypt(initialPassword),
            OccurredAt: DateTimeOffset.UtcNow);

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
