using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmartSolutionsLab.Roomy.Web.Http;

public static class OptionsRegistration
{
    // Binds a configuration section to TOptions, validates its data annotations at startup, and also
    // exposes the bound instance as a concrete singleton so endpoints/seeders can inject TOptions directly
    // (the hosts consume the option type, not IOptions<TOptions>).
    public static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<TOptions>>().Value);

        return services;
    }

    public static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        Func<TOptions, bool> validate,
        string validationMessage)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(validate, validationMessage)
            .ValidateOnStart();

        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<TOptions>>().Value);

        return services;
    }
}
