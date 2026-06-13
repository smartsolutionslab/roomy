using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class ExceptionHandling
{
    public static IServiceCollection AddRoomyExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<ArgumentExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
