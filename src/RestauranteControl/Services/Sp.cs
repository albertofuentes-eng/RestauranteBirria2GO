using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;

namespace RestauranteControl.Services;

/// <summary>Ayudantes para invocar procedimientos almacenados desde EF Core.</summary>
public static class Sp
{
    /// <summary>
    /// Ejecuta un procedimiento que devuelve una fila con la columna Value (Id del registro afectado).
    /// Los errores de negocio (THROW 50000+) llegan como SqlException.
    /// </summary>
    public static async Task<long> EjecutarAsync(this AppDbContext db, string procedimiento, params (string Nombre, object? Valor)[] args)
    {
        var filas = await db.Database
            .SqlQueryRaw<long>(Armar(procedimiento, args), Parametros(args))
            .ToListAsync();
        return filas.FirstOrDefault();
    }

    /// <summary>Ejecuta un procedimiento de consulta y mapea el resultado a un DTO.</summary>
    public static async Task<List<T>> ConsultarAsync<T>(this AppDbContext db, string procedimiento, params (string Nombre, object? Valor)[] args)
    {
        return await db.Database
            .SqlQueryRaw<T>(Armar(procedimiento, args), Parametros(args))
            .ToListAsync();
    }

    private static string Armar(string procedimiento, (string Nombre, object? Valor)[] args)
    {
        if (args.Length == 0)
        {
            return "EXEC " + procedimiento;
        }
        return "EXEC " + procedimiento + " " + string.Join(", ", args.Select(a => "@" + a.Nombre + " = @" + a.Nombre));
    }

    private static object[] Parametros((string Nombre, object? Valor)[] args)
    {
        return args.Select(a => (object)new SqlParameter("@" + a.Nombre, a.Valor ?? DBNull.Value)).ToArray();
    }
}
