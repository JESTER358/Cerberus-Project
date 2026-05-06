using Microsoft.Extensions.Configuration;
using ProyectoInnovador.Security.Exceptions;
using ProyectoInnovador.Security.Services;

namespace Cerberus.Security.Tests;

public sealed class Check1_AesRoundtripTests
{
    [Fact]
    public void Check1_Roundtrip_WithValidKey_ReturnsExactOriginalBytes()
    {
        var service = new AesCbcRoundtripService();
        var key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Semana2-CHECK1");

        var encrypted = service.Encrypt(plaintext, key, "v1");
        var decrypted = service.Decrypt(encrypted, key);

        Assert.Equal(plaintext, decrypted);
        Assert.Equal("v1", encrypted.KeyVersion);
        Assert.NotEmpty(encrypted.Iv);
        Assert.NotEmpty(encrypted.Ciphertext);
    }

    [Fact]
    public void Check1_Roundtrip_EmptyPayload_IsSupported()
    {
        var service = new AesCbcRoundtripService();
        var key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

        var encrypted = service.Encrypt([], key, "v1");
        var decrypted = service.Decrypt(encrypted, key);

        Assert.Empty(decrypted);
    }

    [Fact]
    public void Check1_InvalidKey_ThrowsExplicitLengthError()
    {
        var service = new AesCbcRoundtripService();
        var invalidKey = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        var ex = Assert.Throws<Check1KeyException>(() =>
            service.Encrypt([1, 2, 3], invalidKey, "v1"));

        Assert.Contains("32 bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check1_MissingKey_ThrowsExplicitMissingKeyError()
    {
        var service = new AesCbcRoundtripService();

        var ex = Assert.Throws<Check1KeyException>(() =>
            service.Encrypt([1, 2, 3], [], "v1"));

        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Check1_MissingKeyProviderConfig_ThrowsExplicitMissingKeyError()
    {
        var configuration = new ConfigurationManager();
        configuration["Security:Check1KeyVersion"] = "v1";
        var provider = new ConfigurationCheck1KeyProvider(configuration);

        var ex = Assert.Throws<Check1KeyException>(() => provider.GetRequiredKey());

        Assert.Contains("Security:Check1KeyBase64", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
