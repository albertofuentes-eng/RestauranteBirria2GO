using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestauranteControl.Services;

namespace RestauranteControl.Security;

/// <summary>Exige un permiso (módulo + acción) para ejecutar la acción o el controlador.</summary>
public class PermisoAttribute : TypeFilterAttribute
{
    public PermisoAttribute(string modulo, string accion = Acciones.Ver) : base(typeof(PermisoFilter))
    {
        Arguments = new object[] { modulo, accion };
    }
}

public class PermisoFilter : IAsyncActionFilter
{
    private readonly string _modulo;
    private readonly string _accion;
    private readonly PermisoService _permisos;

    public PermisoFilter(string modulo, string accion, PermisoService permisos)
    {
        _modulo = modulo;
        _accion = accion;
        _permisos = permisos;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _permisos.PuedeAsync(_modulo, _accion))
        {
            context.Result = new ViewResult { ViewName = "~/Views/Account/AccesoDenegado.cshtml", StatusCode = 403 };
            return;
        }
        await next();
    }
}
