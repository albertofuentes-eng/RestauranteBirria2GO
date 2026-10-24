using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RestauranteControl.Models;

namespace RestauranteControl.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor _http;

    // Propiedades que nunca se escriben en la bitácora
    private static readonly HashSet<string> Sensibles = new() { nameof(Usuario.PasswordHash) };

    // Cambios de contadores internos que no aportan a la auditoría
    private static readonly HashSet<string> Ignoradas = new()
    {
        nameof(Usuario.IntentosFallidos), nameof(Usuario.BloqueadoHasta), nameof(Usuario.UltimoAcceso)
    };

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor http) : base(options)
    {
        _http = http;
    }

    public DbSet<Moneda> Monedas => Set<Moneda>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<EstadoMesa> EstadosMesa => Set<EstadoMesa>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<CategoriaProducto> CategoriasProducto => Set<CategoriaProducto>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<TipoOrden> TiposOrden => Set<TipoOrden>();
    public DbSet<EstadoOrden> EstadosOrden => Set<EstadoOrden>();
    public DbSet<MetodoPago> MetodosPago => Set<MetodoPago>();
    public DbSet<EstadoPreorden> EstadosPreorden => Set<EstadoPreorden>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<OrdenDetalle> OrdenDetalles => Set<OrdenDetalle>();
    public DbSet<OrdenDetalleEstadoHist> OrdenDetalleEstadoHist => Set<OrdenDetalleEstadoHist>();
    public DbSet<OrdenPromocion> OrdenPromociones => Set<OrdenPromocion>();
    public DbSet<Preorden> Preordenes => Set<Preorden>();
    public DbSet<PreordenDetalle> PreordenDetalles => Set<PreordenDetalle>();
    public DbSet<Promocion> Promociones => Set<Promocion>();
    public DbSet<PromocionDetalle> PromocionDetalles => Set<PromocionDetalle>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Parametro> Parametros => Set<Parametro>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // Montos con dos decimales (la tasa de impuesto se redefine más abajo)
        foreach (var entity in m.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetColumnType("decimal(18,2)");
            }
        }

        m.Entity<Moneda>().ToTable("Moneda");
        m.Entity<Empresa>(e =>
        {
            e.ToTable("Empresa");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.ImpuestoPorcentaje).HasColumnType("decimal(5,2)");
        });
        m.Entity<Rol>().ToTable("Rol");
        m.Entity<Modulo>().ToTable("Modulo");
        m.Entity<RolPermiso>(e =>
        {
            e.ToTable("RolPermiso");
            e.HasKey(x => new { x.RolId, x.ModuloId });
            e.HasOne(x => x.Modulo).WithMany().HasForeignKey(x => x.ModuloId);
        });
        m.Entity<Usuario>().ToTable("Usuario");
        m.Entity<Zona>().ToTable("Zona");
        m.Entity<EstadoMesa>().ToTable("EstadoMesa");
        m.Entity<Mesa>().ToTable("Mesa");
        m.Entity<CategoriaProducto>().ToTable("CategoriaProducto");
        m.Entity<Producto>().ToTable("Producto");
        m.Entity<TipoOrden>().ToTable("TipoOrden");
        m.Entity<EstadoOrden>().ToTable("EstadoOrden");
        m.Entity<MetodoPago>().ToTable("MetodoPago");
        m.Entity<EstadoPreorden>().ToTable("EstadoPreorden");
        m.Entity<Orden>(e =>
        {
            e.ToTable("Orden");
            e.HasMany(x => x.Detalles).WithOne(x => x.Orden).HasForeignKey(x => x.OrdenId);
            e.HasMany(x => x.Promociones).WithOne().HasForeignKey(x => x.OrdenId);
            e.HasOne(x => x.Pago).WithOne().HasForeignKey<Pago>(x => x.OrdenId);
        });
        m.Entity<OrdenDetalle>().ToTable("OrdenDetalle");
        m.Entity<OrdenDetalleEstadoHist>().ToTable("OrdenDetalleEstadoHist");
        m.Entity<OrdenPromocion>(e =>
        {
            e.ToTable("OrdenPromocion");
            e.HasKey(x => new { x.OrdenId, x.PromocionId });
        });
        m.Entity<Preorden>(e =>
        {
            e.ToTable("Preorden");
            e.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.PreordenId);
        });
        m.Entity<PreordenDetalle>().ToTable("PreordenDetalle");
        m.Entity<Promocion>(e =>
        {
            e.ToTable("Promocion");
            e.HasMany(x => x.Detalles).WithOne().HasForeignKey(x => x.PromocionId);
        });
        m.Entity<PromocionDetalle>(e =>
        {
            e.ToTable("PromocionDetalle");
            e.HasKey(x => new { x.PromocionId, x.ProductoId });
        });
        m.Entity<Pago>().ToTable("Pago");
        m.Entity<Auditoria>().ToTable("Auditoria");
        m.Entity<Parametro>().ToTable("Parametro");

    }

    // ------------------------------------------------------------------
    // Bitácora automática: cada INSERT / UPDATE / DELETE hecho con EF queda
    // registrado en dbo.Auditoria (usuario, acción, tabla, fecha y valores).
    // Los procedimientos almacenados registran su propia bitácora.
    // ------------------------------------------------------------------
    private sealed class CambioPendiente
    {
        public EntityEntry Entrada { get; init; } = null!;
        public string Tabla { get; init; } = "";
        public string Accion { get; init; } = "";
        public string? RegistroId { get; set; }
        public string? Anteriores { get; init; }
        public string? Nuevos { get; init; }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var pendientes = CapturarCambios();
        if (pendientes.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var transaccionPropia = Database.CurrentTransaction == null;
        var tx = transaccionPropia ? await Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            var filas = await base.SaveChangesAsync(true, cancellationToken);

            var (usuarioId, usuario) = UsuarioActual();
            foreach (var p in pendientes)
            {
                if (p.RegistroId == null)
                {
                    p.RegistroId = ClaveComoTexto(p.Entrada);
                }
                Auditorias.Add(new Auditoria
                {
                    UsuarioId = usuarioId,
                    Usuario = usuario,
                    Accion = p.Accion,
                    Tabla = p.Tabla,
                    RegistroId = p.RegistroId,
                    Fecha = DateTime.Now,
                    ValoresAnteriores = p.Anteriores,
                    ValoresNuevos = p.Nuevos
                });
            }
            await base.SaveChangesAsync(true, cancellationToken);

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }
            return filas;
        }
        finally
        {
            if (tx != null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    private (int? Id, string Nombre) UsuarioActual()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var nombre = user.FindFirstValue(ClaimTypes.Name);
            if (int.TryParse(id, out var uid) && !string.IsNullOrEmpty(nombre))
            {
                return (uid, nombre);
            }
        }
        return (null, "Sistema");
    }

    private List<CambioPendiente> CapturarCambios()
    {
        var lista = new List<CambioPendiente>();
        foreach (var entrada in ChangeTracker.Entries().ToList())
        {
            if (entrada.Entity is Auditoria || entrada.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var tabla = entrada.Metadata.GetTableName() ?? entrada.Metadata.ClrType.Name;
            switch (entrada.State)
            {
                case EntityState.Added:
                    lista.Add(new CambioPendiente { Entrada = entrada, Tabla = tabla, Accion = "INSERT", Nuevos = Describir(entrada, false) });
                    break;

                case EntityState.Deleted:
                    lista.Add(new CambioPendiente
                    {
                        Entrada = entrada, Tabla = tabla, Accion = "DELETE",
                        RegistroId = ClaveComoTexto(entrada), Anteriores = Describir(entrada, true)
                    });
                    break;

                case EntityState.Modified:
                    var cambiadas = entrada.Properties
                        .Where(p => p.IsModified && !Ignoradas.Contains(p.Metadata.Name) && !Equals(p.OriginalValue, p.CurrentValue))
                        .ToList();
                    if (cambiadas.Count == 0)
                    {
                        break;
                    }
                    lista.Add(new CambioPendiente
                    {
                        Entrada = entrada, Tabla = tabla, Accion = "UPDATE", RegistroId = ClaveComoTexto(entrada),
                        Anteriores = Formatear(cambiadas.Select(p => (p.Metadata.Name, p.OriginalValue))),
                        Nuevos = Formatear(cambiadas.Select(p => (p.Metadata.Name, p.CurrentValue)))
                    });
                    break;
            }
        }
        return lista;
    }

    private static string Describir(EntityEntry entrada, bool original)
    {
        return Formatear(entrada.Properties
            .Where(p => original || !p.IsTemporary)
            .Select(p => (p.Metadata.Name, original ? p.OriginalValue : p.CurrentValue)));
    }

    private static string Formatear(IEnumerable<(string Nombre, object? Valor)> valores)
    {
        var sb = new StringBuilder();
        foreach (var (nombre, valor) in valores)
        {
            if (sb.Length > 0)
            {
                sb.Append("; ");
            }
            sb.Append(nombre).Append('=');
            if (Sensibles.Contains(nombre))
            {
                sb.Append("***");
            }
            else if (valor is DateTime fecha)
            {
                sb.Append(fecha.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                sb.Append(valor ?? "NULL");
            }
        }
        return sb.ToString();
    }

    private static string ClaveComoTexto(EntityEntry entrada)
    {
        var clave = entrada.Metadata.FindPrimaryKey();
        if (clave == null)
        {
            return "";
        }
        return string.Join("-", clave.Properties.Select(p => entrada.Property(p.Name).CurrentValue?.ToString()));
    }
}
