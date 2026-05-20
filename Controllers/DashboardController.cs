using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Contracts;
using ProyectoInnovador.Security.Filters;

namespace ProyectoInnovador.Controllers;

[RequireSession]
public class DashboardController : Controller
{
    private readonly ISecurityCheckOrchestrator _orchestrator;
    private readonly IFileOrchestrator _fileOrchestrator;
    private readonly ApplicationDbContext _db;
    // Sin límite fijo — Kestrel ya está configurado con long.MaxValue en Program.cs
    private const long MaxFileSizeBytes = long.MaxValue;

    // ── Límites por plan ──────────────────────────────────────────────────────
    // Free: 3 archivos máx, 10 MB por archivo
    // Premium: 50 archivos máx, 200 MB por archivo
    // Enterprise: ilimitado
    private static readonly Dictionary<string, (int MaxArchivos, long MaxBytes)> PlanLimits = new()
    {
        ["Free"]       = (3,   10  * 1024 * 1024),   // 3 archivos, 10 MB
        ["Premium"]    = (50,  200 * 1024 * 1024),   // 50 archivos, 200 MB
        ["Enterprise"] = (int.MaxValue, long.MaxValue) // ilimitado
    };

    // ── Límite mensual por plan (GB/mes) ────────────────────────────────────
    // Free: 1 GB/mes, Premium: 50 GB/mes, Enterprise: ilimitado
    private static readonly Dictionary<string, long> MonthlyPlanLimitsBytes = new()
    {
        ["Free"]       = 1L  * 1024 * 1024 * 1024,   // 1 GB
        ["Premium"]    = 50L * 1024 * 1024 * 1024,   // 50 GB
        ["Enterprise"] = long.MaxValue
    };

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

        // Resolver UsuarioId desde la sesión
        int? usuarioId = null;
        var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre");
        if (nombreUsuario != "cerberus_admin" && !string.IsNullOrEmpty(nombreUsuario))
        {
            var usuarioDb = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, ct);
            usuarioId = usuarioDb?.Id;
        }

        // ── Validar límites del plan ──────────────────────────────────────────────
        var plan = HttpContext.Session.GetString("UsuarioPlan") ?? "Free";
        if (!PlanLimits.TryGetValue(plan, out var limites))
            limites = PlanLimits["Free"];

        // Límite de tamaño por archivo
        if (archivo.Length > limites.MaxBytes)
        {
            var mbMax = limites.MaxBytes / 1024 / 1024;
            TempData["Error"] = $"Tu plan {plan} permite archivos de hasta {mbMax} MB. " +
                                "Actualiza a Premium o Enterprise para subir archivos más grandes.";
            return RedirectToAction(nameof(Index));
        }

        // Límite de cantidad de archivos
        var totalArchivos = await _db.ArchivosOriginales
            .CountAsync(a => a.UsuarioId == usuarioId, ct);
        if (totalArchivos >= limites.MaxArchivos)
        {
            TempData["Error"] = $"Tu plan {plan} permite un máximo de {limites.MaxArchivos} archivo(s). " +
                                "Actualiza tu plan para continuar.";
            return RedirectToAction(nameof(Index));
        }

        // Límite mensual de GB por plan (suma de archivos subidos en el mes actual)
        if (!MonthlyPlanLimitsBytes.TryGetValue(plan, out var limiteMensualBytes))
            limiteMensualBytes = MonthlyPlanLimitsBytes["Free"];

        if (limiteMensualBytes != long.MaxValue)
        {
            var nowUtc = DateTime.UtcNow;
            var inicioMes = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var bytesMesActual = await _db.ArchivosOriginales
                .Where(a => a.UsuarioId == usuarioId && a.DateCreatedUtc >= inicioMes)
                .SumAsync(a => (long?)a.Tamano, ct) ?? 0L;

            if (bytesMesActual + archivo.Length > limiteMensualBytes)
            {
                var gbMax = (double)limiteMensualBytes / 1024 / 1024 / 1024;
                TempData["Error"] = $"Tu plan {plan} permite {gbMax:0.#} GB por mes. " +
                                    "Actualiza a Premium o Enterprise para ampliar tu cuota mensual.";
                return RedirectToAction(nameof(Index));
            }
        }

        try
        {
            // El servidor genera la semilla internamente — el usuario nunca la elige
            var (result, seed) = await _orchestrator.UploadMultiCloudAsync(archivo, usuarioId, ct);

            // La semilla viaja UNA sola vez por TempData (se elimina tras la siguiente request)
            // Nunca se guarda en DB, logs, ni cookies.
            TempData["Seed"]        = seed;
            TempData["SeedArchivo"] = result.ArchivoOriginal.Nombre;
            TempData["SeedId"]      = result.ArchivoOriginal.PublicId.ToString();

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
    public async Task<IActionResult> Decrypt(string publicId, string seed, CancellationToken ct)
    {
        if (!Guid.TryParse(publicId, out var guid))
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
            // Resolver el int Id interno desde el PublicId
            var archivoMeta = await _db.ArchivosOriginales
                .FirstOrDefaultAsync(a => a.PublicId == guid, ct);
            if (archivoMeta is null)
            {
                TempData["Error"] = "No se encontró un archivo con ese ID.";
                return RedirectToAction(nameof(Index));
            }
            var bytes         = await _fileOrchestrator.DownloadAndReassembleAsync(archivoMeta.Id, seed.Trim(), ct);
            var archivo       = archivoMeta;
            var nombreArchivo = archivo.Nombre;

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
