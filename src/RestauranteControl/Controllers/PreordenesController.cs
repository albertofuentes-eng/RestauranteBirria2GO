using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Preordenes)]
public class PreordenesController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;
    private readonly ComboService _combos;

    public PreordenesController(AppDbContext db, ConfigService cfg, ComboService combos)
    {
        _db = db;
        _cfg = cfg;
        _combos = combos;
    }

    public async Task<IActionResult> Index(bool todas = false)
    {
        await _combos.SincronizarAsync();
        var activa = await _cfg.TextoAsync(Param.PreordenEstadoActiva);
        ViewBag.Todas = todas;
        ViewBag.EstadoActiva = activa;
        ViewBag.Mesas = await _db.Mesas.AsNoTracking().Where(m => m.Activa).OrderBy(m => m.Numero).ToListAsync();
        ViewBag.Productos = await _db.Productos.AsNoTracking().Include(p => p.CategoriaProducto)
            .Where(p => p.Activo && p.CategoriaProducto!.Activa)
            .OrderBy(p => p.CategoriaProducto!.Nombre).ThenBy(p => p.Nombre).ToListAsync();

        var consulta = _db.Preordenes.AsNoTracking()
            .Include(p => p.Mesa).Include(p => p.EstadoPreorden)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .AsQueryable();
        if (!todas)
        {
            consulta = consulta.Where(p => p.EstadoPreorden!.Codigo == activa);
        }
        return View(await consulta.OrderBy(p => p.FechaHora).Take(200).ToListAsync());
    }

    [Permiso(Modulos.Preordenes, Acciones.Crear)]
    [HttpPost]
    public async Task<IActionResult> Crear(int mesaId, string cliente, string? telefono, DateTime fechaHora, string? notas,
                                           int[] productoId, int[] cantidad)
    {
        try
        {
            var lineas = new List<object>();
            for (var i = 0; i < productoId.Length && i < cantidad.Length; i++)
            {
                if (cantidad[i] > 0)
                {
                    lineas.Add(new { productoId = productoId[i], cantidad = cantidad[i] });
                }
            }
            var json = JsonSerializer.Serialize(lineas);
            await _db.EjecutarAsync("dbo.sp_Preorden_Crear",
                ("MesaId", mesaId), ("Cliente", cliente), ("Telefono", telefono), ("FechaHora", fechaHora),
                ("Notas", notas), ("DetalleJson", json), ("UsuarioId", UsuarioId));
            Exito("Reserva registrada.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Preordenes, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> Atender(long id)
    {
        if (!await PuedeAsync(Modulos.Ordenes, Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            var ordenId = await _db.EjecutarAsync("dbo.sp_Preorden_Convertir", ("PreordenId", id), ("UsuarioId", UsuarioId));
            Exito($"Reserva atendida. Se abrió la orden #{ordenId} con los platillos reservados.");
            return RedirectToAction("Detalle", "Ordenes", new { id = ordenId });
        }
        catch (Exception ex)
        {
            Falla(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    [Permiso(Modulos.Preordenes, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> Cancelar(long id)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Preorden_Cancelar", ("PreordenId", id), ("UsuarioId", UsuarioId));
            Exito("Reserva cancelada.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }
}
