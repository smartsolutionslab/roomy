using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application;

public interface IInitialCredentialEncryptor
{
    EncryptedCredential Encrypt(string initialPassword);
}
