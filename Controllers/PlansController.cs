using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Security.Filters;

namespace ProyectoInnovador.Controllers;

[RequireSession]
public class PlansController : Controller
{
    private readonly ApplicationDbContext _db;
    private static readonly HashSet<string> PaidPlans = new(StringComparer.OrdinalIgnoreCase)
    {
        "Premium",
        "Enterprise"
    };
    private static readonly HashSet<string> AllowedPlans = new(StringComparer.OrdinalIgnoreCase)
    {
        "Free",
        "Premium",
        "Enterprise"
    };

    public PlansController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /Plans
    public IActionResult Index()
    {
        ViewData["Title"]      = "Planes";
        ViewData["ActivePage"] = "Plans";
        return View();
    }

    // GET /Plans/Checkout
    [HttpGet]
    public IActionResult Checkout(string plan)
    {
        ViewData["Title"]      = "Checkout";
        ViewData["ActivePage"] = "Plans";

        if (string.IsNullOrWhiteSpace(plan) || !PaidPlans.Contains(plan))
        {
            TempData["Error"] = "Plan inválido para checkout.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["CheckoutPlan"] = plan;
        return View();
    }

    // POST /Plans/Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string plan, string cardHolder, string cardNumber, string cardExpiry, string cardCvv)
    {
        ViewData["Title"]      = "Checkout";
        ViewData["ActivePage"] = "Plans";

        if (string.IsNullOrWhiteSpace(plan) || !PaidPlans.Contains(plan))
        {
            TempData["Error"] = "Plan inválido para checkout.";
            return RedirectToAction(nameof(Index));
        }

        // Validaciones mínimas (simulación de pago)
        if (string.IsNullOrWhiteSpace(cardHolder) || string.IsNullOrWhiteSpace(cardNumber) ||
            string.IsNullOrWhiteSpace(cardExpiry) || string.IsNullOrWhiteSpace(cardCvv))
        {
            TempData["Error"] = "Completa los datos de la tarjeta para simular el pago.";
            ViewData["CheckoutPlan"] = plan;
            return View();
        }

        var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre");
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            TempData["Error"] = "Sesión inválida. Inicia sesión nuevamente.";
            return RedirectToAction("Login", "Account");
        }

        if (nombreUsuario == "cerberus_admin")
        {
            TempData["Error"] = "El usuario admin hardcodeado no puede cambiar de plan.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

        if (usuario is null)
        {
            TempData["Error"] = "No se encontró el usuario en la base de datos.";
            return RedirectToAction(nameof(Index));
        }

        // Simular pago exitoso → actualizar plan
        usuario.Plan = AllowedPlans.First(p => p.Equals(plan, StringComparison.OrdinalIgnoreCase));
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString("UsuarioPlan", usuario.Plan);
        TempData["Success"] = $"Pago simulado exitoso. Tu plan ahora es {usuario.Plan}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Plans/Upgrade
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(string plan)
    {
        ViewData["Title"]      = "Planes";
        ViewData["ActivePage"] = "Plans";

        if (string.IsNullOrWhiteSpace(plan) || !AllowedPlans.Contains(plan))
        {
            TempData["Error"] = "Plan inválido. Selecciona Free, Premium o Enterprise.";
            return RedirectToAction(nameof(Index));
        }

        var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre");
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            TempData["Error"] = "Sesión inválida. Inicia sesión nuevamente.";
            return RedirectToAction("Login", "Account");
        }

        if (nombreUsuario == "cerberus_admin")
        {
            TempData["Error"] = "El usuario admin hardcodeado no puede cambiar de plan.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

        if (usuario is null)
        {
            TempData["Error"] = "No se encontró el usuario en la base de datos.";
            return RedirectToAction(nameof(Index));
        }

        usuario.Plan = AllowedPlans.First(p => p.Equals(plan, StringComparison.OrdinalIgnoreCase));
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString("UsuarioPlan", usuario.Plan);
        TempData["Success"] = $"Plan actualizado a {usuario.Plan}.";
        return RedirectToAction(nameof(Index));
    }
}
