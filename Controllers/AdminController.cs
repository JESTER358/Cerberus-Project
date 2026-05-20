using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Filters;

namespace ProyectoInnovador.Controllers;

[RequireSession]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private static readonly HashSet<string> AllowedPlans = new(StringComparer.OrdinalIgnoreCase)
    {
        "Free",
        "Premium",
        "Enterprise"
    };

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    private bool EsAdmin() => HttpContext.Session.GetString("UsuarioAdmin") == "true";

    // GET /Admin
    public async Task<IActionResult> Index()
    {
        if (!EsAdmin())
        {
            TempData["Error"] = "No tienes permisos para acceder a esta sección.";
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["Title"]      = "Admin";
        ViewData["ActivePage"] = "Admin";

        var usuarios = await _db.Usuarios
            .OrderBy(u => u.Id)
            .ToListAsync();

        var totalArchivos = await _db.ArchivosOriginales.CountAsync();

        ViewData["TotalUsuarios"]   = usuarios.Count;
        ViewData["TotalArchivos"]   = totalArchivos;
        ViewData["TotalFree"]       = usuarios.Count(u => u.Plan == "Free");
        ViewData["TotalPagos"]      = usuarios.Count(u => u.Plan != "Free");

        return View(usuarios);
    }

    // POST /Admin/CambiarPlan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPlan(int usuarioId, string plan)
    {
        if (!EsAdmin())
        {
            TempData["Error"] = "No tienes permisos para realizar esta acción.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(plan) || !AllowedPlans.Contains(plan))
        {
            TempData["Error"] = "Plan inválido. Selecciona Free, Premium o Enterprise.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
        {
            TempData["Error"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        usuario.Plan = AllowedPlans.First(p => p.Equals(plan, StringComparison.OrdinalIgnoreCase));
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Plan de {usuario.NombreUsuario} actualizado a {usuario.Plan}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/ToggleAdmin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(int usuarioId)
    {
        if (!EsAdmin())
        {
            TempData["Error"] = "No tienes permisos para realizar esta acción.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
        {
            TempData["Error"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        usuario.EsAdmin = !usuario.EsAdmin;
        await _db.SaveChangesAsync();

        TempData["Success"] = usuario.EsAdmin
            ? $"{usuario.NombreUsuario} ahora es admin."
            : $"{usuario.NombreUsuario} dejó de ser admin.";

        return RedirectToAction(nameof(Index));
    }

    // POST /Admin/EliminarUsuario
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(int usuarioId)
    {
        if (!EsAdmin())
        {
            TempData["Error"] = "No tienes permisos para realizar esta acción.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
        {
            TempData["Error"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Usuario {usuario.NombreUsuario} eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
