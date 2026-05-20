namespace ProyectoInnovador.Security.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Redirige al login si no hay sesión activa.
/// Aplicar como [RequireSession] en controllers o actions protegidas.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSessionAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (string.IsNullOrEmpty(session.GetString("UsuarioNombre")))
        {
            var returnUrl = context.HttpContext.Request.Path;
            context.Result = new RedirectToActionResult("Login", "Account",
                new { returnUrl });
        }
    }
}
