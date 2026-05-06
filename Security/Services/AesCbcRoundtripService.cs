using System.Security.Cryptography;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Exceptions;
using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Services;

public sealed class AesCbcRoundtripService : IAesRoundtripService
{
    public AesPayload Encrypt(byte[] plaintext, byte[] key, string keyVersion)
    {
        ValidateKey(key);

        var input = plaintext ?? throw new ArgumentNullException(nameof(plaintext));
        if (string.IsNullOrWhiteSpace(keyVersion))
        {
            throw new Check1KeyException("Security key version is required.");
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(input, 0, input.Length);

        return new AesPayload
        {
            Ciphertext = ciphertext,
            Iv = aes.IV,
            KeyVersion = keyVersion
        };
    }

    public byte[] Decrypt(AesPayload payload, byte[] key)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Iv is null || payload.Iv.Length == 0)
        {
            throw new ArgumentException("AES IV is required for decryption.", nameof(payload));
        }

        if (payload.Ciphertext is null)
        {
            throw new ArgumentException("AES ciphertext is required for decryption.", nameof(payload));
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = payload.Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(payload.Ciphertext, 0, payload.Ciphertext.Length);
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length == 0)
        {
            throw new Check1KeyException("Security key is missing.");
        }

        if (key.Length != 32)
        {
            throw new Check1KeyException("Security key must be exactly 32 bytes for AES-256.");
        }
    }
}
