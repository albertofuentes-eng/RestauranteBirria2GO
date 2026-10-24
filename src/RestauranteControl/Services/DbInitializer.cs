using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;

namespace RestauranteControl.Services;

/// <summary>
/// Crea el administrador inicial la primera vez que arranca la aplicación (si no hay usuarios).
/// La contraseña se cifra con BCrypt; se exige cambiarla en el primer ingreso.
/// </summary>
public static class DbInitializer
{
    /// <summary>Crea parámetros nuevos que la aplicación necesita si todavía no existen (solo datos).</summary>
    public static async Task AsegurarParametrosAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Database.CanConnectAsync())
            {
                return;
            }
            if (!await db.Parametros.AnyAsync(p => p.Clave == Param.CombosCategoria))
            {
                db.Parametros.Add(new Parametro
                {
                    Clave = Param.CombosCategoria,
                    Valor = "Combos",
                    TipoDato = "TEXT",
                    Grupo = "Menú",
                    Descripcion = "Nombre de la categoría del menú donde se publican los combos"
                });
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "No se pudo asegurar el parámetro de la categoría de combos.");
        }
    }

    public static async Task SembrarAdministradorAsync(IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            if (!await db.Database.CanConnectAsync())
            {
                log.LogWarning("No hay conexión con la base de datos. Ejecute database/RestauranteControlDB.sql y revise appsettings.json.");
                return;
            }
            if (await db.Usuarios.AnyAsync())
            {
                return;
            }

            var rol = await db.Roles.FirstOrDefaultAsync(r => r.EsAdministrador && r.Activo);
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];
            var nombre = config["Seed:AdminNombre"];
            if (rol == null || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                log.LogWarning("No se pudo crear el administrador inicial: revise la sección Seed de appsettings.json y los roles de la base.");
                return;
            }

            db.Usuarios.Add(new Usuario
            {
                Nombre = string.IsNullOrWhiteSpace(nombre) ? email : nombre,
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RolId = rol.Id,
                Activo = true,
                DebeCambiarPassword = true
            });
            await db.SaveChangesAsync();
            log.LogInformation("Administrador inicial creado: {Email}", email);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al sembrar el administrador inicial.");
        }
    }
}
