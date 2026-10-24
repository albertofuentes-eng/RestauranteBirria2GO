namespace RestauranteControl.Models;

public class Zona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activa { get; set; } = true;
}

public class EstadoMesa
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Color { get; set; } = "#6c757d";
    public bool Activo { get; set; } = true;
}

public class Mesa
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public int ZonaId { get; set; }
    public Zona? Zona { get; set; }
    public int Capacidad { get; set; }
    public int EstadoMesaId { get; set; }
    public EstadoMesa? EstadoMesa { get; set; }
    public bool Activa { get; set; } = true;
}

public class CategoriaProducto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activa { get; set; } = true;
}

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int CategoriaProductoId { get; set; }
    public CategoriaProducto? CategoriaProducto { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; } = true;
    public bool Activo { get; set; } = true;

    public string? ImagenUrl { get; set; }
}

public class TipoOrden
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool RequiereMesa { get; set; }
    public bool RequiereDireccion { get; set; }
    public bool Activo { get; set; } = true;
}

public class EstadoOrden
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int OrdenVisual { get; set; }
    public string Color { get; set; } = "#6c757d";
    public bool Activo { get; set; } = true;
}

public class MetodoPago
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;
}

public class EstadoPreorden
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public class Orden
{
    public long Id { get; set; }
    public int TipoOrdenId { get; set; }
    public TipoOrden? TipoOrden { get; set; }
    public int? MesaId { get; set; }
    public Mesa? Mesa { get; set; }
    public int EstadoOrdenId { get; set; }
    public EstadoOrden? EstadoOrden { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string? Cliente { get; set; }
    public string? Telefono { get; set; }
    public string? DireccionEntrega { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public DateTime? FechaCierre { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public bool Cerrada { get; set; }
    public bool Anulada { get; set; }
    public string? MotivoAnulacion { get; set; }
    public List<OrdenDetalle> Detalles { get; set; } = new();
    public List<OrdenPromocion> Promociones { get; set; } = new();
    public Pago? Pago { get; set; }
}

public class OrdenDetalle
{
    public long Id { get; set; }
    public long OrdenId { get; set; }
    public Orden? Orden { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Nota { get; set; }
    public int EstadoOrdenId { get; set; }
    public EstadoOrden? EstadoOrden { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}

public class OrdenDetalleEstadoHist
{
    public long Id { get; set; }
    public long OrdenDetalleId { get; set; }
    public int EstadoOrdenId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int? UsuarioId { get; set; }
}

public class OrdenPromocion
{
    public long OrdenId { get; set; }
    public int PromocionId { get; set; }
    public Promocion? Promocion { get; set; }
    public int Veces { get; set; }
    public decimal Descuento { get; set; }
}

public class Preorden
{
    public long Id { get; set; }
    public int MesaId { get; set; }
    public Mesa? Mesa { get; set; }
    public string Cliente { get; set; } = "";
    public string? Telefono { get; set; }
    public DateTime FechaHora { get; set; }
    public int EstadoPreordenId { get; set; }
    public EstadoPreorden? EstadoPreorden { get; set; }
    public long? OrdenId { get; set; }
    public string? Notas { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public List<PreordenDetalle> Detalles { get; set; } = new();
}

public class PreordenDetalle
{
    public long Id { get; set; }
    public long PreordenId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public string? Nota { get; set; }
}

public class Promocion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal PrecioCombo { get; set; }
    public DateTime VigenciaDesde { get; set; }
    public DateTime VigenciaHasta { get; set; }
    public bool Activa { get; set; } = true;
    public List<PromocionDetalle> Detalles { get; set; } = new();
}

public class PromocionDetalle
{
    public int PromocionId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
}

public class Pago
{
    public long Id { get; set; }
    public long OrdenId { get; set; }
    public int MetodoPagoId { get; set; }
    public MetodoPago? MetodoPago { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
