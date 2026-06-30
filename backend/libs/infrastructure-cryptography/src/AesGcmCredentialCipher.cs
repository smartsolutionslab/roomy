using System.Security.Cryptography;
using System.Text;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Cryptography;

public sealed class AesGcmCredentialCipher : ICredentialCipher
{
    private const int NonceSizeInBytes = 12;
    private const int TagSizeInBytes = 16;
    private const int KeySizeInBytes = 32;
    private const char KeyIdSeparator = '.';

    private readonly IReadOnlyDictionary<string, byte[]> keysById;
    private readonly string activeKeyId;

    public AesGcmCredentialCipher(CredentialEncryptionOptions options)
    {
        Ensure.That((CredentialEncryptionOptions?)options).IsNotNull();
        activeKeyId = Ensure.That(options.ActiveKeyId).IsNotNullOrWhiteSpace().Value;

        keysById = options.Keys.ToDictionary(
            entry => entry.Key,
            entry => DecodeKey(entry.Key, entry.Value));

        if (!keysById.ContainsKey(activeKeyId))
        {
            throw new InvalidOperationException(
                $"The active credential-encryption key '{activeKeyId}' is not present in the configured key ring.");
        }
    }

    public string Encrypt(string plaintext)
    {
        Ensure.That(plaintext).IsNotNull();

        var key = keysById[activeKeyId];
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeInBytes];

        using var aes = new AesGcm(key, TagSizeInBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var frame = new byte[NonceSizeInBytes + TagSizeInBytes + ciphertext.Length];
        nonce.CopyTo(frame, 0);
        tag.CopyTo(frame, NonceSizeInBytes);
        ciphertext.CopyTo(frame, NonceSizeInBytes + TagSizeInBytes);

        return $"{activeKeyId}{KeyIdSeparator}{Convert.ToBase64String(frame)}";
    }

    public string Decrypt(string ciphertext)
    {
        Ensure.That(ciphertext).IsNotNullOrWhiteSpace();

        var separator = ciphertext.IndexOf(KeyIdSeparator, StringComparison.Ordinal);
        if (separator <= 0)
        {
            throw new FormatException("The encrypted credential is not in the expected '<keyId>.<payload>' format.");
        }

        var keyId = ciphertext[..separator];
        if (!keysById.TryGetValue(keyId, out var key))
        {
            throw new InvalidOperationException(
                $"The credential was encrypted with key '{keyId}', which is not in the configured key ring.");
        }

        var frame = Convert.FromBase64String(ciphertext[(separator + 1)..]);
        var nonce = frame.AsSpan(0, NonceSizeInBytes);
        var tag = frame.AsSpan(NonceSizeInBytes, TagSizeInBytes);
        var payload = frame.AsSpan(NonceSizeInBytes + TagSizeInBytes);
        var plaintext = new byte[payload.Length];

        using var aes = new AesGcm(key, TagSizeInBytes);
        aes.Decrypt(nonce, payload, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DecodeKey(string keyId, string base64Key)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"The credential-encryption key '{keyId}' is not valid base64.", exception);
        }

        if (key.Length != KeySizeInBytes)
        {
            throw new InvalidOperationException(
                $"The credential-encryption key '{keyId}' must be {KeySizeInBytes} bytes (AES-256); it was {key.Length}.");
        }

        return key;
    }
}
