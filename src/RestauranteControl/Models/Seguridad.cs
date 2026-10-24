namespace RestauranteControl.Models;

public class Moneda
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Simbolo { get; set; } = "";
    public bool Activa { get; set; } = true;
}

public class Empresa
{
    public int Id { get; set; } = 1;
    public string Nombre { get; set; } = "";
    public string? Nit { get; set; }
    public string Direccion { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public int MonedaId { get; set; }
    public Moneda? Moneda { get; set; }
    public string ImpuestoNombre { get; set; } = "";
    public decimal ImpuestoPorcentaje { get; set; }
    public string? MensajeComprobante { get; set; }
}

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsAdministrador { get; set; }
    public bool Activo { get; set; } = true;
}

public class Modulo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Controlador { get; set; } = "";
    public string Icono { get; set; } = "bi-circle";
    public int Orden { get; set; }
    public bool EsAdministracion { get; set; }
    public bool Activo { get; set; } = true;
}

public class RolPermiso
{
    public int RolId { get; set; }
    public int ModuloId { get; set; }
    public Modulo? Modulo { get; set; }
    public bool Ver { get; set; }
    public bool Crear { get; set; }
    public bool Editar { get; set; }
    public bool Eliminar { get; set; }
}

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int RolId { get; set; }
    public Rol? Rol { get; set; }
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}

public class Parametro
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public string TipoDato { get; set; } = "TEXT";
    public string Grupo { get; set; } = "General";
    public string? Descripcion { get; set; }
}

public class Auditoria
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Usuario { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Tabla { get; set; } = "";
    public string? RegistroId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }
    public string? Detalle { get; set; }
}
