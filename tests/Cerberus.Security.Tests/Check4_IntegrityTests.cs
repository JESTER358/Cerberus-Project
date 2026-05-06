using Microsoft.Extensions.Configuration;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Services;

namespace Cerberus.Security.Tests;

public sealed class Check4_IntegrityTests
{
    [Fact]
    public void Check4_IntegrityMatch_OriginalAndDecryptedHashesAreEqual()
    {
        var orchestrator = CreateOrchestrator();
        var bytes = System.Text.Encoding.UTF8.GetBytes("integrity-check");

        var result = orchestrator.RunFileRoundtrip(bytes);

        Assert.True(result.IntegrityPass);
        Assert.Equal(result.OriginalHash, result.DecryptedHash);
    }

    [Fact]
    public void Check4_IntegrityTamperFail_ModifiedCiphertextFailsIntegrity()
    {
        var orchestrator = CreateOrchestrator();
        var bytes = System.Text.Encoding.UTF8.GetBytes("tamper-check");

        var result = orchestrator.RunTamperedCiphertextScenario(bytes);

        Assert.False(result.IntegrityPass);
        Assert.NotEqual(result.OriginalHash, result.DecryptedHash);
    }

    private static ISecurityCheckOrchestrator CreateOrchestrator()
    {
        var config = new ConfigurationManager();
        config["Security:Check1KeyVersion"] = "v1";
        config["Security:Check1KeyBase64"] = Convert.ToBase64String(Enumerable.Repeat((byte)3, 32).ToArray());

        var provider = new ConfigurationCheck1KeyProvider(config);
        var aes = new AesCbcRoundtripService();
        var fileService = new FileCryptoService(aes, provider);
        var hashService = new Sha256IntegrityHashService();

        return new SecurityCheckOrchestrator(fileService, hashService, null!, null!, null!);
    }
}
