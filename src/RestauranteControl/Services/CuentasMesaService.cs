using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;

namespace RestauranteControl.Services;

/// <summary>
/// Cuentas separadas por mesa: varias órdenes abiertas en la misma mesa, cada una a nombre de un cliente.
/// Trabaja solo con las tablas y procedimientos que ya existen (no requiere cambios en la base de datos).
/// </summary>
public class CuentasMesaService
{
    private const string ClaveEstadoLimpieza = "MESA.ESTADO_LIMPIEZA";

    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;

    public CuentasMesaService(AppDbContext db, ConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    /// <summary>¿La mesa ya tiene una cuenta abierta?</summary>
    public async Task<bool> TieneCuentaAbiertaAsync(int mesaId)
        => await _db.Ordenes.AnyAsync(o => o.MesaId == mesaId && !o.Cerrada && !o.Anulada);

    /// <summary>Abre una cuenta adicional en una mesa que ya tiene otra abierta.</summary>
    public async Task<long> CrearCuentaAdicionalAsync(int tipoOrdenId, int mesaId, int usuarioId, string? cliente, string? telefono)
    {
        var tipo = await _db.TiposOrden.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tipoOrdenId && t.Activo)
            ?? throw new InvalidOperationException("El tipo de orden no existe o está inactivo.");
        if (!tipo.RequiereMesa)
        {
            throw new InvalidOperationException("Este tipo de orden no usa mesa.");
        }

        var mesa = await _db.Mesas.AsNoTracking().Include(m => m.EstadoMesa).FirstOrDefaultAsync(m => m.Id == mesaId && m.Activa)
            ?? throw new InvalidOperationException("La mesa no existe o está inactiva.");
        var codigoLimpieza = await _cfg.TextoAsync(ClaveEstadoLimpieza);
        if (mesa.EstadoMesa?.Codigo == codigoLimpieza)
        {
            throw new InvalidOperationException("La mesa está en limpieza; márquela como libre antes de abrir una orden.");
        }

        var codigoInicial = await _cfg.TextoAsync(Param.OrdenEstadoInicial);
        var inicial = await _db.EstadosOrden.AsNoTracking().FirstOrDefaultAsync(e => e.Codigo == codigoInicial)
            ?? throw new InvalidOperationException("Falta configurar el estado inicial de las órdenes.");

        var orden = new Orden
        {
            TipoOrdenId = tipoOrdenId,
            MesaId = mesaId,
            EstadoOrdenId = inicial.Id,
            UsuarioId = usuarioId,
            Cliente = Limpiar(cliente),
            Telefono = Limpiar(telefono),
            Fecha = DateTime.Now
        };
        _db.Ordenes.Add(orden);
        await _db.SaveChangesAsync();
        return orden.Id;
    }

    /// <summary>
    /// Los procedimientos de cobro y anulación liberan la mesa al cerrar una cuenta. Si todavía quedan
    /// cuentas abiertas en esa mesa, se vuelve a dejar como ocupada.
    /// </summary>
    public async Task MantenerMesaOcupadaAsync(long ordenId)
    {
        var mesaId = await _db.Ordenes.AsNoTracking().Where(o => o.Id == ordenId).Select(o => o.MesaId).FirstOrDefaultAsync();
        if (mesaId == null || !await TieneCuentaAbiertaAsync(mesaId.Value))
        {
            return;
        }
        var codigo = await _cfg.TextoAsync(Param.MesaEstadoOcupada);
        var ocupada = await _db.EstadosMesa.AsNoTracking().FirstOrDefaultAsync(e => e.Codigo == codigo);
        var mesa = await _db.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId.Value);
        if (mesa != null && ocupada != null && mesa.EstadoMesaId != ocupada.Id)
        {
            mesa.EstadoMesaId = ocupada.Id;
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Pasa un platillo (o parte de su cantidad) a otra cuenta abierta de la misma mesa.</summary>
    public async Task MoverPlatilloAsync(long detalleId, long ordenDestinoId, int cantidad)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var detalle = await _db.OrdenDetalles.FirstOrDefaultAsync(d => d.Id == detalleId)
            ?? throw new InvalidOperationException("El platillo no existe.");
        var origenId = detalle.OrdenId;
        if (origenId == ordenDestinoId)
        {
            throw new InvalidOperationException("Elija una cuenta distinta a la actual.");
        }
        if (cantidad <= 0 || cantidad > detalle.Cantidad)
        {
            throw new InvalidOperationException("La cantidad a mover no es válida.");
        }

        var origen = await _db.Ordenes.AsNoTracking().FirstAsync(o => o.Id == origenId);
        var destino = await _db.Ordenes.AsNoTracking().FirstOrDefaultAsync(o => o.Id == ordenDestinoId)
            ?? throw new InvalidOperationException("La cuenta de destino no existe.");
        if (origen.Cerrada || origen.Anulada || destino.Cerrada || destino.Anulada)
        {
            throw new InvalidOperationException("Las dos cuentas deben estar abiertas.");
        }
        if (origen.MesaId == null || origen.MesaId != destino.MesaId)
        {
            throw new InvalidOperationException("Solo se pueden mover platillos entre cuentas de la misma mesa.");
        }

        if (cantidad == detalle.Cantidad)
        {
            detalle.OrdenId = ordenDestinoId;
            await _db.SaveChangesAsync();
        }
        else
        {
            detalle.Cantidad -= cantidad;
            var nuevo = new OrdenDetalle
            {
                OrdenId = ordenDestinoId,
                ProductoId = detalle.ProductoId,
                Cantidad = cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Nota = detalle.Nota,
                EstadoOrdenId = detalle.EstadoOrdenId,
                Fecha = detalle.Fecha
            };
            _db.OrdenDetalles.Add(nuevo);
            await _db.SaveChangesAsync();

            // Conserva el historial de estados (tiempos de cocina) en el platillo nuevo
            var historial = await _db.OrdenDetalleEstadoHist.AsNoTracking().Where(h => h.OrdenDetalleId == detalle.Id).ToListAsync();
            foreach (var h in historial)
            {
                _db.OrdenDetalleEstadoHist.Add(new OrdenDetalleEstadoHist
                {
                    OrdenDetalleId = nuevo.Id,
                    EstadoOrdenId = h.EstadoOrdenId,
                    Fecha = h.Fecha,
                    UsuarioId = h.UsuarioId
                });
            }
            await _db.SaveChangesAsync();
        }

        await RecalcularAsync(origenId);
        await RecalcularAsync(ordenDestinoId);
        await tx.CommitAsync();
    }

    // Procedimiento existente que recalcula subtotal, promociones, impuesto y total de la cuenta
    private async Task RecalcularAsync(long ordenId)
    {
        await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_Orden_Recalcular @OrdenId = @OrdenId", new SqlParameter("@OrdenId", ordenId));
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
