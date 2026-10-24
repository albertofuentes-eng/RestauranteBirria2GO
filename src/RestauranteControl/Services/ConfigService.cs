using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RestauranteControl.Data;
using RestauranteControl.Models;

namespace RestauranteControl.Services;

/// <summary>Acceso a la configuración de empresa y a los parámetros del sistema (con caché corta).</summary>
public class ConfigService
{
    private const string ClaveEmpresa = "cfg.empresa";
    private const string ClaveParametros = "cfg.parametros";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public ConfigService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Empresa> EmpresaAsync()
    {
        var empresa = await _cache.GetOrCreateAsync(ClaveEmpresa, async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var fila = await _db.Empresas.AsNoTracking().Include(x => x.Moneda).FirstOrDefaultAsync();
            return fila ?? new Empresa { Moneda = new Moneda() };
        });
        return empresa!;
    }

    public void Invalidar()
    {
        _cache.Remove(ClaveEmpresa);
        _cache.Remove(ClaveParametros);
    }

    private async Task<Dictionary<string, string>> ParametrosAsync()
    {
        var dic = await _cache.GetOrCreateAsync(ClaveParametros, async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _db.Parametros.AsNoTracking().ToDictionaryAsync(x => x.Clave, x => x.Valor);
        });
        return dic!;
    }

    public async Task<string> TextoAsync(string clave)
    {
        var parametros = await ParametrosAsync();
        if (!parametros.TryGetValue(clave, out var valor))
        {
            throw new InvalidOperationException($"Falta el parámetro de configuración '{clave}'.");
        }
        return valor;
    }

    public async Task<int> EnteroAsync(string clave)
    {
        var texto = await TextoAsync(clave);
        if (!int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor))
        {
            throw new InvalidOperationException($"El parámetro '{clave}' debe ser un número entero.");
        }
        return valor;
    }

    public async Task<bool> BoolAsync(string clave)
    {
        var texto = await TextoAsync(clave);
        return string.Equals(texto, "true", StringComparison.OrdinalIgnoreCase);
    }
}
