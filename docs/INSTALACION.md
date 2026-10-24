# Instalación

Requisitos: .NET SDK 8, SQL Server 2017 o superior (LocalDB, Express o Developer) y SSMS/Azure Data Studio.

1. **Base de datos**: ejecute `database/RestauranteControlDB.sql` completo. Crea tablas, restricciones, datos iniciales (catálogos, parámetros, roles, permisos, mesas y menú de ejemplo), funciones y procedimientos almacenados. Advertencia: borra la base `RestauranteControlDB` si ya existe (también reemplaza instalaciones de versiones anteriores).
2. **Conexión**: `src/RestauranteControl/appsettings.json > ConnectionStrings:DefaultConnection`.
3. **Administrador inicial**: sección `Seed` del mismo archivo. Solo se usa si la tabla de usuarios está vacía. La contraseña se guarda con hash BCrypt y debe cambiarse en el primer ingreso.
4. **Ejecutar**: `dotnet run --project src/RestauranteControl`.
5. **Primeros pasos**: Configuración (nombre, logotipo, moneda, impuesto) → Catálogos (zonas, métodos de pago…) → Mesas > Administrar → Menú → Usuarios.

Si la aplicación no conecta, el administrador no se crea y el aviso aparece en la consola.
