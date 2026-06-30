using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmartSolutionsLab.Roomy.Infrastructure.Cryptography;

public static class CredentialEncryptionServiceCollectionExtensions
{
    public static IServiceCollection AddCredentialEncryption(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CredentialEncryptionOptions>(configuration.GetSection(CredentialEncryptionOptions.SectionName));
        services.AddSingleton<ICredentialCipher>(serviceProvider => new AesGcmCredentialCipher(serviceProvider.GetRequiredService<IOptions<CredentialEncryptionOptions>>().Value));

        return services;
    }
}
