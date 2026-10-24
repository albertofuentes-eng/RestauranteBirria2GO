namespace RestauranteControl.Security;

/// <summary>Códigos de módulo (coinciden con dbo.Modulo.Codigo).</summary>
public static class Modulos
{
    public const string Mesas = "MESAS";
    public const string Ordenes = "ORDENES";
    public const string Preordenes = "PREORDENES";
    public const string Cocina = "COCINA";
    public const string Menu = "MENU";
    public const string Promociones = "PROMOS";
    public const string Caja = "CAJA";
    public const string Reportes = "REPORTES";
    public const string Auditoria = "AUDITORIA";
    public const string Configuracion = "CONFIG";
    public const string Catalogos = "CATALOGOS";
    public const string Usuarios = "USUARIOS";
    public const string Roles = "ROLES";
}

/// <summary>Acciones de permiso (columnas de dbo.RolPermiso).</summary>
public static class Acciones
{
    public const string Ver = "Ver";
    public const string Crear = "Crear";
    public const string Editar = "Editar";
    public const string Eliminar = "Eliminar";
}
