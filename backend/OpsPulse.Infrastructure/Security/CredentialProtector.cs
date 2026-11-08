using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OpsPulse.Application.Common;

namespace OpsPulse.Infrastructure.Security;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>64-character hex string (32 bytes) used for AES-GCM credential encryption.</summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// AES-GCM encryption for stored credentials (database passwords, kubeconfigs).
/// Format: base64(nonce[12] || tag[16] || ciphertext).
/// </summary>
public sealed class CredentialProtector : ICredentialProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public CredentialProtector(IOptions<EncryptionOptions> options)
    {
        var keyHex = options.Value.Key;
        byte[] key;
        try
        {
            key = string.IsNullOrWhiteSpace(keyHex) || keyHex.Length != 64
                ? throw new FormatException("Key must be exactly 64 hex characters.")
                : Convert.FromHexString(keyHex);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Encryption:Key must be configured as a 64-character hex string (32 bytes). " +
                "Provide it via environment variables or user secrets — never commit production keys.",
                exception);
        }

        _key = key;
    }

    public string Protect(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipherBytes]);
    }

    public string Unprotect(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);
        if (data.Length < NonceSize + TagSize)
        {
            throw new InvalidOperationException("Encrypted payload is malformed.");
        }

        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = data[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
