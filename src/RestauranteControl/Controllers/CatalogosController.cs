using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Security;
using RestauranteControl.Services;

namespace RestauranteControl.Controllers;

/// <summary>Mantenimiento (CRUD) genérico de los catálogos del sistema, guiado por CatalogoRegistry.</summary>
[Permiso(Modulos.Catalogos)]
public class CatalogosController : BaseController
{
    private readonly AppDbContext _db;

    public CatalogosController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Index(string? tipo)
    {
        var def = CatalogoRegistry.Buscar(tipo) ?? CatalogoRegistry.Todos[0];
        ViewBag.Def = def;
        return View(Listar(def));
    }

    private List<object> Listar(DefCatalogo def)
    {
        var metodo = typeof(DbContext).GetMethods()
            .First(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
            .MakeGenericMethod(def.Entidad);
        var consulta = (IEnumerable<object>)metodo.Invoke(_db, null)!;
        var idProp = def.Entidad.GetProperty("Id")!;
        return consulta.AsEnumerable().OrderBy(x => idProp.GetValue(x)).ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Guardar(string tipo, int? id)
    {
        var def = CatalogoRegistry.Buscar(tipo);
        if (def == null)
        {
            return NotFound();
        }
        var editando = id.HasValue && id.Value > 0;
        if (!await PuedeAsync(Modulos.Catalogos, editando ? Acciones.Editar : Acciones.Crear))
        {
            return SinPermiso();
        }
        try
        {
            object entidad;
            if (editando)
            {
                entidad = await _db.FindAsync(def.Entidad, id!.Value)
                    ?? throw new InvalidOperationException("El registro no existe.");
            }
            else
            {
                entidad = Activator.CreateInstance(def.Entidad)!;
            }

            var codigoProp = def.Entidad.GetProperty("Codigo");
            var codigoAnterior = editando ? codigoProp?.GetValue(entidad) as string : null;

            foreach (var campo in def.Campos)
            {
                var prop = def.Entidad.GetProperty(campo.Propiedad)!;
                var crudo = Request.Form[campo.Propiedad].ToString();
                prop.SetValue(entidad, Convertir(campo, prop, crudo));
            }
            if (def.CampoActivo != null)
            {
                var activoProp = def.Entidad.GetProperty(def.CampoActivo)!;
                activoProp.SetValue(entidad, editando ? Request.Form[def.CampoActivo].ToString().Contains("true", StringComparison.OrdinalIgnoreCase) : true);
            }

            // Los códigos referenciados por parámetros del sistema no pueden cambiar
            if (editando && codigoProp != null)
            {
                var codigoNuevo = codigoProp.GetValue(entidad) as string;
                if (codigoAnterior != codigoNuevo && await _db.Parametros.AnyAsync(p => p.Valor == codigoAnterior))
                {
                    throw new InvalidOperationException("Ese código lo usa un parámetro del sistema; cambie primero el parámetro en Configuración.");
                }
            }

            if (!editando)
            {
                _db.Add(entidad);
            }
            await _db.SaveChangesAsync();
            Exito("Registro guardado.");
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            Falla(ex);
        }
        return RedirectToAction(nameof(Index), new { tipo });
    }

    [Permiso(Modulos.Catalogos, Acciones.Eliminar)]
    [HttpPost]
    public async Task<IActionResult> Eliminar(string tipo, int id)
    {
        var def = CatalogoRegistry.Buscar(tipo);
        if (def == null)
        {
            return NotFound();
        }
        try
        {
            var entidad = await _db.FindAsync(def.Entidad, id) ?? throw new InvalidOperationException("El registro no existe.");
            var codigo = def.Entidad.GetProperty("Codigo")?.GetValue(entidad) as string;
            if (codigo != null && await _db.Parametros.AnyAsync(p => p.Valor == codigo))
            {
                throw new InvalidOperationException("Ese código lo usa un parámetro del sistema; no puede eliminarse.");
            }
            _db.Remove(entidad);
            await _db.SaveChangesAsync();
            Exito("Registro eliminado.");
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            var raiz = ex.GetBaseException();
            Falla(ex is DbUpdateException ? "El registro está en uso; desactívelo en lugar de eliminarlo." : MensajeDeError(raiz));
        }
        return RedirectToAction(nameof(Index), new { tipo });
    }

    private static object? Convertir(CampoCatalogo campo, PropertyInfo prop, string crudo)
    {
        crudo = crudo.Trim();
        switch (campo.Tipo)
        {
            case "bool":
                return crudo.Contains("true", StringComparison.OrdinalIgnoreCase);
            case "int":
                if (!int.TryParse(crudo, out var n))
                {
                    throw new InvalidOperationException($"{campo.Etiqueta} debe ser un número entero.");
                }
                return n;
            default:
                if (campo.Requerido && crudo.Length == 0)
                {
                    throw new InvalidOperationException($"{campo.Etiqueta} es obligatorio.");
                }
                return prop.PropertyType == typeof(string) ? crudo : null;
        }
    }
}
