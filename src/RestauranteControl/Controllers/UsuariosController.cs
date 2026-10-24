using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Usuarios)]
public class UsuariosController : BaseController
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public UsuariosController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await _db.Roles.AsNoTracking().Where(r => r.Activo).OrderBy(r => r.Nombre).ToListAsync();
        var usuarios = await _db.Usuarios.AsNoTracking().Include(u => u.Rol).OrderBy(u => u.Nombre).ToListAsync();
        return View(usuarios);
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(int? id, string nombre, string email, string? password, int rolId, bool activo = false, bool forzarCambio = false)
    {
        var editando = id.HasValue && id.Value > 0;
        if (!await PuedeAsync(Modulos.Usuarios, editando ? Acciones.Editar : Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            if (editando && id == UsuarioId && !activo)
            {
                throw new InvalidOperationException("No puede desactivar su propio usuario.");
            }
            string? hash = null;
            if (!string.IsNullOrEmpty(password))
            {
                var error = await _auth.ValidarPasswordAsync(password);
                if (error != null)
                {
                    throw new InvalidOperationException(error);
                }
                hash = BCrypt.Net.BCrypt.HashPassword(password);
            }
            await _db.EjecutarAsync("dbo.sp_Usuario_Guardar",
                ("Id", editando ? id : null), ("Nombre", nombre), ("Email", email), ("PasswordHash", hash),
                ("RolId", rolId), ("Activo", editando ? activo : true),
                ("DebeCambiarPassword", hash != null && (forzarCambio || !editando)), ("UsuarioId", UsuarioId));
            Exito("Usuario guardado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }
}
