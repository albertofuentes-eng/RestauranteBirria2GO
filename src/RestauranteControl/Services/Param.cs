namespace RestauranteControl.Services;

/// <summary>
/// Claves (identificadores técnicos) de la tabla dbo.Parametro. Los VALORES viven en la
/// base de datos y se editan desde Configuración; aquí solo se nombran las claves.
/// </summary>
public static class Param
{
    public const string OrdenEstadoCerrado = "ORDEN.ESTADO_CERRADO";
    public const string OrdenEstadoListo = "ORDEN.ESTADO_LISTO";
    public const string OrdenEstadoEntregado = "ORDEN.ESTADO_ENTREGADO";
    public const string OrdenEstadoInicial = "ORDEN.ESTADO_INICIAL";
    public const string MesaEstadoLibre = "MESA.ESTADO_LIBRE";
    public const string MesaEstadoOcupada = "MESA.ESTADO_OCUPADA";
    public const string PreordenEstadoActiva = "PREORDEN.ESTADO_ACTIVA";
    public const string ComprobantePrefijo = "COMPROBANTE.PREFIJO";
    public const string ComprobanteDigitos = "COMPROBANTE.DIGITOS";
    public const string SeguridadMaxIntentos = "SEGURIDAD.MAX_INTENTOS";
    public const string SeguridadMinutosBloqueo = "SEGURIDAD.MINUTOS_BLOQUEO";
    public const string SeguridadLongitudPassword = "SEGURIDAD.LONGITUD_MIN_PASSWORD";
    public const string UiRefrescoSegundos = "UI.REFRESCO_SEGUNDOS";
    public const string UiFilasBitacora = "UI.FILAS_BITACORA";
    public const string ReporteTopPlatillos = "REPORTE.TOP_PLATILLOS";
    public const string CombosCategoria = "COMBOS.CATEGORIA";
}
