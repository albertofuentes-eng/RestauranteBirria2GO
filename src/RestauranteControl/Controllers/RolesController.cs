using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Roles)]
public class RolesController : BaseController
{
    private readonly AppDbContext _db;

    public RolesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Usuarios = await _db.Usuarios.AsNoTracking().GroupBy(u => u.RolId)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        return View(await _db.Roles.AsNoTracking().OrderBy(r => r.Nombre).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(int? id, string nombre, string? descripcion, bool activo = false)
    {
        var editando = id.HasValue && id.Value > 0;
        if (!await PuedeAsync(Modulos.Roles, editando ? Acciones.Editar : Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0)
            {
                throw new InvalidOperationException("El nombre del rol es obligatorio.");
            }
            if (await _db.Roles.AnyAsync(r => r.Nombre == nombre && r.Id != (id ?? 0)))
            {
                throw new InvalidOperationException("Ya existe un rol con ese nombre.");
            }
            if (editando)
            {
                var rol = await _db.Roles.FindAsync(id!.Value) ?? throw new InvalidOperationException("El rol no existe.");
                if (rol.EsAdministrador && !activo)
                {
                    throw new InvalidOperationException("El rol administrador no puede desactivarse.");
                }
                rol.Nombre = nombre;
                rol.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
                rol.Activo = activo;
            }
            else
            {
                _db.Roles.Add(new Rol { Nombre = nombre, Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim() });
            }
            await _db.SaveChangesAsync();
            Exito("Rol guardado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Roles, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var rol = await _db.Roles.FindAsync(id) ?? throw new InvalidOperationException("El rol no existe.");
            if (rol.EsAdministrador)
            {
                throw new InvalidOperationException("El rol administrador no puede eliminarse.");
            }
            if (await _db.Usuarios.AnyAsync(u => u.RolId == id))
            {
                throw new InvalidOperationException("Hay usuarios con este rol; reasígnelos o desactive el rol.");
            }
            var permisos = await _db.RolPermisos.Where(p => p.RolId == id).ToListAsync();
            _db.RolPermisos.RemoveRange(permisos);
            _db.Roles.Remove(rol);
            await _db.SaveChangesAsync();
            Exito("Rol eliminado.");
        }
        catch (Exception ex) { _db.ChangeTracker.Clear(); Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Permisos(int id)
    {
        var rol = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (rol == null)
        {
            Falla("El rol no existe.");
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Rol = rol;
        ViewBag.Modulos = await _db.Modulos.AsNoTracking().Where(m => m.Activo).OrderBy(m => m.Orden).ToListAsync();
        ViewBag.Permisos = await _db.RolPermisos.AsNoTracking().Where(p => p.RolId == id).ToDictionaryAsync(p => p.ModuloId);
        return View();
    }

    [Permiso(Modulos.Roles, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> GuardarPermisos(int id)
    {
        try
        {
            var rol = await _db.Roles.FindAsync(id) ?? throw new InvalidOperationException("El rol no existe.");
            if (rol.EsAdministrador)
            {
                throw new InvalidOperationException("El administrador tiene acceso total; sus permisos no se editan.");
            }
            var modulos = await _db.Modulos.Where(m => m.Activo).ToListAsync();
            var actuales = await _db.RolPermisos.Where(p => p.RolId == id).ToDictionaryAsync(p => p.ModuloId);
            foreach (var m in modulos)
            {
                bool Marca(string accion) => Request.Form.ContainsKey($"{accion}_{m.Id}");
                var crear = Marca("c");
                var editar = Marca("e");
                var eliminar = Marca("d");
                var ver = Marca("v") || crear || editar || eliminar;
                if (!actuales.TryGetValue(m.Id, out var fila))
                {
                    if (!ver)
                    {
                        continue;
                    }
                    fila = new RolPermiso { RolId = id, ModuloId = m.Id };
                    _db.RolPermisos.Add(fila);
                }
                fila.Ver = ver;
                fila.Crear = crear;
                fila.Editar = editar;
                fila.Eliminar = eliminar;
            }
            await _db.SaveChangesAsync();
            Exito("Permisos actualizados.");
        }
        catch (Exception ex) { _db.ChangeTracker.Clear(); Falla(ex); }
        return RedirectToAction(nameof(Permisos), new { id });
    }
}
