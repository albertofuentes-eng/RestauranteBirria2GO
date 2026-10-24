using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;

namespace RestauranteControl.Services;

/// <summary>
/// Mantiene cada combo/promoción como un platillo más del menú (categoría configurable en el parámetro
/// COMBOS.CATEGORIA) con el precio del combo. Así el combo se ve en el menú, se ordena como un solo
/// platillo y se cobra al precio del combo. Solo usa las tablas existentes (Producto y CategoriaProducto).
/// </summary>
public class ComboService
{
    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;

    public ComboService(AppDbContext db, ConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    /// <summary>Un combo no tiene fecha límite: se guarda con la vigencia final en el último día posible.</summary>
    public static readonly DateTime SinFechaLimite = new DateTime(9999, 12, 31, 23, 59, 0);

    public static bool EsCombo(Promocion promo) => promo.VigenciaHasta.Year >= 9999;

    public async Task<string> NombreCategoriaAsync() => await _cfg.TextoAsync(Param.CombosCategoria);

    public async Task SincronizarAsync()
    {
        try
        {
            var ahora = DateTime.Now;

            // 1. Vencer automáticamente todas las promociones que hayan sobrepasado su fecha de vigencia
            var vencidas = await _db.Promociones
                .Where(p => p.Activa && p.VigenciaHasta < ahora && p.VigenciaHasta.Year < 9999)
                .ToListAsync();

            if (vencidas.Any())
            {
                foreach (var promoVencida in vencidas)
                {
                    promoVencida.Activa = false;
                }
                await _db.SaveChangesAsync();
            }

            var nombreCategoria = await NombreCategoriaAsync();
            var promos = await _db.Promociones
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .ToListAsync();

            var combosPromos = promos.Where(EsCombo).ToList();

            var categoria = await _db.CategoriasProducto.FirstOrDefaultAsync(c => c.Nombre == nombreCategoria);
            if (categoria == null)
            {
                if (combosPromos.Count == 0)
                {
                    return;
                }
                categoria = new CategoriaProducto { Nombre = nombreCategoria, Activa = true };
                _db.CategoriasProducto.Add(categoria);
                await _db.SaveChangesAsync();
            }
            else if (!categoria.Activa)
            {
                categoria.Activa = true;
            }

            var existentes = await _db.Productos.Where(p => p.CategoriaProductoId == categoria.Id).ToListAsync();
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var promo in combosPromos)
            {
                nombres.Add(promo.Nombre);

                // Un combo está vigente si su propiedad Activa en DB está en true
                var vigente = promo.Activa;
                var descripcion = Resumen(promo);
                var producto = existentes.FirstOrDefault(p => string.Equals(p.Nombre, promo.Nombre, StringComparison.OrdinalIgnoreCase));

                if (producto == null)
                {
                    _db.Productos.Add(new Producto
                    {
                        Nombre = promo.Nombre,
                        Descripcion = descripcion,
                        CategoriaProductoId = categoria.Id,
                        Precio = promo.PrecioCombo,
                        Disponible = true,
                        Activo = vigente
                    });
                    continue;
                }

                if (producto.Nombre != promo.Nombre) producto.Nombre = promo.Nombre;
                if (producto.Precio != promo.PrecioCombo) producto.Precio = promo.PrecioCombo;
                if (producto.Descripcion != descripcion) producto.Descripcion = descripcion;

                // Forzamos que el estado Activo del producto del menú coincida exactamente con la promoción
                producto.Activo = vigente;
            }

            // Platillos de combos que ya no existen (promoción eliminada o renombrada)
            foreach (var huerfano in existentes.Where(p => p.Activo && !nombres.Contains(p.Nombre)))
            {
                huerfano.Activo = false;
            }

            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
        }
    }

    private static string Resumen(Promocion promo)
    {
        var texto = string.Join(", ", promo.Detalles.Select(d => d.Cantidad + " x " + d.Producto?.Nombre));
        return texto.Length > 500 ? texto.Substring(0, 500) : texto;
    }
}