using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Data;
using ProyectoInnovador.Models;
using ProyectoInnovador.Security.Contracts;
using System.Text;

namespace ProyectoInnovador.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IIntegrityHashService _hash;

    // Usuario admin hardcodeado — siempre disponible aunque la DB esté vacía
    private const string AdminUser = "cerberus_admin";

    // Hash SHA-256 de "Cerberus2026!" precalculado
    private static readonly string AdminHash =
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes("Cerberus2026!")))
        .ToLowerInvariant();

    public AccountController(ApplicationDbContext db, IIntegrityHashService hash)
    {
        _db   = db;
        _hash = hash;
    }

    // ── GET /Account/Login ────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya hay sesión activa → redirige al dashboard
        if (HttpContext.Session.GetString("UsuarioNombre") != null)
            return RedirectToAction("Index", "Dashboard");

        ViewData["Title"]   = "Iniciar Sesión";
        ViewData["HideNav"] = true;
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // ── POST /Account/Login ───────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string nombreUsuario, string password, string? returnUrl = null)
    {
        ViewData["Title"]   = "Iniciar Sesión";
        ViewData["HideNav"] = true;

        // RF-2: validación de campos obligatorios
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Usuario y contraseña son requeridos.";
            return View();
        }

        // RF-4: hash SHA-256 de la contraseña ingresada
        var hashIngresado = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(password)))
            .ToLowerInvariant();

        // Verificar admin hardcodeado primero
        if (nombreUsuario == AdminUser && hashIngresado == AdminHash)
        {
            HttpContext.Session.SetString("UsuarioNombre", AdminUser);
            HttpContext.Session.SetString("UsuarioPlan",   "Enterprise");
            HttpContext.Session.SetString("UsuarioAdmin",  "true");
            return RedirectToLocal(returnUrl);
        }

        // RF-5: buscar en DB
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

        // RF-6: error genérico — no revelar qué campo es incorrecto
        if (usuario == null || usuario.PasswordHash != hashIngresado)
        {
            TempData["Error"] = "Credenciales incorrectas. Verifica tu usuario y contraseña.";
            return View();
        }

        // Sesión activa
        HttpContext.Session.SetString("UsuarioNombre", usuario.NombreUsuario);
        HttpContext.Session.SetString("UsuarioPlan",   usuario.Plan);
        HttpContext.Session.SetString("UsuarioAdmin",  usuario.EsAdmin ? "true" : "false");

        return RedirectToLocal(returnUrl);
    }

    // ── GET /Account/Register ─────────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetString("UsuarioNombre") != null)
            return RedirectToAction("Index", "Dashboard");

        ViewData["Title"]   = "Crear Cuenta";
        ViewData["HideNav"] = true;
        return View();
    }

    // ── POST /Account/Register ────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string nombreUsuario, string email, string password, string passwordConfirm)
    {
        ViewData["Title"]   = "Crear Cuenta";
        ViewData["HideNav"] = true;

        // RF-2: campos obligatorios
        if (string.IsNullOrWhiteSpace(nombreUsuario) ||
            string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Usuario y contraseña son requeridos.";
            return View();
        }

        // Confirmación de contraseña
        if (password != passwordConfirm)
        {
            TempData["Error"] = "Las contraseñas no coinciden.";
            return View();
        }

        // RF-3: unicidad de nombre de usuario
        var existe = await _db.Usuarios
            .AnyAsync(u => u.NombreUsuario == nombreUsuario);
        if (existe)
        {
            TempData["Error"] = "Ese nombre de usuario ya está en uso. Elige otro.";
            return View();
        }

        // RF-4: hash SHA-256
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(password)))
            .ToLowerInvariant();

        var nuevo = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Email         = email ?? "",
            PasswordHash  = hash,
            Plan          = "Free",
            EsAdmin       = false
        };

        _db.Usuarios.Add(nuevo);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Cuenta creada. Bienvenido, {nombreUsuario}.";
        return RedirectToAction(nameof(Login));
    }

    // ── GET /Account/Logout ───────────────────────────────────────
    public IActionResult Logout()
    {
        // RF-7: destruir todas las variables de sesión
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    // ── Helper ────────────────────────────────────────────────────
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }
}
