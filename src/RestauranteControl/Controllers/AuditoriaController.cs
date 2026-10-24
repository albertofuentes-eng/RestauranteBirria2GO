using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;
using RestauranteControl.ViewModels;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Auditoria)]
public class AuditoriaController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;

    public AuditoriaController(AppDbContext db, ConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    public async Task<IActionResult> Index(string? usuario, string? accion, string? tabla, DateTime? desde, DateTime? hasta, int pagina = 1)
    {
        var porPagina = await _cfg.EnteroAsync(Param.UiFilasBitacora);
        var q = _db.Auditorias.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(usuario)) q = q.Where(x => x.Usuario == usuario);
        if (!string.IsNullOrWhiteSpace(accion)) q = q.Where(x => x.Accion == accion);
        if (!string.IsNullOrWhiteSpace(tabla)) q = q.Where(x => x.Tabla == tabla);
        if (desde.HasValue) q = q.Where(x => x.Fecha >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(x => x.Fecha < hasta.Value.Date.AddDays(1));

        var total = await q.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)porPagina));
        pagina = Math.Min(Math.Max(1, pagina), paginas);

        var vm = new BitacoraVm
        {
            Usuario = usuario, Accion = accion, Tabla = tabla, Desde = desde, Hasta = hasta,
            Pagina = pagina, TotalPaginas = paginas, Total = total,
            Usuarios = await _db.Auditorias.AsNoTracking().Select(x => x.Usuario).Distinct().OrderBy(x => x).ToListAsync(),
            Tablas = await _db.Auditorias.AsNoTracking().Select(x => x.Tabla).Distinct().OrderBy(x => x).ToListAsync(),
            Filas = await q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
                           .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync()
        };
        return View(vm);
    }
}
