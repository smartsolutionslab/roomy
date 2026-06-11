using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Saga.E2ETests;

public sealed class HireEmployeeSagaTests(SagaStackFixture fixture) : IClassFixture<SagaStackFixture>
{
    [Fact]
    public async Task Hiring_a_colleague_provisions_their_account_and_lets_them_sign_in()
    {
        var email = UniqueEmail();
        const string Password = "Colleague.1";

        var hired = await HireAsync("Ada Lovelace", email, "Employee", Password);

        var employee = await fixture.WaitForTerminalStateAsync(hired.EmployeeId, email, TestContext.Current.CancellationToken);
        employee.State.ShouldBe(ProvisioningState.Active);

        (await fixture.CanAuthenticateAsync(email, Password, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Hiring_with_an_already_used_email_fails_provisioning_without_a_half_account()
    {
        var hired = await HireAsync("Imposter", SagaStackFixture.AdminEmail, "Employee", "Imposter.1");

        var employee = await fixture.WaitForTerminalStateAsync(
            hired.EmployeeId, SagaStackFixture.AdminEmail, TestContext.Current.CancellationToken);

        employee.State.ShouldBe(ProvisioningState.Failed);
        employee.FailureReason.ShouldBe(ProvisioningFailureReason.EmailTaken);
    }

    [Fact]
    public async Task Hiring_the_same_email_twice_yields_one_active_and_one_failed_never_two_accounts()
    {
        var email = UniqueEmail();

        var first = await HireAsync("First Hire", email, "Employee", "FirstHire.1");
        var firstEmployee = await fixture.WaitForTerminalStateAsync(first.EmployeeId, email, TestContext.Current.CancellationToken);
        firstEmployee.State.ShouldBe(ProvisioningState.Active);

        var second = await HireAsync("Second Hire", email, "Employee", "SecondHire.1");
        var secondEmployee = await fixture.WaitForTerminalStateAsync(second.EmployeeId, email, TestContext.Current.CancellationToken);

        secondEmployee.State.ShouldBe(ProvisioningState.Failed);
        secondEmployee.FailureReason.ShouldBe(ProvisioningFailureReason.EmailTaken);
    }

    private async Task<HiredEmployee> HireAsync(string displayName, string email, string role, string password)
    {
        var token = await fixture.AcquireUserTokenAsync(
            SagaStackFixture.AdminEmail, SagaStackFixture.AdminPassword, TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "employees")
        {
            Content = JsonContent.Create(new
            {
                displayName,
                email,
                role,
                initialPassword = password,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await fixture.Organization.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted, $"Hire failed: {(int)response.StatusCode} {body}");

        var hired = await response.Content.ReadFromJsonAsync<HiredEmployee>(TestContext.Current.CancellationToken);
        hired.ShouldNotBeNull();
        hired.EmployeeId.ShouldNotBe(Guid.Empty);
        return hired;
    }

    private static string UniqueEmail() => $"colleague-{Guid.CreateVersion7():n}@example.com";

    private sealed record HiredEmployee(Guid EmployeeId, Guid UserId, string State);
}
