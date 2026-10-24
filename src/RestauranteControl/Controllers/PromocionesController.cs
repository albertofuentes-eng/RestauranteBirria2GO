using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Promociones)]
public class PromocionesController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ComboService _combos;

    public PromocionesController(AppDbContext db, ComboService combos)
    {
        _db = db;
        _combos = combos;
    }

    public async Task<IActionResult> Index()
    {
        await _combos.SincronizarAsync();
        var categoriaCombos = await _combos.NombreCategoriaAsync();
        ViewBag.Productos = await _db.Productos.AsNoTracking().Include(p => p.CategoriaProducto)
            .Where(p => p.Activo && p.CategoriaProducto!.Nombre != categoriaCombos).OrderBy(p => p.Nombre).ToListAsync();
        var promos = await _db.Promociones.AsNoTracking()
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .OrderByDescending(p => p.VigenciaHasta).ToListAsync();
        return View(promos);
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(int? id, string nombre, string? descripcion, decimal precioCombo,
                                             DateTime vigenciaDesde, DateTime vigenciaHasta, bool activa = false,
                                             int[]? productoId = null, int[]? cantidad = null, string? tipo = null)
    {
        var editando = id.HasValue && id.Value > 0;
        if (!await PuedeAsync(Modulos.Promociones, editando ? Acciones.Editar : Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            var esCombo = tipo == "combo";
            if (esCombo)
            {
                // Los combos no llevan fecha límite: solo se activan o se desactivan
                vigenciaDesde = DateTime.Today;
                vigenciaHasta = ComboService.SinFechaLimite;
            }
            else if (vigenciaDesde.Year < 2000 || vigenciaHasta.Year < 2000)
            {
                throw new InvalidOperationException("Indique las fechas de vigencia de la promoción.");
            }
            var lineas = new List<object>();
            productoId ??= Array.Empty<int>();
            cantidad ??= Array.Empty<int>();
            for (var i = 0; i < productoId.Length && i < cantidad.Length; i++)
            {
                if (cantidad[i] > 0)
                {
                    lineas.Add(new { productoId = productoId[i], cantidad = cantidad[i] });
                }
            }

            var cantidades = new Dictionary<int, int>();
            for (var i = 0; i < productoId.Length && i < cantidad.Length; i++)
            {
                if (cantidad[i] > 0)
                {
                    cantidades[productoId[i]] = cantidades.GetValueOrDefault(productoId[i]) + cantidad[i];
                }
            }
            var ids = cantidades.Keys.ToList();
            var precios = await _db.Productos.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Precio);
            var sumaSueltos = cantidades.Sum(c => c.Value * precios.GetValueOrDefault(c.Key));
            if (precioCombo >= sumaSueltos)
            {
                throw new InvalidOperationException(
                    $"El precio del combo ({precioCombo:N2}) debe ser menor que la suma de sus platillos por separado ({sumaSueltos:N2}); si no, no se aplica ningún descuento.");
            }

            // Si es un registro nuevo (creación), por defecto va activo
            var estadoActivo = editando ? activa : true;

            await _db.EjecutarAsync("dbo.sp_Promocion_Guardar",
                ("Id", editando ? id : null), ("Nombre", nombre), ("Descripcion", descripcion), ("PrecioCombo", precioCombo),
                ("VigenciaDesde", vigenciaDesde), ("VigenciaHasta", vigenciaHasta), ("Activa", estadoActivo),
                ("DetalleJson", JsonSerializer.Serialize(lineas)), ("UsuarioId", UsuarioId));

            await _combos.SincronizarAsync();
            Exito(esCombo ? "Combo guardado y publicado en el menú." : "Promoción guardada.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Promociones, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var eliminada = await _db.EjecutarAsync("dbo.sp_Promocion_Eliminar", ("Id", id), ("UsuarioId", UsuarioId));
            await _combos.SincronizarAsync();
            Exito(eliminada == 1 ? "Promoción eliminada." : "La promoción ya se aplicó en órdenes: se desactivó en lugar de eliminarse.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }
}