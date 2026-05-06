using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Models;

namespace ProyectoInnovador.Security.Services;

/// <summary>
/// Orquestador para la recuperación, reensamblaje y verificación de archivos.
/// </summary>
public sealed class FileOrchestrator : IFileOrchestrator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly S3Service _s3Service;
    private readonly AzureService _azureService;
    private readonly IFileCryptoService _fileCryptoService;

    /// <summary>
    /// Constructor de FileOrchestrator.
    /// </summary>
    public FileOrchestrator(
        ApplicationDbContext dbContext,
        S3Service s3Service,
        AzureService azureService,
        IFileCryptoService fileCryptoService)
    {
        _dbContext = dbContext;
        _s3Service = s3Service;
        _azureService = azureService;
        _fileCryptoService = fileCryptoService;
    }

    /// <summary>
    /// Recupera un archivo desde las nubes, lo reensambla, lo desencripta y verifica su integridad.
    /// </summary>
    /// <param name="archivoId">El identificador del archivo original en la base de datos.</param>
    /// <param name="cancellationToken">Token para cancelar la operación asíncrona.</param>
    /// <returns>Arreglo de bytes con el contenido original desencriptado.</returns>
    /// <exception cref="ArgumentException">Se lanza si el archivo no existe o no tiene los fragmentos requeridos.</exception>
    /// <exception cref="CryptographicException">Se lanza si la verificación de integridad SHA-256 falla.</exception>
    public async Task<byte[]> DownloadAndReassembleAsync(int archivoId, CancellationToken cancellationToken = default)
    {
        var archivo = await _dbContext.ArchivosOriginales
            .Include(a => a.Fragmentos)
            .FirstOrDefaultAsync(a => a.Id == archivoId, cancellationToken);

        if (archivo == null)
            throw new ArgumentException("Archivo no encontrado", nameof(archivoId));

        var fragmentoS3 = archivo.Fragmentos.FirstOrDefault(f => f.CloudProvider == "AWS");
        var fragmentoAzure = archivo.Fragmentos.FirstOrDefault(f => f.CloudProvider == "Azure");

        if (fragmentoS3 == null || fragmentoAzure == null)
            throw new ArgumentException("El archivo no tiene todos los fragmentos requeridos en S3 y Azure.");

        // 1. Descarga Asíncrona en Paralelo usando Task.WhenAll
        var s3Task = _s3Service.DescargarFragmentoAsync(fragmentoS3.UrlRemota, cancellationToken);
        var azureTask = _azureService.DescargarFragmentoAzureAsync(fragmentoAzure.UrlRemota, cancellationToken);

        await Task.WhenAll(s3Task, azureTask);

        // Uso de bloques using para la gestión de limpieza de archivos temporales/streams y liberación de memoria inmediata.
        using var streamS3 = await s3Task;
        using var streamAzure = await azureTask;

        using var memoryStream = new MemoryStream();

        // 2. Reensamblaje Secuencial
        await streamS3.CopyToAsync(memoryStream, cancellationToken);
        await streamAzure.CopyToAsync(memoryStream, cancellationToken);

        var reensambladoBytes = memoryStream.ToArray();

        // 3. Desencriptación AES-256
        // Se asume que el arreglo reensamblado contiene el IV en los primeros 16 bytes y luego el Ciphertext
        // Si la encriptación se manejó diferente, se debe ajustar la creación de AesPayload.
        var iv = reensambladoBytes.Take(16).ToArray();
        var ciphertext = reensambladoBytes.Skip(16).ToArray();

        var payload = new AesPayload
        {
            Iv = iv,
            Ciphertext = ciphertext,
            KeyVersion = "1" // Se asume versión activa, se puede extender si se guarda en DB
        };

        byte[] decryptedBytes;
        try
        {
            decryptedBytes = _fileCryptoService.DecryptFile(payload);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Fallo en la desencriptación AES-256. Verifique las llaves o la integridad del criptograma.", ex);
        }

        // 4. Verificación SHA-256 post-reensamblaje comparando contra el valor en la base de datos
        using var sha256 = SHA256.Create();
        var hashCalculado = sha256.ComputeHash(decryptedBytes);
        var hashHex = Convert.ToHexString(hashCalculado);

        if (!string.Equals(hashHex, archivo.HashSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new CryptographicException($"Fallo de integridad: El hash SHA-256 calculado ({hashHex}) no coincide con el original de la DB ({archivo.HashSha256}).");
        }

        return decryptedBytes;
    }
}
