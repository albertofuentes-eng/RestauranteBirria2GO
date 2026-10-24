using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Caja)]
public class CajaController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;
    private readonly CuentasMesaService _cuentas;

    public CajaController(AppDbContext db, ConfigService cfg, CuentasMesaService cuentas)
    {
        _db = db;
        _cfg = cfg;
        _cuentas = cuentas;
    }

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        ViewBag.Metodos = await _db.MetodosPago.AsNoTracking().Where(m => m.Activo).OrderBy(m => m.Id).ToListAsync();
        ViewBag.Cobradas = await _db.Ordenes.AsNoTracking()
            .Include(o => o.Mesa).Include(o => o.Pago).ThenInclude(p => p!.MetodoPago)
            .Where(o => o.Cerrada && o.FechaCierre >= hoy)
            .OrderByDescending(o => o.FechaCierre).Take(30).ToListAsync();

        var abiertas = await _db.Ordenes.AsNoTracking()
            .Include(o => o.Mesa).Include(o => o.TipoOrden).Include(o => o.EstadoOrden)
            .Include(o => o.Detalles).Include(o => o.Promociones)
            .Where(o => !o.Cerrada && !o.Anulada)
            .OrderBy(o => o.Fecha).ToListAsync();
        return View(abiertas);
    }

    [Permiso(Modulos.Caja, Acciones.Crear)]
    [HttpPost]
    public async Task<IActionResult> Cobrar(long ordenId, int metodoPagoId)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Orden_Cerrar", ("OrdenId", ordenId), ("MetodoPagoId", metodoPagoId), ("UsuarioId", UsuarioId));
            // Si la mesa todavía tiene otras cuentas abiertas, sigue ocupada
            await _cuentas.MantenerMesaOcupadaAsync(ordenId);
            Exito("Cuenta cobrada correctamente.");
            return RedirectToAction(nameof(Comprobante), new { id = ordenId });
        }
        catch (Exception ex)
        {
            Falla(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Comprobante(long id)
    {
        var orden = await _db.Ordenes.AsNoTracking()
            .Include(o => o.Mesa).Include(o => o.TipoOrden).Include(o => o.Usuario)
            .Include(o => o.Detalles).ThenInclude(d => d.Producto)
            .Include(o => o.Promociones).ThenInclude(p => p.Promocion).ThenInclude(pr => pr!.Detalles).ThenInclude(d => d.Producto)
            .Include(o => o.Pago).ThenInclude(p => p!.MetodoPago)
            .Include(o => o.Pago).ThenInclude(p => p!.Usuario)
            .FirstOrDefaultAsync(o => o.Id == id && o.Cerrada);
        if (orden?.Pago == null)
        {
            Falla("El comprobante no existe: la orden aún no ha sido cobrada.");
            return RedirectToAction(nameof(Index));
        }
        var prefijo = await _cfg.TextoAsync(Param.ComprobantePrefijo);
        var digitos = await _cfg.EnteroAsync(Param.ComprobanteDigitos);
        ViewBag.Numero = prefijo + "-" + orden.Pago.Id.ToString().PadLeft(digitos, '0');
        return View(orden);
    }
}
