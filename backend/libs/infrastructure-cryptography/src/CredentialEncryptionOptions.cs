namespace SmartSolutionsLab.Roomy.Infrastructure.Cryptography;

public sealed class CredentialEncryptionOptions
{
    public const string SectionName = "CredentialEncryption";

    public string ActiveKeyId { get; init; } = string.Empty;

    public Dictionary<string, string> Keys { get; init; } = [];
}
