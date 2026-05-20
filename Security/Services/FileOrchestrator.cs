using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    private readonly IAzureService _azureService;
    private readonly IAesRoundtripService _aesRoundtripService;

    /// <summary>
    /// Constructor de FileOrchestrator.
    /// </summary>
    public FileOrchestrator(
        ApplicationDbContext dbContext,
        S3Service s3Service,
        IAzureService azureService,
        IAesRoundtripService aesRoundtripService)
    {
        _dbContext = dbContext;
        _s3Service = s3Service;
        _azureService = azureService;
        _aesRoundtripService = aesRoundtripService;
    }

    /// <summary>
    /// Recupera un archivo desde las nubes, lo reensambla, lo desencripta y verifica su integridad.
    /// </summary>
    /// <param name="archivoId">El identificador del archivo original en la base de datos.</param>
    /// <param name="userKey">Clave AES-256 del usuario (32 bytes). El servidor nunca la persiste.</param>
    /// <param name="cancellationToken">Token para cancelar la operación asíncrona.</param>
    /// <returns>Arreglo de bytes con el contenido original desencriptado.</returns>
    /// <exception cref="ArgumentException">Se lanza si el archivo no existe o no tiene los fragmentos requeridos.</exception>
    /// <exception cref="CryptographicException">Se lanza si la verificación de integridad SHA-256 falla.</exception>
    public async Task<byte[]> DownloadAndReassembleAsync(int archivoId, string seed, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(seed))
            throw new ArgumentException("La semilla es requerida.", nameof(seed));

        var archivo = await _dbContext.ArchivosOriginales
            .Include(a => a.Fragmentos)
            .FirstOrDefaultAsync(a => a.Id == archivoId, cancellationToken);

        if (archivo == null)
            throw new ArgumentException("Archivo no encontrado", nameof(archivoId));

        // Validar semilla ANTES de descargar nada de las nubes
        var seedHashIngresado = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(seed))).ToLowerInvariant();

        if (!string.Equals(seedHashIngresado, archivo.SeedHash, StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException(
                "Semilla incorrecta. Verifica que copiaste exactamente la semilla que recibiste al cifrar.");

        var fragmentoS3 = archivo.Fragmentos.FirstOrDefault(f => f.CloudProvider == "AWS");
        var fragmentoAzure = archivo.Fragmentos.FirstOrDefault(f => f.CloudProvider == "Azure");

        if (fragmentoS3 == null || fragmentoAzure == null)
            throw new ArgumentException("El archivo no tiene todos los fragmentos requeridos en S3 y Azure.");

        // 1. Descarga Asíncrona en Paralelo usando Task.WhenAll
        var s3Task = _s3Service.DescargarFragmentoAsync(fragmentoS3.UrlRemota, cancellationToken);
        var azureTask = _azureService.DownloadFragmentAsync(fragmentoAzure.UrlRemota, cancellationToken);

        await Task.WhenAll(s3Task, azureTask);

        // Uso de bloques using para la gestión de limpieza de archivos temporales/streams y liberación de memoria inmediata.
        using var streamS3 = await s3Task;
        using var streamAzure = await azureTask;

        using var memoryStream = new MemoryStream();

        // 2. Reensamblaje Secuencial
        await streamS3.CopyToAsync(memoryStream, cancellationToken);
        await streamAzure.CopyToAsync(memoryStream, cancellationToken);

        var reensambladoBytes = memoryStream.ToArray();

        // 3. Desencriptación AES-256 con la clave del usuario
        // El IV ocupa los primeros 16 bytes; el resto es el ciphertext.
        if (reensambladoBytes.Length <= 16)
            throw new CryptographicException("El criptograma reensamblado es inválido (demasiado corto).");

        var iv = reensambladoBytes.Take(16).ToArray();
        var ciphertext = reensambladoBytes.Skip(16).ToArray();

        var payload = new AesPayload
        {
            Iv = iv,
            Ciphertext = ciphertext,
            KeyVersion = "user-key"
        };

        // Derivar la clave AES desde la semilla (mismo algoritmo que al cifrar)
        var aesKey = SecurityCheckOrchestrator.DeriveKeyFromSeed(seed);

        byte[] decryptedBytes;
        try
        {
            decryptedBytes = _aesRoundtripService.Decrypt(payload, aesKey);
        }
        catch (Exception ex)
        {
            throw new CryptographicException(
                "Fallo al descifrar. El archivo puede estar corrompido.", ex);
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
