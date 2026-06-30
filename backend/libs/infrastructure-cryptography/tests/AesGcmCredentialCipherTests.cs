using System.Security.Cryptography;
using Shouldly;

namespace SmartSolutionsLab.Roomy.Infrastructure.Cryptography.Tests;

public sealed class AesGcmCredentialCipherTests
{
    private const string Plaintext = "correct horse battery staple";

    [Fact]
    public void Round_trips_a_credential_back_to_the_original_plaintext()
    {
        var cipher = CipherWith(("k1", RandomKey()));

        var encrypted = cipher.Encrypt(Plaintext);

        cipher.Decrypt(encrypted).ShouldBe(Plaintext);
    }

    [Fact]
    public void Produces_ciphertext_that_does_not_contain_the_plaintext()
    {
        var cipher = CipherWith(("k1", RandomKey()));

        var encrypted = cipher.Encrypt(Plaintext);

        encrypted.ShouldNotContain(Plaintext);
    }

    [Fact]
    public void Produces_distinct_ciphertext_for_the_same_plaintext()
    {
        var cipher = CipherWith(("k1", RandomKey()));

        cipher.Encrypt(Plaintext).ShouldNotBe(cipher.Encrypt(Plaintext));
    }

    [Fact]
    public void Tags_the_ciphertext_with_the_active_key_id()
    {
        var cipher = CipherWith("active", ("retired", RandomKey()), ("active", RandomKey()));

        cipher.Encrypt(Plaintext).ShouldStartWith("active.");
    }

    [Fact]
    public void Rejects_tampered_ciphertext()
    {
        var cipher = CipherWith(("k1", RandomKey()));
        var encrypted = cipher.Encrypt(Plaintext);
        var tampered = Tamper(encrypted);

        Should.Throw<CryptographicException>(() => cipher.Decrypt(tampered));
    }

    [Fact]
    public void Decrypts_a_credential_encrypted_under_a_now_retired_key_after_rotation()
    {
        var retiredKey = RandomKey();
        var rotatedKey = RandomKey();

        var beforeRotation = CipherWith("k1", ("k1", retiredKey));
        var encrypted = beforeRotation.Encrypt(Plaintext);

        var afterRotation = CipherWith("k2", ("k1", retiredKey), ("k2", rotatedKey));

        afterRotation.Decrypt(encrypted).ShouldBe(Plaintext);
        afterRotation.Encrypt(Plaintext).ShouldStartWith("k2.");
    }

    [Fact]
    public void Rejects_a_credential_whose_key_is_no_longer_in_the_ring()
    {
        var encrypted = CipherWith(("k1", RandomKey())).Encrypt(Plaintext);
        var differentRing = CipherWith(("k2", RandomKey()));

        Should.Throw<InvalidOperationException>(() => differentRing.Decrypt(encrypted));
    }

    [Fact]
    public void Fails_fast_when_the_active_key_is_absent_from_the_ring()
    {
        Should.Throw<InvalidOperationException>(() => CipherWith("missing", ("k1", RandomKey())));
    }

    [Fact]
    public void Fails_fast_when_a_key_is_not_a_256_bit_key()
    {
        var options = new CredentialEncryptionOptions
        {
            ActiveKeyId = "k1",
            Keys = new Dictionary<string, string> { ["k1"] = Convert.ToBase64String(new byte[16]) },
        };

        Should.Throw<InvalidOperationException>(() => new AesGcmCredentialCipher(options));
    }

    private static AesGcmCredentialCipher CipherWith(params (string Id, string Key)[] keys) =>
        CipherWith(keys[0].Id, keys);

    private static AesGcmCredentialCipher CipherWith(string activeKeyId, params (string Id, string Key)[] keys) =>
        new(new CredentialEncryptionOptions
        {
            ActiveKeyId = activeKeyId,
            Keys = keys.ToDictionary(entry => entry.Id, entry => entry.Key),
        });

    private static string RandomKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Tamper(string encrypted)
    {
        var separator = encrypted.IndexOf('.', StringComparison.Ordinal);
        var keyId = encrypted[..separator];
        var frame = Convert.FromBase64String(encrypted[(separator + 1)..]);
        frame[^1] ^= 0xFF;
        return $"{keyId}.{Convert.ToBase64String(frame)}";
    }
}
