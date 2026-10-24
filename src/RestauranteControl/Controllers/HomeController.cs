using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

public class HomeController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;

    public HomeController(AppDbContext db, ConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        ViewBag.OrdenesAbiertas = await _db.Ordenes.CountAsync(o => !o.Cerrada && !o.Anulada);
        ViewBag.VentasHoy = await _db.Ordenes.Where(o => o.Cerrada && o.FechaCierre >= hoy && o.FechaCierre < manana).SumAsync(o => (decimal?)o.Total) ?? 0m;
        var activa = await _cfg.TextoAsync(Param.PreordenEstadoActiva);
        ViewBag.ReservasHoy = await _db.Preordenes.CountAsync(p => p.FechaHora >= hoy && p.FechaHora < manana && p.EstadoPreorden!.Codigo == activa);

        var cerrado = await _cfg.TextoAsync(Param.OrdenEstadoCerrado);
        var entregado = await _cfg.TextoAsync(Param.OrdenEstadoEntregado);
        ViewBag.PlatillosEnCocina = await _db.OrdenDetalles.CountAsync(d =>
            !d.Orden!.Cerrada && !d.Orden.Anulada && d.EstadoOrden!.Codigo != cerrado && d.EstadoOrden.Codigo != entregado);

        ViewBag.Mesas = await _db.Mesas.Where(m => m.Activa)
            .GroupBy(m => new { m.EstadoMesa!.Nombre, m.EstadoMesa.Color })
            .Select(g => new MesaResumen { Estado = g.Key.Nombre, Color = g.Key.Color, Cantidad = g.Count() })
            .ToListAsync();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}

public class MesaResumen
{
    public string Estado { get; set; } = "";
    public string Color { get; set; } = "";
    public int Cantidad { get; set; }
}
