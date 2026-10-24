using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;

namespace RestauranteControl.Services;

/// <summary>Resuelve los permisos del usuario actual (por módulo y acción) desde la base de datos.</summary>
public class PermisoService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private bool _cargado;
    private bool _usuarioValido;
    private bool _esAdmin;
    private Dictionary<string, RolPermiso> _permisos = new();
    private List<Modulo> _modulos = new();

    public PermisoService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    private async Task CargarAsync()
    {
        if (_cargado)
        {
            return;
        }
        _cargado = true;

        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true || !int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
        {
            return;
        }

        var u = await _db.Usuarios.AsNoTracking().Include(x => x.Rol).FirstOrDefaultAsync(x => x.Id == uid);
        if (u == null || !u.Activo || u.Rol == null || !u.Rol.Activo)
        {
            return;
        }

        _usuarioValido = true;
        _esAdmin = u.Rol.EsAdministrador;
        _modulos = await _db.Modulos.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Orden).ToListAsync();
        var filas = await _db.RolPermisos.AsNoTracking().Include(x => x.Modulo).Where(x => x.RolId == u.RolId).ToListAsync();
        _permisos = filas.Where(f => f.Modulo != null).ToDictionary(f => f.Modulo!.Codigo, f => f);
    }

    public async Task<bool> UsuarioValidoAsync()
    {
        await CargarAsync();
        return _usuarioValido;
    }

    public async Task<bool> PuedeAsync(string modulo, string accion)
    {
        await CargarAsync();
        if (!_usuarioValido)
        {
            return false;
        }
        if (_esAdmin)
        {
            return true;
        }
        if (!_permisos.TryGetValue(modulo, out var p))
        {
            return false;
        }
        return accion switch
        {
            "Ver" => p.Ver,
            "Crear" => p.Crear,
            "Editar" => p.Editar,
            "Eliminar" => p.Eliminar,
            _ => false
        };
    }

    /// <summary>Módulos que el usuario puede ver (para construir el menú).</summary>
    public async Task<List<Modulo>> MenuAsync()
    {
        await CargarAsync();
        if (!_usuarioValido)
        {
            return new List<Modulo>();
        }
        if (_esAdmin)
        {
            return _modulos;
        }
        return _modulos.Where(m => _permisos.TryGetValue(m.Codigo, out var p) && p.Ver).ToList();
    }
}
