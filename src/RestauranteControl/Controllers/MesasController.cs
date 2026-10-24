using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;
using RestauranteControl.ViewModels;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Mesas)]
public class MesasController : BaseController
{
    private readonly AppDbContext _db;

    public MesasController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<List<MesaMapaDto>> CargarMapaAsync()
    {
        await _db.EjecutarAsync("dbo.sp_Mesa_SincronizarReservas");
        ViewBag.Estados = await _db.EstadosMesa.AsNoTracking().Where(e => e.Activo).OrderBy(e => e.Id).ToListAsync();
        var mapa = await _db.ConsultarAsync<MesaMapaDto>("dbo.sp_Mesas_Mapa");

        // Una mesa puede tener varias cuentas abiertas (cuentas separadas)
        var abiertas = await _db.Ordenes.AsNoTracking()
            .Where(o => o.MesaId != null && !o.Cerrada && !o.Anulada)
            .GroupBy(o => o.MesaId!.Value)
            .Select(g => new { Mesa = g.Key, Cuentas = g.Count(), Total = g.Sum(x => x.Total) })
            .ToListAsync();
        foreach (var m in mapa)
        {
            var a = abiertas.FirstOrDefault(x => x.Mesa == m.MesaId);
            m.OrdenesAbiertas = a?.Cuentas ?? 0;
            m.TotalAbierto = a?.Total ?? 0m;
        }
        return mapa;
    }

    public async Task<IActionResult> Index()
    {
        var mapa = await CargarMapaAsync();
        return View(mapa);
    }

    // Fragmento que el navegador consulta periódicamente (estado en tiempo real)
    public async Task<IActionResult> Mapa()
    {
        var mapa = await CargarMapaAsync();
        return PartialView("_Mapa", mapa);
    }

    [Permiso(Modulos.Mesas, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, int estadoId)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Mesa_CambiarEstado", ("MesaId", id), ("EstadoId", estadoId), ("UsuarioId", UsuarioId));
            Exito("Estado de la mesa actualizado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Administrar()
    {
        ViewBag.Zonas = await _db.Zonas.AsNoTracking().Where(z => z.Activa).OrderBy(z => z.Nombre).ToListAsync();
        var mesas = await _db.Mesas.AsNoTracking().Include(m => m.Zona).Include(m => m.EstadoMesa)
            .OrderBy(m => m.Zona!.Nombre).ThenBy(m => m.Numero).ToListAsync();
        return View(mesas);
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(int? id, string numero, int zonaId, int capacidad, bool activa = false)
    {
        var accion = id.HasValue && id.Value > 0 ? Acciones.Editar : Acciones.Crear;
        if (!await PuedeAsync(Modulos.Mesas, accion))
        {
            return SinPermiso();
        }
        try
        {
            await _db.EjecutarAsync("dbo.sp_Mesa_Guardar",
                ("Id", id.HasValue && id.Value > 0 ? id : null), ("Numero", numero), ("ZonaId", zonaId),
                ("Capacidad", capacidad), ("Activa", id.HasValue && id.Value > 0 ? activa : true), ("UsuarioId", UsuarioId));
            Exito("Mesa guardada correctamente.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Administrar));
    }

    [Permiso(Modulos.Mesas, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var eliminada = await _db.EjecutarAsync("dbo.sp_Mesa_Eliminar", ("Id", id), ("UsuarioId", UsuarioId));
            Exito(eliminada == 1 ? "Mesa eliminada." : "La mesa tiene historial: se desactivó en lugar de eliminarse.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Administrar));
    }
}
