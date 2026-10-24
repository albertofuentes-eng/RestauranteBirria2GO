using RestauranteControl.Models;

namespace RestauranteControl.Services;

public record CampoCatalogo(string Propiedad, string Etiqueta, string Tipo, bool Requerido = true);

public record DefCatalogo(string Clave, string Titulo, string Icono, Type Entidad, CampoCatalogo[] Campos, string? CampoActivo);

/// <summary>
/// Describe los catálogos que se mantienen desde la pantalla genérica de Catálogos.
/// Tipos de campo: text, int, bool, color.
/// </summary>
public static class CatalogoRegistry
{
    public static readonly IReadOnlyList<DefCatalogo> Todos = new List<DefCatalogo>
    {
        new("zonas", "Zonas", "bi-columns-gap", typeof(Zona),
            new[] { new CampoCatalogo("Nombre", "Nombre", "text") }, "Activa"),

        new("estados-mesa", "Estados de mesa", "bi-circle-half", typeof(EstadoMesa),
            new[]
            {
                new CampoCatalogo("Codigo", "Código", "text"),
                new CampoCatalogo("Nombre", "Nombre", "text"),
                new CampoCatalogo("Color", "Color", "color")
            }, "Activo"),

        new("tipos-orden", "Tipos de orden", "bi-bag", typeof(TipoOrden),
            new[]
            {
                new CampoCatalogo("Codigo", "Código", "text"),
                new CampoCatalogo("Nombre", "Nombre", "text"),
                new CampoCatalogo("RequiereMesa", "Requiere mesa", "bool"),
                new CampoCatalogo("RequiereDireccion", "Requiere dirección", "bool")
            }, "Activo"),

        new("estados-orden", "Estados de orden / cocina", "bi-kanban", typeof(EstadoOrden),
            new[]
            {
                new CampoCatalogo("Codigo", "Código", "text"),
                new CampoCatalogo("Nombre", "Nombre", "text"),
                new CampoCatalogo("OrdenVisual", "Orden", "int"),
                new CampoCatalogo("Color", "Color", "color")
            }, "Activo"),

        new("metodos-pago", "Métodos de pago", "bi-credit-card", typeof(MetodoPago),
            new[] { new CampoCatalogo("Nombre", "Nombre", "text") }, "Activo"),

        new("monedas", "Monedas", "bi-currency-exchange", typeof(Moneda),
            new[]
            {
                new CampoCatalogo("Codigo", "Código", "text"),
                new CampoCatalogo("Nombre", "Nombre", "text"),
                new CampoCatalogo("Simbolo", "Símbolo", "text")
            }, "Activa")
    };

    public static DefCatalogo? Buscar(string? clave) => Todos.FirstOrDefault(c => c.Clave == clave);
}
