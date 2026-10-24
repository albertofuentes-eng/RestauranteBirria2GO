# Cumplimiento de requisitos

| Requisito | Dónde se cumple |
|---|---|
| 2.1 Modelo normalizado 3FN, PK/FK | `database/RestauranteControlDB.sql` §1 (catálogos para estados, métodos de pago, moneda, tipos de orden; ver `docs/DER.md`) |
| 2.1 CHECK / UNIQUE / DEFAULT / NOT NULL | Mismo script: montos ≥ 0, descuento ≤ subtotal, impuesto 0–100, vigencia, acción de bitácora, tipos de parámetro, correo, capacidad, etc. |
| 2.1 Procedimientos almacenados | §3: `sp_Orden_*`, `sp_Preorden_*`, `sp_Mesa_*`, `sp_Producto_*`, `sp_Promocion_*`, `sp_Usuario_Guardar`, `sp_Reporte_*`, `sp_Mesas_Mapa` |
| 2.1 Transacciones | Todo `sp_*` público: `BEGIN TRAN` + `TRY/CATCH` + `XACT_ABORT`; la bitácora por EF usa transacción propia |
| 2.2 Bitácora | Tabla `Auditoria`; registro por SP y automático por `AppDbContext`; consulta en *Administración > Bitácora* con filtros por usuario, fecha, acción y tabla |
| 2.3 Nada escrito en código | `Empresa`, `Moneda`, `Parametro` (reglas, códigos de estado, prefijos, horarios, seguridad), `Modulo` (menú) y catálogos en BD. El código solo usa las claves de `Param.cs` |
| 2.3 Módulo de configuración | *Configuración*: nombre, NIT, dirección, logotipo (subida o URL), moneda, impuesto, mensaje de comprobante y parámetros |
| 2.3 CRUD de catálogos | Catálogos (zonas, estados de mesa/orden, tipos de orden, métodos de pago, monedas), Menú (categorías, platillos), Mesas, Promociones, Usuarios, Roles |
| 2.4 Login con hash | BCrypt, bloqueo por intentos, cambio obligatorio de contraseña inicial |
| 2.4 Roles y permisos | `Rol`, `Modulo`, `RolPermiso` (Ver/Crear/Editar/Eliminar) + filtro `[Permiso]` y menú dinámico; Administrador, Mesero, Cocina, Cajero |
| Opción 1 — módulos y requisitos funcionales | Mesas (mapa en vivo), Reservas (preorden con platillos, atender → orden), órdenes para llevar/delivery, Cocina kanban por platillo u orden, Menú, Promociones/combos con vigencia aplicadas automáticamente, Caja + comprobante, Reportes (ventas, más vendidos, tiempo de preparación) |

Notas: los textos de interfaz (etiquetas de pantallas) están en las vistas; todo valor de negocio (nombres, tarifas, estados, reglas) está en base de datos.
