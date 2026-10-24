using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

[Permiso(Modulos.Configuracion)]
public class ConfiguracionController : BaseController
{
    private static readonly string[] ExtensionesLogo = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxLogoBytes = 1_000_000;

    private readonly AppDbContext _db;
    private readonly ConfigService _cfg;
    private readonly IWebHostEnvironment _env;

    public ConfiguracionController(AppDbContext db, ConfigService cfg, IWebHostEnvironment env)
    {
        _db = db;
        _cfg = cfg;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Parametros = await _db.Parametros.AsNoTracking().OrderBy(p => p.Grupo).ThenBy(p => p.Clave).ToListAsync();
        ViewBag.Monedas = await _db.Monedas.AsNoTracking().Where(m => m.Activa).OrderBy(m => m.Codigo).ToListAsync();
        var empresa = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync();
        return View(empresa);
    }

    [Permiso(Modulos.Configuracion, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> GuardarEmpresa(string nombre, string? nit, string direccion, string? telefono, string? email,
                                                    int monedaId, string impuestoNombre, decimal impuestoPorcentaje,
                                                    string? mensajeComprobante, string? logoUrl, IFormFile? logoArchivo, bool quitarLogo = false)
    {
        try
        {
            var empresa = await _db.Empresas.FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("No existe el registro de empresa. Ejecute el script de base de datos.");
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new InvalidOperationException("El nombre de la empresa es obligatorio.");
            }
            if (impuestoPorcentaje < 0 || impuestoPorcentaje > 100)
            {
                throw new InvalidOperationException("El impuesto debe estar entre 0 y 100.");
            }
            if (!await _db.Monedas.AnyAsync(m => m.Id == monedaId && m.Activa))
            {
                throw new InvalidOperationException("Seleccione una moneda válida.");
            }

            empresa.Nombre = nombre.Trim();
            empresa.Nit = Limpiar(nit);
            empresa.Direccion = (direccion ?? "").Trim();
            empresa.Telefono = Limpiar(telefono);
            empresa.Email = Limpiar(email);
            empresa.MonedaId = monedaId;
            empresa.ImpuestoNombre = string.IsNullOrWhiteSpace(impuestoNombre) ? empresa.ImpuestoNombre : impuestoNombre.Trim();
            empresa.ImpuestoPorcentaje = impuestoPorcentaje;
            empresa.MensajeComprobante = Limpiar(mensajeComprobante);

            if (logoArchivo is { Length: > 0 })
            {
                empresa.LogoUrl = await GuardarLogoAsync(logoArchivo);
            }
            else if (quitarLogo)
            {
                empresa.LogoUrl = null;
            }
            else if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                empresa.LogoUrl = logoUrl.Trim();
            }

            await _db.SaveChangesAsync();
            _cfg.Invalidar();
            Exito("Configuración de la empresa guardada.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    [Permiso(Modulos.Configuracion, Acciones.Editar)]
    [HttpPost]
    public async Task<IActionResult> GuardarParametro(int id, string valor)
    {
        try
        {
            var parametro = await _db.Parametros.FindAsync(id)
                ?? throw new InvalidOperationException("El parámetro no existe.");
            valor = (valor ?? "").Trim();
            ValidarTipo(parametro.TipoDato, valor);
            parametro.Valor = valor;
            await _db.SaveChangesAsync();
            _cfg.Invalidar();
            Exito($"Parámetro {parametro.Clave} actualizado.");
        }
        catch (Exception ex) { Falla(ex); }
        return RedirectToAction(nameof(Index));
    }

    private static void ValidarTipo(string tipo, string valor)
    {
        if (valor.Length == 0)
        {
            throw new InvalidOperationException("El valor no puede estar vacío.");
        }
        var valido = tipo switch
        {
            "INT" => int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 0,
            "DECIMAL" => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            "BOOL" => valor.Equals("true", StringComparison.OrdinalIgnoreCase) || valor.Equals("false", StringComparison.OrdinalIgnoreCase),
            "TIME" => TimeSpan.TryParseExact(valor, @"hh\:mm", CultureInfo.InvariantCulture, out _),
            _ => true
        };
        if (!valido)
        {
            throw new InvalidOperationException($"El valor no es válido para el tipo {tipo} (use true/false, números enteros o la hora como HH:mm).");
        }
    }

    private async Task<string> GuardarLogoAsync(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesLogo.Contains(extension))
        {
            throw new InvalidOperationException("El logotipo debe ser PNG, JPG, SVG o WEBP.");
        }
        if (archivo.Length > MaxLogoBytes)
        {
            throw new InvalidOperationException("El logotipo no puede superar 1 MB.");
        }
        var carpeta = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(carpeta);
        var nombre = "logo-" + Guid.NewGuid().ToString("N") + extension;
        await using (var flujo = System.IO.File.Create(Path.Combine(carpeta, nombre)))
        {
            await archivo.CopyToAsync(flujo);
        }
        return "/uploads/" + nombre;
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
