using SmartSolutionsLab.Roomy.Infrastructure.Cryptography;
using SmartSolutionsLab.Roomy.Organization.Application;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Security;

internal sealed class CredentialEncryptor(ICredentialCipher cipher) : IInitialCredentialEncryptor
{
    public EncryptedCredential Encrypt(string initialPassword) =>
        EncryptedCredential.From(cipher.Encrypt(initialPassword));
}
