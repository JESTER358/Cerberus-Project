using Microsoft.Extensions.Configuration;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Services;

namespace Cerberus.Security.Tests;

public sealed class Check3_FileFlowTests
{
    [Fact]
    public void Check3_FileRoundtrip_EncryptThenDecrypt_RestoresOriginalBytes()
    {
        var service = CreateFileService("v1", BuildKeyBase64(7));
        var fileBytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        var encrypted = service.EncryptFile(fileBytes);
        var decrypted = service.DecryptFile(encrypted);

        Assert.Equal(fileBytes, decrypted);
        Assert.NotEmpty(encrypted.Iv);
        Assert.Equal("v1", encrypted.KeyVersion);
    }

    private static IFileCryptoService CreateFileService(string version, string keyBase64)
    {
        var config = new ConfigurationManager();
        config["Security:Check1KeyVersion"] = version;
        config["Security:Check1KeyBase64"] = keyBase64;

        var provider = new ConfigurationCheck1KeyProvider(config);
        var aes = new AesCbcRoundtripService();
        return new FileCryptoService(aes, provider);
    }

    private static string BuildKeyBase64(byte seed)
    {
        var key = Enumerable.Repeat(seed, 32).ToArray();
        return Convert.ToBase64String(key);
    }
}
