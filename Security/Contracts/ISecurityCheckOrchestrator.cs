using Microsoft.AspNetCore.Http;
using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Contracts;

public interface ISecurityCheckOrchestrator
{
    FileRoundtripResult RunFileRoundtrip(byte[] originalBytes);
    FileRoundtripResult RunTamperedCiphertextScenario(byte[] originalBytes);
    string ComputeSha256(byte[] data);
    Task<MultiCloudUploadResult> UploadMultiCloudAsync(IFormFile archivo, CancellationToken cancellationToken = default);
}
