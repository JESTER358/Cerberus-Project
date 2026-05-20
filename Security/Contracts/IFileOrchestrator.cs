using System.Threading;
using System.Threading.Tasks;

namespace ProyectoInnovador.Security.Contracts;

/// <summary>
/// Interfaz para la orquestación de la recuperación de archivos.
/// </summary>
public interface IFileOrchestrator
{
    /// <summary>
    /// Recupera, reensambla y verifica un archivo desde múltiples nubes.
    /// </summary>
    /// <param name="archivoId">Id del archivo original.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Bytes del archivo recuperado y desencriptado.</returns>
    /// <summary>
    /// Descifra y reensambla un archivo usando la semilla que el usuario guardó.
    /// </summary>
    Task<byte[]> DownloadAndReassembleAsync(int archivoId, string seed, CancellationToken cancellationToken = default);
}
