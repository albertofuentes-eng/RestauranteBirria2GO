# Restaurante con Control de Mesas

Aplicación web (ASP.NET Core MVC .NET 8 + SQL Server) para controlar mesas, preórdenes/reservas, órdenes en mesa / para llevar / delivery, cocina (tablero kanban), menú, promociones y combos, caja con comprobante, reportes y bitácora. Es configurable para cualquier restaurante: nombre, logotipo, moneda, impuestos, catálogos, tarifas y reglas se administran desde la propia aplicación.

## Puesta en marcha rápida
1. En SSMS (o Azure Data Studio) abra `database/RestauranteControlDB.sql` y ejecútelo completo. **Elimina y recrea** la base `RestauranteControlDB`.
2. Revise la cadena de conexión en `src/RestauranteControl/appsettings.json`.
3. Ejecute `dotnet run --project src/RestauranteControl` (o F5 en Visual Studio).
4. Ingrese con el usuario inicial de `appsettings.json > Seed` (`admin@restaurante.local` / `Admin123!`). La aplicación lo crea al arrancar, con la contraseña cifrada (BCrypt), y pide cambiarla en el primer ingreso.

## Documentación
- `docs/CUMPLIMIENTO_REQUISITOS.md`: dónde se cumple cada requisito (2.1 a 2.4 y módulos de la Opción 1).
- `docs/INSTALACION.md`, `docs/ARQUITECTURA.md`, `docs/DER.md`, `docs/MANUAL_USUARIO.md`, `docs/GIT_Y_KANBAN.md`.

## Roles incluidos
Administrador (acceso total), Mesero, Cocina y Cajero. Los permisos por módulo se editan en *Administración > Roles y permisos*.
