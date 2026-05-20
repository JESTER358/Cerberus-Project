using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using System.Text;

namespace ProyectoInnovador.Controllers;

public class LogController : Controller
{
    private readonly ApplicationDbContext _db;

    public LogController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /Log
    public async Task<IActionResult> Index()
    {
        ViewData["Title"]      = "Historial de Auditoría";
        ViewData["ActivePage"] = "Logs";

        var archivos = await _db.ArchivosOriginales
            .Include(a => a.Fragmentos)
            .OrderByDescending(a => a.Id)
            .ToListAsync();

        return View(archivos);
    }

    // GET /Log/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var archivo = await _db.ArchivosOriginales
            .Include(a => a.Fragmentos)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
        {
            TempData["Error"] = $"No existe un registro con ID #{id}.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"]      = $"Detalle del Registro #{id}";
        ViewData["ActivePage"] = "Logs";
        return View(archivo);
    }

    // GET /Log/Download
    public async Task<IActionResult> Download()
    {
        var archivos = await _db.ArchivosOriginales
            .Include(a => a.Fragmentos)
            .OrderByDescending(a => a.Id)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Nombre,Tamaño (bytes),SHA-256,Fragmentos,Proveedores");

        foreach (var a in archivos)
        {
            var proveedores = string.Join(" | ", a.Fragmentos.Select(f => f.CloudProvider));
            var numFragmentos = a.Fragmentos.Count;
            sb.AppendLine($"{a.Id},\"{a.Nombre}\",{a.Tamano},{a.HashSha256},{numFragmentos},\"{proveedores}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"cerberus_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
