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
    private readonly IAesRoundtripService _aesRoundtripService;
    private readonly IIntegrityHashService _integrityHashService;
    private readonly ApplicationDbContext _dbContext;
    private readonly S3Service _s3Service;
    private readonly IAzureService _azureService;

    public SecurityCheckOrchestrator(
        IFileCryptoService fileCryptoService,
        IAesRoundtripService aesRoundtripService,
        IIntegrityHashService integrityHashService,
        ApplicationDbContext dbContext,
        S3Service s3Service,
        IAzureService azureService)
    {
        _fileCryptoService = fileCryptoService;
        _aesRoundtripService = aesRoundtripService;
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

    /// <summary>
    /// Límite operacional mientras el cifrado opere sobre byte[].
    /// Con 4 copias en RAM simultáneas (original + cifrado + fragmentoA + fragmentoB),
    /// un archivo de 200 MB consume ~800 MB de heap. Aumentar este valor requiere
    /// migrar IAesRoundtripService a CryptoStream primero.
    /// </summary>
    private const long MaxFileSizeBytes = 200 * 1024 * 1024; // 200 MB

    public async Task<(MultiCloudUploadResult Result, string Seed)> UploadMultiCloudAsync(IFormFile archivo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archivo);
        if (archivo.Length <= 0)
            throw new ArgumentException("El archivo está vacío.", nameof(archivo));

        // Guard honesto: el límite real no es int.MaxValue sino la presión de RAM que genera
        // tener original + cifrado + dos fragmentos en heap al mismo tiempo.
        // Cuando IAesRoundtripService opere con CryptoStream, este guard se puede eliminar.
        if (archivo.Length > MaxFileSizeBytes)
            throw new ArgumentException(
                $"El archivo supera el límite operacional de {MaxFileSizeBytes / 1024 / 1024} MB. " +
                "La arquitectura de streaming completo está planificada para la próxima iteración.",
                nameof(archivo));

        var nombreArchivo = Path.GetFileName(archivo.FileName);

        // --- Fase 1: Leer el archivo original en RAM (límite acotado por el guard) ---
        byte[] originalBytes;
        using (var memoryStream = new MemoryStream((int)archivo.Length))
        {
            await archivo.CopyToAsync(memoryStream, cancellationToken);
            originalBytes = memoryStream.ToArray();
        }
        // El MemoryStream ya se liberó. En heap: solo originalBytes.

        // 1. Generar semilla criptográficamente segura (24 bytes → 32 chars Base64url).
        //    El servidor genera la semilla, la usa para cifrar, y luego la olvida.
        //    Solo se devuelve al usuario UNA vez — nunca se persiste en DB.
        var seedBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(24);
        var seed = Convert.ToBase64String(seedBytes)
                          .Replace('+', '-').Replace('/', '_').TrimEnd('='); // Base64url

        // 2. Derivar clave AES-256 desde la semilla con PBKDF2 (100 000 iteraciones, SHA-256).
        var aesKey = DeriveKeyFromSeed(seed);

        // 3. Hash de la semilla para validación futura — el servidor NUNCA persiste la semilla en texto.
        var seedHash = _integrityHashService.ComputeSha256(System.Text.Encoding.UTF8.GetBytes(seed));

        // --- Fase 2: Cifrado AES-256 ---
        var hashArchivoHex   = _integrityHashService.ComputeSha256(originalBytes);
        var encryptedPayload = _aesRoundtripService.Encrypt(originalBytes, aesKey, "seed-v1");

        // originalBytes ya no se necesita — liberar activamente en vez de esperar al GC.
        Array.Clear(originalBytes, 0, originalBytes.Length);
        aesKey = null;

        // IV (16 bytes) + ciphertext concatenados = stream completo para dispersión.
        var encryptedBytes = encryptedPayload.Iv.Concat(encryptedPayload.Ciphertext).ToArray();

        // --- Fase 3: Partición 50/50 ---
        long tamanoTotal = encryptedBytes.Length;
        int  tamanoA     = (int)(tamanoTotal / 2 + tamanoTotal % 2);

        // Hashes de fragmentos calculados ANTES de crear los streams para no tener
        // que rebobinar ni duplicar los arrays.
        var hashAHex = _integrityHashService.ComputeSha256(encryptedBytes[..tamanoA]);
        var hashBHex = _integrityHashService.ComputeSha256(encryptedBytes[tamanoA..]);

        // --- Fase 4: Persistencia en DB (antes de subir — el ID es el prefijo único) ---
        var archivoOriginal = new ArchivoOriginal
        {
            Nombre     = nombreArchivo,
            Tamano     = archivo.Length,
            HashSha256 = hashArchivoHex,
            SeedHash   = seedHash,
            Fragmentos = new List<Fragmento>()
        };

        _dbContext.ArchivosOriginales.Add(archivoOriginal);
        await _dbContext.SaveChangesAsync(cancellationToken); // ← ID asignado aquí

        var fragmentoA = $"{archivoOriginal.Id}_{nombreArchivo}.part_A";
        var fragmentoB = $"{archivoOriginal.Id}_{nombreArchivo}.part_B";

        // --- Fase 5: Subida a las nubes con streams envueltos en using ---
        // MemoryStream(slice) no copia el array — apunta al mismo buffer subyacente.
        // El using garantiza liberación del wrapper al terminar cada subida.
        string s3ETag;
        using (var streamA = new MemoryStream(encryptedBytes, 0, tamanoA, writable: false))
        {
            s3ETag = await _s3Service.SubirFragmentoAsync(fragmentoA, streamA, cancellationToken);
        }

        using (var streamB = new MemoryStream(encryptedBytes, tamanoA, (int)tamanoTotal - tamanoA, writable: false))
        {
            await _azureService.UploadFragmentAsync(fragmentoB, streamB, cancellationToken);
        }

        // encryptedBytes ya cumplió su función — liberar antes del siguiente SaveChanges.
        Array.Clear(encryptedBytes, 0, encryptedBytes.Length);

        // --- Fase 6: Persistir fragmentos en DB ---
        // IMPORTANTE: usar AddRange sobre la colección ya trackeada por EF Core,
        // NO asignar "= new List<>()" — eso rompe el change tracker y la FK queda en 0.
        archivoOriginal.Fragmentos.Add(new Fragmento { CloudProvider = "AWS",   UrlRemota = fragmentoA, HashFragmento = hashAHex });
        archivoOriginal.Fragmentos.Add(new Fragmento { CloudProvider = "Azure", UrlRemota = fragmentoB, HashFragmento = hashBHex });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var uploadResult = new MultiCloudUploadResult
        {
            ArchivoOriginal = archivoOriginal,
            S3ETag          = s3ETag
        };

        // La semilla se devuelve en texto plano — es la ÚNICA vez que existe fuera de RAM.
        return (uploadResult, seed);
    }

    /// <summary>
    /// Deriva 32 bytes AES desde una semilla usando PBKDF2-SHA256.
    /// Salt fijo por diseño — la semilla ya tiene 144 bits de entropía.
    /// </summary>
    internal static byte[] DeriveKeyFromSeed(string seed)
    {
        var seedBytes  = System.Text.Encoding.UTF8.GetBytes(seed);
        var salt       = System.Text.Encoding.UTF8.GetBytes("cerberus-encriptaseguro-v1");
        return System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            seedBytes, salt, iterations: 100_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            outputLength: 32);
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
