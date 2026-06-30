namespace SmartSolutionsLab.Roomy.Infrastructure.Cryptography;

public interface ICredentialCipher
{
    string Encrypt(string plaintext);

    string Decrypt(string ciphertext);
}
