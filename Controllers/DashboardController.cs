using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Contracts;

namespace ProyectoInnovador.Controllers;

public class DashboardController : Controller
{
    private readonly ISecurityCheckOrchestrator _orchestrator;
    private readonly IFileOrchestrator _fileOrchestrator;
    private readonly ApplicationDbContext _db;
    // Sin límite fijo — Kestrel ya está configurado con long.MaxValue en Program.cs
    private const long MaxFileSizeBytes = long.MaxValue;

    public DashboardController(
        ISecurityCheckOrchestrator orchestrator,
        IFileOrchestrator fileOrchestrator,
        ApplicationDbContext db)
    {
        _orchestrator    = orchestrator;
        _fileOrchestrator = fileOrchestrator;
        _db              = db;
    }

    // GET /Dashboard
    public IActionResult Index()
    {
        ViewData["Title"]      = "Panel Principal";
        ViewData["ActivePage"] = "Dashboard";
        return View();
    }

    // POST /Dashboard/Encrypt
    [HttpPost]
    [ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Encrypt(IFormFile archivo, CancellationToken ct)
    {
        ViewData["Title"]      = "Panel Principal";
        ViewData["ActivePage"] = "Dashboard";

        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Debes seleccionar un archivo antes de encriptar.";
            return RedirectToAction(nameof(Index));
        }

        // Sin límite de tamaño — Kestrel configurado como long.MaxValue en Program.cs

        try
        {
            // El servidor genera la semilla internamente — el usuario nunca la elige
            var (result, seed) = await _orchestrator.UploadMultiCloudAsync(archivo, ct);

            // La semilla viaja UNA sola vez por TempData (se elimina tras la siguiente request)
            // Nunca se guarda en DB, logs, ni cookies.
            TempData["Seed"]        = seed;
            TempData["SeedArchivo"] = result.ArchivoOriginal.Nombre;
            TempData["SeedId"]      = result.ArchivoOriginal.Id.ToString();

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
        {
            TempData["Error"] = "La operación fue cancelada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al encriptar: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST /Dashboard/Decrypt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decrypt(int archivoId, string seed, CancellationToken ct)
    {
        if (archivoId <= 0)
        {
            TempData["Error"] = "ID de archivo inválido.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(seed))
        {
            TempData["Error"] = "Debes ingresar la semilla que recibiste al cifrar el archivo.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var bytes         = await _fileOrchestrator.DownloadAndReassembleAsync(archivoId, seed.Trim(), ct);
            var archivo       = await _db.ArchivosOriginales.FindAsync([archivoId], ct);
            var nombreArchivo = archivo?.Nombre ?? $"archivo_recuperado_{archivoId}.bin";

            return File(bytes, "application/octet-stream", nombreArchivo);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al descifrar: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
