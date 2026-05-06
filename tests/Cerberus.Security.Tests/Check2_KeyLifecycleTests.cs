using Microsoft.Extensions.Configuration;
using ProyectoInnovador.Security.Services;

namespace Cerberus.Security.Tests;

public sealed class Check2_KeyLifecycleTests
{
    [Fact]
    public void Check2_KeyVersion_EncryptionMetadataIncludesActiveVersion()
    {
        var config = CreateConfig("v1", BuildKeyBase64(1));
        var provider = new ConfigurationCheck1KeyProvider(config);
        var aes = new AesCbcRoundtripService();

        var payload = aes.Encrypt([10, 20, 30], provider.GetRequiredKey(), provider.GetActiveVersion());

        Assert.Equal("v1", payload.KeyVersion);
    }

    [Fact]
    public void Check2_KeyRotation_NewEncryptionsUseRotatedVersion()
    {
        var config = CreateConfig("v1", BuildKeyBase64(1));
        var provider = new ConfigurationCheck1KeyProvider(config);
        var aes = new AesCbcRoundtripService();

        var v1Payload = aes.Encrypt([1, 2, 3], provider.GetRequiredKey(), provider.GetActiveVersion());

        config["Security:Check1KeyVersion"] = "v2";
        config["Security:Check1KeyBase64"] = BuildKeyBase64(2);
        var v2Payload = aes.Encrypt([1, 2, 3], provider.GetRequiredKey(), provider.GetActiveVersion());

        Assert.Equal("v1", v1Payload.KeyVersion);
        Assert.Equal("v2", v2Payload.KeyVersion);
    }

    private static ConfigurationManager CreateConfig(string version, string keyBase64)
    {
        var config = new ConfigurationManager();
        config["Security:Check1KeyVersion"] = version;
        config["Security:Check1KeyBase64"] = keyBase64;
        return config;
    }

    private static string BuildKeyBase64(byte seed)
    {
        var key = Enumerable.Repeat(seed, 32).ToArray();
        return Convert.ToBase64String(key);
    }
}
