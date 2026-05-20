using Microsoft.AspNetCore.Http;
using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Contracts;

public interface ISecurityCheckOrchestrator
{
    FileRoundtripResult RunFileRoundtrip(byte[] originalBytes);
    FileRoundtripResult RunTamperedCiphertextScenario(byte[] originalBytes);
    string ComputeSha256(byte[] data);
    /// <summary>
    /// Cifra y sube el archivo. Genera internamente la semilla segura.
    /// Devuelve la semilla en texto plano UNA sola vez — el servidor no la vuelve a conocer.
    /// </summary>
    Task<(MultiCloudUploadResult Result, string Seed)> UploadMultiCloudAsync(IFormFile archivo, int? usuarioId = null, CancellationToken cancellationToken = default);
}
