using Microsoft.AspNetCore.Mvc;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;
using RestauranteControl.ViewModels;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Reportes)]
public class ReportesController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;

    public ReportesController(AppDbContext db, ConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-30)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        if (h < d)
        {
            (d, h) = (h, d);
        }
        var top = await _cfg.EnteroAsync(Param.ReporteTopPlatillos);

        var vm = new ReporteVm
        {
            Desde = d,
            Hasta = h,
            Ventas = await _db.ConsultarAsync<VentaDiaDto>("dbo.sp_Reporte_VentasPeriodo", ("Desde", d), ("Hasta", h)),
            MasVendidos = await _db.ConsultarAsync<MasVendidoDto>("dbo.sp_Reporte_MasVendidos", ("Desde", d), ("Hasta", h), ("Top", top)),
            Tiempos = await _db.ConsultarAsync<TiempoPreparacionDto>("dbo.sp_Reporte_TiempoPreparacion", ("Desde", d), ("Hasta", h))
        };
        ViewBag.VerBitacora = await PuedeAsync(Modulos.Auditoria, Acciones.Ver);
        return View(vm);
    }
}
