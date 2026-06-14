using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;
using SmartSolutionsLab.Roomy.Web.Http;
namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/employees", HireEmployeeAsync)
            .RequireAdministrator()
            .WithName("HireEmployee")
            .Produces<Response.HiredEmployee>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> HireEmployeeAsync(
        Request.HireEmployee request,
        ICommandHandler<HireEmployee, HiredEmployee> commandHandler,
        CancellationToken cancellationToken)
    {
        var role = Ensure.That(request.Role).IsEnum<EmployeeRole>().Value;
        var password = Ensure.That(request.InitialPassword).IsNotNullOrWhiteSpace().Value;

        var command = new HireEmployee(
            EmployeeName.From(request.DisplayName),
            WorkEmail.From(request.Email),
            role,
            password);
        var result = await commandHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return result.Error.ToHttpResult();

        var hired = result.Value;
        return Results.Accepted($"/employees/{hired.Employee.Value}", hired.ToResponse());
    }
}
