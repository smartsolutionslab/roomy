using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

// The hiring surface (contract: organization-api.md, 008). Hiring requires the administrator role, so an
// authenticated employee is Forbidden (403, FR-001); the service is internal — the BFF forwards the
// Keycloak token whose realm roles the host flattens to role claims. Hiring is eventually consistent: the
// 202 response means the employee is recorded and provisioning has started, not that the login exists yet
// (ADR-0025).
public static class EmployeeEndpoints
{
    private const string AdministratorRole = "administrator";

    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/employees", HireEmployeeAsync)
            .RequireAuthorization(policy => policy.RequireRole(AdministratorRole))
            .WithName("HireEmployee")
            .Produces<Response.HiredEmployee>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    // POST /employees — hire a colleague and start provisioning their login (FR-001/002/003). 400 for a
    // missing/invalid field; 202 with the recorded employee + the pre-allocated user id and Provisioning
    // state. Administrator-only (403 otherwise, enforced by the policy).
    private static async Task<IResult> HireEmployeeAsync(
        Request.HireEmployee request,
        ICommandHandler<HireEmployee, HiredEmployee> hireEmployee,
        CancellationToken cancellationToken)
    {
        var name = EmployeeName.TryParse(request.DisplayName);
        var email = WorkEmail.TryParse(request.Email);
        if (name is null
            || email is null
            || string.IsNullOrWhiteSpace(request.InitialPassword)
            || !Enum.TryParse<EmployeeRole>(request.Role, ignoreCase: true, out var role))
        {
            return Results.BadRequest(
                "A hire requires a display name, a valid work email, a role (Employee or Administrator), and an initial password.");
        }

        var result = await hireEmployee.HandleAsync(
            new HireEmployee(name, email, role, request.InitialPassword), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToHttpResult();
        }

        var hired = result.Value;
        return Results.Accepted(
            $"/employees/{hired.Employee.Value}",
            new Response.HiredEmployee(hired.Employee.Value, hired.User.Value, ProvisioningState.Provisioning.ToString()));
    }
}
