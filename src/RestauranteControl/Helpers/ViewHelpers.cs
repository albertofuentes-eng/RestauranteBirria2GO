using Microsoft.AspNetCore.Mvc.Rendering;
using RestauranteControl.Models;

namespace RestauranteControl.Helpers;

public static class ViewHelpers
{
    /// <summary>Formatea un monto con el símbolo de la moneda configurada para la empresa.</summary>
    public static string Dinero(this IHtmlHelper html, decimal valor)
    {
        var empresa = html.ViewData["Empresa"] as Empresa;
        var simbolo = empresa?.Moneda?.Simbolo ?? "";
        var numero = valor.ToString("N2");
        return string.IsNullOrEmpty(simbolo) ? numero : simbolo + " " + numero;
    }

    public static string FechaHora(this DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm");

    public static string Fecha(this DateTime fecha) => fecha.ToString("dd/MM/yyyy");
}
