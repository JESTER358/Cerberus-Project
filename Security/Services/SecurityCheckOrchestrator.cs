using Microsoft.AspNetCore.Http;
using ProyectoInnovador.Data;
using ProyectoInnovador.Models;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Models;
using System.Security.Cryptography;

namespace ProyectoInnovador.Security.Services;

public sealed class SecurityCheckOrchestrator : ISecurityCheckOrchestrator
{
    private const int BufferSize = 81920;
    private readonly IFileCryptoService _fileCryptoService;
    private readonly IIntegrityHashService _integrityHashService;
    private readonly ApplicationDbContext _dbContext;
    private readonly S3Service _s3Service;
    private readonly AzureService _azureService;

    public SecurityCheckOrchestrator(
        IFileCryptoService fileCryptoService,
        IIntegrityHashService integrityHashService,
        ApplicationDbContext dbContext,
        S3Service s3Service,
        AzureService azureService)
    {
        _fileCryptoService = fileCryptoService;
        _integrityHashService = integrityHashService;
        _dbContext = dbContext;
        _s3Service = s3Service;
        _azureService = azureService;
    }

    public FileRoundtripResult RunFileRoundtrip(byte[] originalBytes)
    {
        ArgumentNullException.ThrowIfNull(originalBytes);
        var encrypted = _fileCryptoService.EncryptFile(originalBytes);
        var decryptedBytes = _fileCryptoService.DecryptFile(encrypted);

        var originalHash = _integrityHashService.ComputeSha256(originalBytes);
        var decryptedHash = _integrityHashService.ComputeSha256(decryptedBytes);

        return new FileRoundtripResult
        {
            Encrypted = encrypted,
            DecryptedBytes = decryptedBytes,
            OriginalHash = originalHash,
            DecryptedHash = decryptedHash,
            IntegrityPass = _integrityHashService.AreEqual(originalHash, decryptedHash)
        };
    }

    public FileRoundtripResult RunTamperedCiphertextScenario(byte[] originalBytes)
    {
        ArgumentNullException.ThrowIfNull(originalBytes);
        var encrypted = _fileCryptoService.EncryptFile(originalBytes);
        var tampered = encrypted.Ciphertext.ToArray();
        if (tampered.Length == 0)
        {
            tampered = [0x01];
        }
        else
        {
            tampered[0] ^= 0xFF;
        }

        byte[] decryptedBytes;
        try
        {
            decryptedBytes = _fileCryptoService.DecryptFile(new AesPayload
            {
                Ciphertext = tampered,
                Iv = encrypted.Iv,
                KeyVersion = encrypted.KeyVersion
            });
        }
        catch
        {
            decryptedBytes = [];
        }

        var originalHash = _integrityHashService.ComputeSha256(originalBytes);
        var decryptedHash = _integrityHashService.ComputeSha256(decryptedBytes);

        return new FileRoundtripResult
        {
            Encrypted = encrypted,
            DecryptedBytes = decryptedBytes,
            OriginalHash = originalHash,
            DecryptedHash = decryptedHash,
            IntegrityPass = _integrityHashService.AreEqual(originalHash, decryptedHash)
        };
    }

    public string ComputeSha256(byte[] data)
    {
        return _integrityHashService.ComputeSha256(data);
    }

    public async Task<MultiCloudUploadResult> UploadMultiCloudAsync(IFormFile archivo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archivo);
        if (archivo.Length <= 0)
            throw new ArgumentException("archivo es requerido", nameof(archivo));
        if (archivo.Length > int.MaxValue)
            throw new ArgumentException("archivo demasiado grande para procesar", nameof(archivo));

        var nombreArchivo = Path.GetFileName(archivo.FileName);

        using var memoryStream = new MemoryStream();
        await archivo.CopyToAsync(memoryStream, cancellationToken);
        var originalBytes = memoryStream.ToArray();

        var hashArchivoHex = _integrityHashService.ComputeSha256(originalBytes);
        var encryptedPayload = _fileCryptoService.EncryptFile(originalBytes);
        var encryptedBytes = encryptedPayload.Iv.Concat(encryptedPayload.Ciphertext).ToArray();

        long tamanoTotalEncrypted = encryptedBytes.Length;
        long tamanoA = tamanoTotalEncrypted / 2 + (tamanoTotalEncrypted % 2);

        var fragmentoA = $"{nombreArchivo}.part_A";
        var fragmentoB = $"{nombreArchivo}.part_B";

        var bytesA = encryptedBytes.Take((int)tamanoA).ToArray();
        var bytesB = encryptedBytes.Skip((int)tamanoA).ToArray();

        var hashAHex = _integrityHashService.ComputeSha256(bytesA);
        var hashBHex = _integrityHashService.ComputeSha256(bytesB);

        using var streamA = new MemoryStream(bytesA);
        using var streamB = new MemoryStream(bytesB);

        var s3ETag = await _s3Service.SubirFragmentoAsync(fragmentoA, streamA, cancellationToken);
        await _azureService.SubirFragmentoAzureAsync(fragmentoB, streamB, cancellationToken);

        var archivoOriginal = new ArchivoOriginal
        {
            Nombre = nombreArchivo,
            Tamano = archivo.Length,
            HashSha256 = hashArchivoHex,
            Fragmentos = new List<Fragmento>
            {
                new Fragmento { CloudProvider = "AWS", UrlRemota = fragmentoA, HashFragmento = hashAHex },
                new Fragmento { CloudProvider = "Azure", UrlRemota = fragmentoB, HashFragmento = hashBHex }
            }
        };

        _dbContext.ArchivosOriginales.Add(archivoOriginal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MultiCloudUploadResult
        {
            ArchivoOriginal = archivoOriginal,
            S3ETag = s3ETag
        };
    }
    private static void TryDeleteFile(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
