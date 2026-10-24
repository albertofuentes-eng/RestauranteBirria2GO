using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Menu)]
public class MenuController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ComboService _combos;

    public MenuController(AppDbContext db, ComboService combos)
    {
        _db = db;
        _combos = combos;
    }

    public async Task<IActionResult> Index()
    {
        await _combos.SincronizarAsync();
        var catCombosNombre = await _combos.NombreCategoriaAsync();
        ViewBag.CategoriaCombos = catCombosNombre;
        ViewBag.Categorias = await _db.CategoriasProducto.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.PuedeVerPromos = await PuedeAsync(Modulos.Promociones, Acciones.Ver);

        // 1. Obtenemos todas las promociones activas con sus detalles y productos
        var todasLasPromos = await _db.Promociones.AsNoTracking()
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                    .ThenInclude(pr => pr!.CategoriaProducto)
            .Where(p => p.Activa)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        // 2. Filtramos ÚNICAMENTE las ofertas (excluyendo cualquier promoción que pertenezca a la categoría de combos)
        ViewBag.Promociones = todasLasPromos
            .Where(p => !p.Detalles.Any(d => d.Producto?.CategoriaProducto?.Nombre == catCombosNombre))
            .ToList();

        var productos = await _db.Productos.AsNoTracking().Include(p => p.CategoriaProducto)
            .OrderBy(p => p.CategoriaProducto!.Nombre).ThenBy(p => p.Nombre).ToListAsync();

        return View(productos);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarProducto(int? id, string nombre, string? descripcion, int categoriaProductoId,
                                                    decimal precio, bool disponible = false, bool activo = false, IFormFile? imagen = null)
    {
        var editando = id.HasValue && id.Value > 0;
        if (!await PuedeAsync(Modulos.Menu, editando ? Acciones.Editar : Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            string? imagenUrl = null;

            // Si se subió un archivo de imagen, lo guardamos en wwwroot/uploads
            if (imagen != null && imagen.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imagen.FileName)}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }

                imagenUrl = $"/uploads/{fileName}";
            }

            await _db.EjecutarAsync("dbo.sp_Producto_Guardar",
                ("Id", editando ? id : null),
                ("Nombre", nombre),
                ("Descripcion", descripcion),
                ("CategoriaId", categoriaProductoId),
                ("Precio", precio),
                ("Disponible", editando ? disponible : true),
                ("Activo", editando ? activo : true),
                ("ImagenUrl", imagenUrl),
                ("UsuarioId", UsuarioId));

            Exito("Platillo guardado correctamente.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Menu, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> Disponibilidad(int id, bool disponible)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Producto_CambiarDisponibilidad", ("Id", id), ("Disponible", disponible), ("UsuarioId", UsuarioId));
            Exito(disponible ? "Platillo disponible nuevamente." : "Platillo marcado como agotado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Menu, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> EliminarProducto(int id)
    {
        try
        {
            var eliminado = await _db.EjecutarAsync("dbo.sp_Producto_Eliminar", ("Id", id), ("UsuarioId", UsuarioId));
            Exito(eliminado == 1 ? "Platillo eliminado." : "El platillo tiene historial: se desactivó en lugar de eliminarse.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> GuardarCategoria(int? id, string nombre, bool activa = false)
    {
        var editando = id.HasValue && id.Value > 0;
        if (!await PuedeAsync(Modulos.Menu, editando ? Acciones.Editar : Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0)
            {
                throw new InvalidOperationException("El nombre de la categoría es obligatorio.");
            }
            if (await _db.CategoriasProducto.AnyAsync(c => c.Nombre == nombre && c.Id != (id ?? 0)))
            {
                throw new InvalidOperationException("Ya existe una categoría con ese nombre.");
            }
            if (editando)
            {
                var categoria = await _db.CategoriasProducto.FindAsync(id!.Value)
                    ?? throw new InvalidOperationException("La categoría no existe.");
                categoria.Nombre = nombre;
                categoria.Activa = activa;
            }
            else
            {
                _db.CategoriasProducto.Add(new CategoriaProducto { Nombre = nombre, Activa = true });
            }
            await _db.SaveChangesAsync();
            Exito("Categoría guardada.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Menu, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> EliminarCategoria(int id)
    {
        try
        {
            var categoria = await _db.CategoriasProducto.FindAsync(id)
                ?? throw new InvalidOperationException("La categoría no existe.");
            if (await _db.Productos.AnyAsync(p => p.CategoriaProductoId == id))
            {
                categoria.Activa = false;
                await _db.SaveChangesAsync();
                Exito("La categoría tiene platillos: se desactivó en lugar de eliminarse.");
            }
            else
            {
                _db.CategoriasProducto.Remove(categoria);
                await _db.SaveChangesAsync();
                Exito("Categoría eliminada.");
            }
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }
}