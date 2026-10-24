# Arquitectura

- **Presentación**: ASP.NET Core MVC (Razor) + Bootstrap 5. El mapa de mesas y el tablero de cocina se refrescan solos consultando fragmentos (`/Mesas/Mapa`, `/Cocina/Tablero`) cada `UI.REFRESCO_SEGUNDOS`.
- **Seguridad**: autenticación por cookie; contraseñas con BCrypt; bloqueo por intentos fallidos; filtro global de autorización + `[Permiso(módulo, acción)]` que consulta `RolPermiso` en cada petición. El menú se construye desde la tabla `Modulo` según los permisos del rol.
- **Datos**: EF Core para consultas y mantenimientos simples; **procedimientos almacenados** (`dbo.sp_*`) para las operaciones críticas: órdenes, platillos de la orden, cierre de cuenta con pago, anulación, reservas, menú, promociones, usuarios y reportes. Cada uno usa `TRY/CATCH`, `XACT_ABORT` y transacción, y devuelve errores de negocio con `THROW`.
- **Cálculo de cuenta**: `sp_Orden_Recalcular` aplica las promociones vigentes (primero la que más ahorra, sin reutilizar unidades), calcula impuesto con la tasa de `Empresa` y mantiene totales y estado de la orden.
- **Bitácora**: los procedimientos escriben en `Auditoria`; los cambios hechos con EF se registran automáticamente en `AppDbContext.SaveChangesAsync` (usuario, acción, tabla, registro, fecha y valores anteriores/nuevos; las contraseñas se enmascaran).
- **Configuración**: `Empresa`, `Moneda`, `Parametro` y los catálogos viven en la base; `ConfigService` los lee con caché corta. El código solo conoce las *claves* de parámetros (`Param.cs`).
