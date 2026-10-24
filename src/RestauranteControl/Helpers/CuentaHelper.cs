using RestauranteControl.Models;

namespace RestauranteControl.Helpers;

/// <summary>Una línea de la cuenta tal como se presenta al cliente: un combo a su precio, o un platillo suelto.</summary>
public class LineaCuenta
{
    public int Cantidad { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Importe { get; set; }
    public bool EsCombo { get; set; }
    public List<string> Incluye { get; set; } = new();
}

public static class CuentaHelper
{
    /// <summary>
    /// Agrupa los platillos de la orden: los que forman parte de las promociones aplicadas se muestran
    /// como una sola línea de combo (con el precio del combo) y el resto como platillos sueltos.
    /// La suma de las líneas es igual a Subtotal - Descuento de la orden.
    /// Requiere cargar Detalles.Producto y Promociones.Promocion.Detalles.Producto.
    /// </summary>
    public static List<LineaCuenta> Armar(Orden orden)
    {
        var lineas = new List<LineaCuenta>();

        var porProducto = orden.Detalles
            .GroupBy(d => d.ProductoId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Cantidad = g.Sum(x => x.Cantidad),
                    Precio = g.Sum(x => x.Cantidad * x.PrecioUnitario) / g.Sum(x => x.Cantidad),
                    Nombre = g.First().Producto?.Nombre ?? ""
                });
        var restante = porProducto.ToDictionary(k => k.Key, v => v.Value.Cantidad);

        foreach (var aplicada in orden.Promociones.OrderBy(p => p.PromocionId))
        {
            var promo = aplicada.Promocion;
            if (promo == null)
            {
                continue;
            }
            var valorSueltos = 0m;
            var incluye = new List<string>();
            foreach (var componente in promo.Detalles)
            {
                var usadas = aplicada.Veces * componente.Cantidad;
                if (porProducto.TryGetValue(componente.ProductoId, out var info))
                {
                    valorSueltos += usadas * info.Precio;
                    restante[componente.ProductoId] = Math.Max(0, restante[componente.ProductoId] - usadas);
                }
                incluye.Add($"{usadas} x {componente.Producto?.Nombre}");
            }
            lineas.Add(new LineaCuenta
            {
                Cantidad = aplicada.Veces,
                Descripcion = promo.Nombre,
                Importe = Math.Round(valorSueltos - aplicada.Descuento, 2),
                EsCombo = true,
                Incluye = incluye
            });
        }

        foreach (var par in porProducto)
        {
            var cantidad = restante[par.Key];
            if (cantidad > 0)
            {
                lineas.Add(new LineaCuenta
                {
                    Cantidad = cantidad,
                    Descripcion = par.Value.Nombre,
                    Importe = Math.Round(cantidad * par.Value.Precio, 2)
                });
            }
        }
        return lineas;
    }
}
