using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Cocina)]
public class CocinaController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;

    public CocinaController(AppDbContext db, ConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    private async Task<List<OrdenDetalle>> CargarTableroAsync()
    {
        var cerrado = await _cfg.TextoAsync(Param.OrdenEstadoCerrado);

        // Muestra todas las columnas activas de la cocina (Pendiente, En preparación, Listo, etc.)
        ViewBag.Estados = await _db.EstadosOrden.AsNoTracking()
            .Where(e => e.Activo && e.Codigo != cerrado)
            .OrderBy(e => e.OrdenVisual)
            .ToListAsync();

        ViewBag.PuedeEditar = await PuedeAsync(Modulos.Cocina, Acciones.Editar);

        // Muestra sólo los ítems de órdenes abiertas que NO tengan la marca [BORRADOR]
        return await _db.OrdenDetalles.AsNoTracking()
            .Include(d => d.Orden).ThenInclude(o => o!.Mesa)
            .Include(d => d.Orden).ThenInclude(o => o!.TipoOrden)
            .Include(d => d.Producto)
            .Include(d => d.EstadoOrden)
            .Where(d => !d.Orden!.Cerrada
                     && !d.Orden.Anulada
                     && (d.Nota == null || !d.Nota.Contains("[BORRADOR]")))
            .OrderBy(d => d.Fecha).ThenBy(d => d.Id)
            .ToListAsync();
    }

    public async Task<IActionResult> Index() => View(await CargarTableroAsync());

    public async Task<IActionResult> Tablero() => PartialView("_Tablero", await CargarTableroAsync());

    [Permiso(Modulos.Cocina, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> Estado(long id, int estadoId)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Orden_CambiarEstadoItem", ("DetalleId", id), ("EstadoId", estadoId), ("UsuarioId", UsuarioId));
            Exito("Estado del platillo actualizado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Cocina, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> EstadoOrden(long ordenId, int estadoId)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Orden_CambiarEstado", ("OrdenId", ordenId), ("EstadoId", estadoId), ("UsuarioId", UsuarioId));
            Exito($"Orden #{ordenId} actualizada.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }
}