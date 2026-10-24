using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using RestauranteControl.Data;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

public abstract class BaseController : Controller
{
    protected int UsuarioId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var servicios = context.HttpContext.RequestServices;
        var cfg = servicios.GetRequiredService<ConfigService>();
        ViewData["Empresa"] = await cfg.EmpresaAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            // Un usuario desactivado pierde el acceso de inmediato
            var permisos = servicios.GetRequiredService<PermisoService>();
            if (!await permisos.UsuarioValidoAsync())
            {
                await servicios.GetRequiredService<AuthService>().CerrarSesionAsync();
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            var controlador = context.RouteData.Values["controller"]?.ToString();
            if (User.FindFirstValue("DebeCambiarPassword") == "1" && controlador != "Account")
            {
                context.Result = RedirectToAction("CambiarPassword", "Account");
                return;
            }
            ViewData["Refresco"] = await cfg.EnteroAsync(Param.UiRefrescoSegundos);
        }
        await next();
    }

    protected async Task<bool> PuedeAsync(string modulo, string accion)
        => await HttpContext.RequestServices.GetRequiredService<PermisoService>().PuedeAsync(modulo, accion);

    protected IActionResult SinPermiso() => View("~/Views/Account/AccesoDenegado.cshtml");

    protected void Exito(string mensaje) => TempData["Ok"] = mensaje;

    protected void Falla(Exception ex) => TempData["Error"] = MensajeDeError(ex);

    protected void Falla(string mensaje) => TempData["Error"] = mensaje;

    protected static string MensajeDeError(Exception ex)
    {
        var raiz = ex.GetBaseException();
        if (raiz is SqlException sql)
        {
            if (sql.Number >= 50000)
            {
                return sql.Message;
            }
            return sql.Number switch
            {
                547 => "La operación incumple una regla de integridad: el registro está relacionado con otros datos.",
                2601 or 2627 => "Ya existe un registro con esos datos.",
                _ => "Error de base de datos: " + sql.Message
            };
        }
        return raiz.Message;
    }

    protected static bool? ComoBool(string? valor) => valor == null ? null : valor.Contains("true", StringComparison.OrdinalIgnoreCase);
}
