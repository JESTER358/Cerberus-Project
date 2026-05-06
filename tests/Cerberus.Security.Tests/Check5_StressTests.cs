using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Models;
using ProyectoInnovador.Security.Services;

namespace Cerberus.Security.Tests;

public sealed class Check5_StressTests
{
    [Fact]
    public void Check5_Stress_MatrixAchievesFullCorrectness()
    {
        var orchestrator = CreateOrchestrator();
        var sizes = new[] { 1024, 64 * 1024, 256 * 1024 };
        const int repetitions = 2;

        var runs = new List<StressRunResult>();

        foreach (var size in sizes)
        {
            for (var i = 1; i <= repetitions; i++)
            {
                var bytes = BuildDeterministicPayload(size, i);
                var sw = Stopwatch.StartNew();
                var result = orchestrator.RunFileRoundtrip(bytes);
                sw.Stop();

                runs.Add(new StressRunResult
                {
                    SizeBytes = size,
                    Iteration = i,
                    RoundtripPass = bytes.SequenceEqual(result.DecryptedBytes),
                    IntegrityPass = result.IntegrityPass,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds
                });
            }
        }

        Assert.NotEmpty(runs);
        Assert.All(runs, run =>
        {
            Assert.True(run.RoundtripPass);
            Assert.True(run.IntegrityPass);
        });

        var docsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../docs"));
        Directory.CreateDirectory(docsDir);
        var reportPath = Path.Combine(docsDir, "week2-check5-stress-report.md");

        var lines = new List<string>
        {
            "# CHECK5 Stress Report",
            "",
            "| SizeBytes | Iteration | RoundtripPass | IntegrityPass | ElapsedMs |",
            "|---:|---:|:---:|:---:|---:|"
        };

        lines.AddRange(runs.Select(r =>
            $"| {r.SizeBytes} | {r.Iteration} | {(r.RoundtripPass ? "PASS" : "FAIL")} | {(r.IntegrityPass ? "PASS" : "FAIL")} | {r.ElapsedMilliseconds} |"));

        File.WriteAllLines(reportPath, lines);
        Assert.True(File.Exists(reportPath));
    }

    private static ISecurityCheckOrchestrator CreateOrchestrator()
    {
        var config = new ConfigurationManager();
        config["Security:Check1KeyVersion"] = "v1";
        config["Security:Check1KeyBase64"] = Convert.ToBase64String(Enumerable.Repeat((byte)5, 32).ToArray());

        var provider = new ConfigurationCheck1KeyProvider(config);
        var aes = new AesCbcRoundtripService();
        var fileService = new FileCryptoService(aes, provider);
        var hashService = new Sha256IntegrityHashService();

        return new SecurityCheckOrchestrator(fileService, hashService, null!, null!, null!);
    }

    private static byte[] BuildDeterministicPayload(int size, int iteration)
    {
        var bytes = new byte[size];
        for (var i = 0; i < size; i++)
        {
            bytes[i] = (byte)((i + iteration) % 256);
        }

        return bytes;
    }
}
