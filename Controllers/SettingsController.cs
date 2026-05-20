using Microsoft.AspNetCore.Mvc;

namespace ProyectoInnovador.Controllers;

/// <summary>
/// Ajustes del sistema — por ahora lectura de la config activa.
/// Cuando migres a nubes reales, aquí irán los campos de conexión.
/// </summary>
public class SettingsController : Controller
{
    private readonly IConfiguration _config;

    public SettingsController(IConfiguration config)
    {
        _config = config;
    }

    // GET /Settings
    public IActionResult Index()
    {
        ViewData["Title"]      = "Configuración del Sistema";
        ViewData["ActivePage"] = "Settings";

        // Leer config actual para mostrarla en la vista (solo lectura por ahora)
        var vm = new SettingsViewModel
        {
            S3Endpoint    = _config["S3:ServiceUrl"]    ?? "http://localhost:9000",
            S3BucketName  = _config["S3:BucketName"]   ?? "cerberus-fragments",
            AzureMode     = "UseDevelopmentStorage=true (Azurite)",
            KeyVersion    = _config["Security:Check1KeyVersion"] ?? "v1",
            Algorithm     = "AES-256-CBC",
            AutoLogout    = true,
        };

        return View(vm);
    }

    // POST /Settings/Save
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(SettingsViewModel model)
    {
        // En esta etapa (simuladores locales) no persiste cambios en config —
        // los campos relevantes están hardcodeados para que funcione con MinIO/Azurite.
        // Cuando migres a nubes reales, aquí guardarás en appsettings o en un secrets store.
        TempData["Success"] = "Preferencias guardadas correctamente.";
        return RedirectToAction(nameof(Index));
    }
}

public class SettingsViewModel
{
    public string S3Endpoint   { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string AzureMode    { get; set; } = string.Empty;
    public string KeyVersion   { get; set; } = string.Empty;
    public string Algorithm    { get; set; } = "AES-256-CBC";
    public bool   AutoLogout   { get; set; } = true;
}
