using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Ordenes)]
public class OrdenesController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;
    private readonly ComboService _combos;
    private readonly CuentasMesaService _cuentas;

    public OrdenesController(AppDbContext db, ConfigService cfg, ComboService combos, CuentasMesaService cuentas)
    {
        _db = db;
        _cfg = cfg;
        _combos = combos;
        _cuentas = cuentas;
    }

    public async Task<IActionResult> Index(bool todas = false, int? mesaId = null, int? soloMesa = null)
    {
        ViewBag.Tipos = await _db.TiposOrden.AsNoTracking().Where(t => t.Activo).OrderBy(t => t.Id).ToListAsync();
        ViewBag.Mesas = await _db.Mesas.AsNoTracking().Include(m => m.EstadoMesa).Where(m => m.Activa).OrderBy(m => m.Numero).ToListAsync();
        ViewBag.Todas = todas;
        ViewBag.MesaPreseleccionada = mesaId;
        ViewBag.SoloMesa = soloMesa;
        ViewBag.NumeroSoloMesa = soloMesa.HasValue
            ? await _db.Mesas.AsNoTracking().Where(m => m.Id == soloMesa.Value).Select(m => m.Numero).FirstOrDefaultAsync()
            : null;
        // Cuentas abiertas por mesa, para mostrarlas al abrir una cuenta nueva
        ViewBag.CuentasPorMesa = await _db.Ordenes.AsNoTracking()
            .Where(o => o.MesaId != null && !o.Cerrada && !o.Anulada)
            .GroupBy(o => o.MesaId!.Value)
            .Select(g => new { Mesa = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Mesa, x => x.N);

        var consulta = _db.Ordenes.AsNoTracking()
            .Include(o => o.TipoOrden).Include(o => o.Mesa).Include(o => o.EstadoOrden).Include(o => o.Usuario)
            .AsQueryable();
        if (!todas)
        {
            consulta = consulta.Where(o => !o.Cerrada && !o.Anulada);
        }
        if (soloMesa.HasValue)
        {
            consulta = consulta.Where(o => o.MesaId == soloMesa.Value);
        }
        return View(await consulta.OrderByDescending(o => o.Fecha).Take(200).ToListAsync());
    }

    [Permiso(Modulos.Ordenes, Acciones.Crear)]
    [HttpPost]
    public async Task<IActionResult> Crear(int tipoOrdenId, int? mesaId, string? cliente, string? telefono, string? direccion)
    {
        try
        {
            // Mesa que ya tiene una cuenta abierta: se abre una cuenta adicional (cuentas separadas)
            if (mesaId.HasValue
                && await _db.TiposOrden.AnyAsync(t => t.Id == tipoOrdenId && t.RequiereMesa)
                && await _cuentas.TieneCuentaAbiertaAsync(mesaId.Value))
            {
                var adicional = await _cuentas.CrearCuentaAdicionalAsync(tipoOrdenId, mesaId.Value, UsuarioId, cliente, telefono);
                Exito($"Cuenta #{adicional} abierta en la mesa. Agregue los platillos.");
                return RedirectToAction(nameof(Detalle), new { id = adicional });
            }
            var id = await _db.EjecutarAsync("dbo.sp_Orden_Crear",
                ("TipoOrdenId", tipoOrdenId), ("MesaId", mesaId), ("UsuarioId", UsuarioId),
                ("Cliente", cliente), ("Telefono", telefono), ("Direccion", direccion));
            Exito($"Orden #{id} creada. Agregue los platillos.");
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (Exception ex)
        {
            Falla(ex);
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Detalle(long id)
    {
        await _combos.SincronizarAsync();
        var orden = await _db.Ordenes.AsNoTracking()
            .Include(o => o.TipoOrden).Include(o => o.Mesa).Include(o => o.EstadoOrden).Include(o => o.Usuario)
            .Include(o => o.Detalles).ThenInclude(d => d.Producto)
            .Include(o => o.Detalles).ThenInclude(d => d.EstadoOrden)
            .Include(o => o.Promociones).ThenInclude(p => p.Promocion).ThenInclude(pr => pr!.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (orden == null)
        {
            Falla("La orden no existe.");
            return RedirectToAction(nameof(Index));
        }
        // Obtener el nombre de la categoría utilizada para los combos
        var categoriaCombos = await _combos.NombreCategoriaAsync();

        // Obtener los productos individuales del menú
        ViewBag.Productos = await _db.Productos.AsNoTracking()
            .Include(p => p.CategoriaProducto)
            .Where(p => p.Activo
                && p.Disponible
                && p.CategoriaProducto!.Activa
                && p.CategoriaProducto.Nombre != categoriaCombos)
            .OrderBy(p => p.CategoriaProducto!.Nombre)
            .ThenBy(p => p.Nombre)
            .ToListAsync();

        // Obtener los combos disponibles
        ViewBag.Combos = await _db.Productos.AsNoTracking()
            .Include(p => p.CategoriaProducto)
            .Where(p => p.Activo
                && p.Disponible
                && p.CategoriaProducto!.Activa
                && p.CategoriaProducto.Nombre == categoriaCombos)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        // Obtener las ofertas vigentes que no son combos
        var hoy = DateTime.Now;

        ViewBag.Ofertas = await _db.Promociones.AsNoTracking()
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(p => p.Activa
                && p.VigenciaHasta.Year < 9999
                && p.VigenciaDesde <= hoy
                && p.VigenciaHasta >= hoy)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        // Otras cuentas abiertas de la misma mesa (para moverles platillos o consultarlas)
        ViewBag.OtrasCuentas = (orden.MesaId.HasValue && !orden.Cerrada && !orden.Anulada)
            ? await _db.Ordenes.AsNoTracking()
                .Where(o => o.MesaId == orden.MesaId && o.Id != orden.Id && !o.Cerrada && !o.Anulada)
                .OrderBy(o => o.Id).ToListAsync()
            : new List<Orden>();

        ViewBag.EstadoListo = await _cfg.TextoAsync(Param.OrdenEstadoListo);
        ViewBag.EstadoInicial = await _cfg.TextoAsync(Param.OrdenEstadoInicial);

        return View(orden);
    }
    [Permiso(Modulos.Ordenes, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> AgregarItem(long ordenId, int? productoId, int cantidad = 1, string? nota = null, bool enviarACocina = true)
    {
        try
        {
            // 1. Si se seleccionó un producto del desplegable, lo insertamos
            if (productoId.HasValue && productoId.Value > 0)
            {
                var producto = await _db.Productos.AsNoTracking().Include(p => p.CategoriaProducto)
                    .FirstOrDefaultAsync(p => p.Id == productoId.Value);
                var categoriaCombos = await _combos.NombreCategoriaAsync();
                var esCombo = producto?.CategoriaProducto?.Nombre == categoriaCombos;

                if (esCombo && !string.IsNullOrWhiteSpace(producto!.Descripcion))
                {
                    nota = string.IsNullOrWhiteSpace(nota) ? "Incluye: " + producto.Descripcion : "Incluye: " + producto.Descripcion + " - " + nota.Trim();
                    if (nota.Length > 300)
                    {
                        nota = nota.Substring(0, 300);
                    }
                }

                // Insertamos el platillo vía Stored Procedure
                await _db.EjecutarAsync("dbo.sp_Orden_AgregarItem",
                    ("OrdenId", ordenId), ("ProductoId", productoId.Value), ("Cantidad", cantidad), ("Nota", nota), ("UsuarioId", UsuarioId));

                // Si presiona "Guardar en orden" (SIN enviar a cocina):
                if (!enviarACocina)
                {
                    var ultimoDetalle = await _db.OrdenDetalles
                        .Where(d => d.OrdenId == ordenId && d.ProductoId == productoId.Value)
                        .OrderByDescending(d => d.Id)
                        .FirstOrDefaultAsync();

                    if (ultimoDetalle != null)
                    {
                        // Marcamos el detalle como [BORRADOR] directamente vía DbContext
                        var notaLimpia = (ultimoDetalle.Nota ?? "").Replace("[BORRADOR]", "").Trim();
                        ultimoDetalle.Nota = string.IsNullOrWhiteSpace(notaLimpia) ? "[BORRADOR]" : "[BORRADOR] " + notaLimpia;
                        _db.OrdenDetalles.Update(ultimoDetalle);
                        await _db.SaveChangesAsync();
                    }

                    Exito(esCombo ? "Combo guardado en la orden." : "Platillo guardado en la orden.");
                    return RedirectToAction(nameof(Detalle), new { id = ordenId });
                }
            }
            else if (!enviarACocina)
            {
                Falla("Por favor seleccione un platillo para agregar.");
                return RedirectToAction(nameof(Detalle), new { id = ordenId });
            }

            // 2. Si presiona "Guardar y enviar a cocina":
            // Quitamos la marca [BORRADOR] de todos los platillos de esta orden para habilitarlos en cocina
            if (enviarACocina)
            {
                var detallesBorrador = await _db.OrdenDetalles
                    .Where(d => d.OrdenId == ordenId && d.Nota != null && d.Nota.Contains("[BORRADOR]"))
                    .ToListAsync();

                if (detallesBorrador.Any())
                {
                    foreach (var d in detallesBorrador)
                    {
                        d.Nota = d.Nota.Replace("[BORRADOR]", "").Trim();
                        _db.OrdenDetalles.Update(d);
                    }
                    await _db.SaveChangesAsync();
                }

                Exito("Orden enviada a cocina correctamente.");
            }
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    [Permiso(Modulos.Ordenes, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> EnviarACocina(long ordenId)
    {
        try
        {
            // Notifica o marca los ítems pendientes como listos para cocina
            await _db.EjecutarAsync("dbo.sp_Orden_EnviarCocina", ("OrdenId", ordenId), ("UsuarioId", UsuarioId));
            Exito("Orden enviada a cocina correctamente.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    // Pasa un platillo (o parte de su cantidad) a otra cuenta abierta de la misma mesa
    [Permiso(Modulos.Ordenes, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> MoverItem(long detalleId, long ordenId, long destinoId, int cantidad)
    {
        try
        {
            await _cuentas.MoverPlatilloAsync(detalleId, destinoId, cantidad);
            Exito($"Platillo movido a la cuenta #{destinoId}.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    [Permiso(Modulos.Ordenes, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> QuitarItem(long detalleId, long ordenId)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Orden_QuitarItem", ("DetalleId", detalleId), ("UsuarioId", UsuarioId));
            Exito("Platillo quitado de la orden.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    // El mesero marca como entregado un platillo que cocina dejó listo
    [Permiso(Modulos.Ordenes, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> Entregar(long detalleId, long ordenId)
    {
        try
        {
            var codigo = await _cfg.TextoAsync(Param.OrdenEstadoEntregado);
            var estado = await _db.EstadosOrden.AsNoTracking().FirstOrDefaultAsync(e => e.Codigo == codigo);
            if (estado == null)
            {
                throw new InvalidOperationException("No existe el estado configurado como 'entregado'.");
            }
            await _db.EjecutarAsync("dbo.sp_Orden_CambiarEstadoItem", ("DetalleId", detalleId), ("EstadoId", estado.Id), ("UsuarioId", UsuarioId));
            Exito("Platillo marcado como entregado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Detalle), new { id = ordenId });
    }

    [Permiso(Modulos.Ordenes, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> Anular(long id, string motivo)
    {
        try
        {
            await _db.EjecutarAsync("dbo.sp_Orden_Anular", ("OrdenId", id), ("Motivo", motivo), ("UsuarioId", UsuarioId));
            await _cuentas.MantenerMesaOcupadaAsync(id);
            Exito($"Orden #{id} anulada.");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Falla(ex);
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}
