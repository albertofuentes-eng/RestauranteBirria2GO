using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Models;

namespace RestauranteControl.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ConfigService _cfg;

    public AuthService(AppDbContext db, IHttpContextAccessor http, ConfigService cfg)
    {
        _db = db;
        _http = http;
        _cfg = cfg;
    }

    public async Task<(bool Ok, string? Error)> LoginAsync(string? email, string? password)
    {
        const string generico = "Credenciales inválidas.";
        var correo = (email ?? "").Trim().ToLowerInvariant();
        var u = await _db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.Email == correo);
        if (u == null)
        {
            return (false, generico);
        }
        if (u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > DateTime.Now)
        {
            return (false, $"Cuenta bloqueada temporalmente hasta las {u.BloqueadoHasta.Value:HH:mm}.");
        }
        if (!BCrypt.Net.BCrypt.Verify(password ?? "", u.PasswordHash))
        {
            var max = await _cfg.EnteroAsync(Param.SeguridadMaxIntentos);
            var minutos = await _cfg.EnteroAsync(Param.SeguridadMinutosBloqueo);
            var intentos = u.IntentosFallidos + 1;
            if (intentos >= max)
            {
                var hasta = DateTime.Now.AddMinutes(minutos);
                await _db.Usuarios.Where(x => x.Id == u.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IntentosFallidos, 0).SetProperty(x => x.BloqueadoHasta, hasta));
            }
            else
            {
                await _db.Usuarios.Where(x => x.Id == u.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IntentosFallidos, intentos));
            }
            return (false, generico);
        }
        if (!u.Activo || u.Rol == null || !u.Rol.Activo)
        {
            return (false, "El usuario o su rol están inactivos.");
        }

        var ahora = DateTime.Now;
        await _db.Usuarios.Where(x => x.Id == u.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IntentosFallidos, 0)
                                      .SetProperty(x => x.BloqueadoHasta, (DateTime?)null)
                                      .SetProperty(x => x.UltimoAcceso, (DateTime?)ahora));
        await IniciarSesionAsync(u);
        return (true, null);
    }

    public async Task IniciarSesionAsync(Usuario u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.Nombre),
            new(ClaimTypes.Email, u.Email),
            new("Rol", u.Rol?.Nombre ?? ""),
            new("DebeCambiarPassword", u.DebeCambiarPassword ? "1" : "0")
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await _http.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
    }

    public async Task CerrarSesionAsync()
    {
        await _http.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<string?> CambiarPasswordAsync(int usuarioId, string? actual, string? nueva, string? confirmar)
    {
        var u = await _db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.Id == usuarioId);
        if (u == null)
        {
            return "Usuario no encontrado.";
        }
        if (!BCrypt.Net.BCrypt.Verify(actual ?? "", u.PasswordHash))
        {
            return "La contraseña actual no es correcta.";
        }
        if (nueva != confirmar)
        {
            return "La confirmación no coincide con la nueva contraseña.";
        }
        var error = await ValidarPasswordAsync(nueva);
        if (error != null)
        {
            return error;
        }
        if (BCrypt.Net.BCrypt.Verify(nueva!, u.PasswordHash))
        {
            return "La nueva contraseña debe ser distinta de la actual.";
        }
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nueva);
        u.DebeCambiarPassword = false;
        await _db.SaveChangesAsync();
        await IniciarSesionAsync(u);
        return null;
    }

    public async Task<string?> ValidarPasswordAsync(string? password)
    {
        var minimo = await _cfg.EnteroAsync(Param.SeguridadLongitudPassword);
        if (string.IsNullOrEmpty(password) || password.Length < minimo)
        {
            return $"La contraseña debe tener al menos {minimo} caracteres.";
        }
        return null;
    }
}
