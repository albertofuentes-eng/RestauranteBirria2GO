using System.ComponentModel.DataAnnotations.Schema;

namespace RestauranteControl.ViewModels;

// Resultado de dbo.sp_Mesas_Mapa
public class MesaMapaDto
{
    public int MesaId { get; set; }
    public string Numero { get; set; } = "";
    public string Zona { get; set; } = "";
    public int Capacidad { get; set; }
    public int EstadoMesaId { get; set; }
    public string Estado { get; set; } = "";
    public string EstadoCodigo { get; set; } = "";
    public string Color { get; set; } = "";
    public long? OrdenId { get; set; }
    public decimal? OrdenTotal { get; set; }
    public int? MinutosOcupada { get; set; }
    public DateTime? ProximaReserva { get; set; }
    public string? ClienteReserva { get; set; }

    // Se calculan en la aplicación (no vienen del procedimiento): cuentas abiertas de la mesa
    [NotMapped] public int OrdenesAbiertas { get; set; }
    [NotMapped] public decimal TotalAbierto { get; set; }
}

// Resultado de dbo.sp_Reporte_VentasPeriodo
public class VentaDiaDto
{
    public DateTime Fecha { get; set; }
    public int Ordenes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
}

// Resultado de dbo.sp_Reporte_MasVendidos
public class MasVendidoDto
{
    public string Producto { get; set; } = "";
    public string Categoria { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Importe { get; set; }
}

// Resultado de dbo.sp_Reporte_TiempoPreparacion
public class TiempoPreparacionDto
{
    public string Producto { get; set; } = "";
    public int Platillos { get; set; }
    public decimal MinutosPromedio { get; set; }
    public decimal MinutosMaximo { get; set; }
}

public class ReporteVm
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public List<VentaDiaDto> Ventas { get; set; } = new();
    public List<MasVendidoDto> MasVendidos { get; set; } = new();
    public List<TiempoPreparacionDto> Tiempos { get; set; } = new();
}

public class BitacoraVm
{
    public List<Models.Auditoria> Filas { get; set; } = new();
    public List<string> Usuarios { get; set; } = new();
    public List<string> Tablas { get; set; } = new();
    public string? Usuario { get; set; }
    public string? Accion { get; set; }
    public string? Tabla { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int Total { get; set; }
}

public class LoginVm
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Error { get; set; }
}
