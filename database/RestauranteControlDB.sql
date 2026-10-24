/* =====================================================================
   RESTAURANTE CON CONTROL DE MESAS  -  Script completo de base de datos
   Motor: SQL Server 2017+ (probado para LocalDB / Express / Developer)

   ADVERTENCIA: este script ELIMINA y vuelve a crear la base
   RestauranteControlDB. Ejecútelo completo una sola vez (F5 en SSMS).

   Contenido:
     1. Estructura (tablas, llaves, restricciones, índices)   - 3FN
     2. Datos iniciales (catálogos, parámetros, roles, permisos)
     3. Funciones auxiliares y procedimientos almacenados
   El usuario administrador inicial lo crea la aplicación al arrancar
   (ver appsettings.json > Seed), con la contraseña guardada como hash.
   ===================================================================== */
USE master;
GO
IF DB_ID(N'RestauranteControlDB') IS NOT NULL
BEGIN
    ALTER DATABASE RestauranteControlDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RestauranteControlDB;
END;
GO
CREATE DATABASE RestauranteControlDB;
GO
USE RestauranteControlDB;
GO

/* ===================== 1. ESTRUCTURA ===================== */

-- Moneda: catálogo (el símbolo depende de la moneda, no de la empresa -> 3FN)
CREATE TABLE dbo.Moneda(
    Id      int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Moneda PRIMARY KEY,
    Codigo  nvarchar(10)  NOT NULL CONSTRAINT UQ_Moneda_Codigo UNIQUE,
    Nombre  nvarchar(80)  NOT NULL CONSTRAINT UQ_Moneda_Nombre UNIQUE,
    Simbolo nvarchar(10)  NOT NULL,
    Activa  bit NOT NULL CONSTRAINT DF_Moneda_Activa DEFAULT 1
);

-- Empresa: una sola fila (Id = 1) con la configuración del negocio
CREATE TABLE dbo.Empresa(
    Id                 int NOT NULL CONSTRAINT PK_Empresa PRIMARY KEY
                           CONSTRAINT CK_Empresa_Unica CHECK (Id = 1),
    Nombre             nvarchar(150) NOT NULL CONSTRAINT CK_Empresa_Nombre CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0),
    Nit                nvarchar(30)  NULL,
    Direccion          nvarchar(300) NOT NULL CONSTRAINT DF_Empresa_Direccion DEFAULT N'',
    Telefono           nvarchar(30)  NULL,
    Email              nvarchar(150) NULL,
    LogoUrl            nvarchar(500) NULL,
    MonedaId           int NOT NULL CONSTRAINT FK_Empresa_Moneda REFERENCES dbo.Moneda(Id),
    ImpuestoNombre     nvarchar(30)  NOT NULL CONSTRAINT DF_Empresa_ImpuestoNombre DEFAULT N'Impuesto',
    ImpuestoPorcentaje decimal(5,2)  NOT NULL CONSTRAINT CK_Empresa_Impuesto CHECK (ImpuestoPorcentaje BETWEEN 0 AND 100),
    MensajeComprobante nvarchar(300) NULL
);

-- Seguridad
CREATE TABLE dbo.Rol(
    Id              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rol PRIMARY KEY,
    Nombre          nvarchar(80)  NOT NULL CONSTRAINT UQ_Rol_Nombre UNIQUE,
    Descripcion     nvarchar(200) NULL,
    EsAdministrador bit NOT NULL CONSTRAINT DF_Rol_EsAdmin DEFAULT 0,
    Activo          bit NOT NULL CONSTRAINT DF_Rol_Activo DEFAULT 1
);

CREATE TABLE dbo.Modulo(
    Id               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Modulo PRIMARY KEY,
    Codigo           nvarchar(50)  NOT NULL CONSTRAINT UQ_Modulo_Codigo UNIQUE,
    Nombre           nvarchar(100) NOT NULL,
    Controlador      nvarchar(50)  NOT NULL,
    Icono            nvarchar(50)  NOT NULL CONSTRAINT DF_Modulo_Icono DEFAULT N'bi-circle',
    Orden            int NOT NULL CONSTRAINT DF_Modulo_Orden DEFAULT 0,
    EsAdministracion bit NOT NULL CONSTRAINT DF_Modulo_EsAdm DEFAULT 0,
    Activo           bit NOT NULL CONSTRAINT DF_Modulo_Activo DEFAULT 1
);

CREATE TABLE dbo.RolPermiso(
    RolId    int NOT NULL CONSTRAINT FK_RolPermiso_Rol REFERENCES dbo.Rol(Id) ON DELETE CASCADE,
    ModuloId int NOT NULL CONSTRAINT FK_RolPermiso_Modulo REFERENCES dbo.Modulo(Id) ON DELETE CASCADE,
    Ver      bit NOT NULL CONSTRAINT DF_RolPermiso_Ver DEFAULT 0,
    Crear    bit NOT NULL CONSTRAINT DF_RolPermiso_Crear DEFAULT 0,
    Editar   bit NOT NULL CONSTRAINT DF_RolPermiso_Editar DEFAULT 0,
    Eliminar bit NOT NULL CONSTRAINT DF_RolPermiso_Eliminar DEFAULT 0,
    CONSTRAINT PK_RolPermiso PRIMARY KEY (RolId, ModuloId)
);

CREATE TABLE dbo.Usuario(
    Id                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
    Nombre              nvarchar(120) NOT NULL CONSTRAINT CK_Usuario_Nombre CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0),
    Email               nvarchar(150) NOT NULL CONSTRAINT UQ_Usuario_Email UNIQUE
                            CONSTRAINT CK_Usuario_Email CHECK (Email LIKE N'%_@_%._%'),
    PasswordHash        nvarchar(200) NOT NULL,
    RolId               int NOT NULL CONSTRAINT FK_Usuario_Rol REFERENCES dbo.Rol(Id),
    Activo              bit NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT 1,
    IntentosFallidos    int NOT NULL CONSTRAINT DF_Usuario_Intentos DEFAULT 0 CONSTRAINT CK_Usuario_Intentos CHECK (IntentosFallidos >= 0),
    BloqueadoHasta      datetime2 NULL,
    UltimoAcceso        datetime2 NULL,
    DebeCambiarPassword bit NOT NULL CONSTRAINT DF_Usuario_DebeCambiar DEFAULT 0,
    FechaCreacion       datetime2 NOT NULL CONSTRAINT DF_Usuario_Fecha DEFAULT SYSDATETIME()
);

-- Mesas y zonas
CREATE TABLE dbo.Zona(
    Id     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Zona PRIMARY KEY,
    Nombre nvarchar(80) NOT NULL CONSTRAINT UQ_Zona_Nombre UNIQUE,
    Activa bit NOT NULL CONSTRAINT DF_Zona_Activa DEFAULT 1
);

CREATE TABLE dbo.EstadoMesa(
    Id     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EstadoMesa PRIMARY KEY,
    Codigo nvarchar(30) NOT NULL CONSTRAINT UQ_EstadoMesa_Codigo UNIQUE,
    Nombre nvarchar(60) NOT NULL CONSTRAINT UQ_EstadoMesa_Nombre UNIQUE,
    Color  nvarchar(20) NOT NULL CONSTRAINT DF_EstadoMesa_Color DEFAULT N'#6c757d',
    Activo bit NOT NULL CONSTRAINT DF_EstadoMesa_Activo DEFAULT 1
);

CREATE TABLE dbo.Mesa(
    Id           int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Mesa PRIMARY KEY,
    Numero       nvarchar(20) NOT NULL CONSTRAINT UQ_Mesa_Numero UNIQUE,
    ZonaId       int NOT NULL CONSTRAINT FK_Mesa_Zona REFERENCES dbo.Zona(Id),
    Capacidad    int NOT NULL CONSTRAINT CK_Mesa_Capacidad CHECK (Capacidad > 0),
    EstadoMesaId int NOT NULL CONSTRAINT FK_Mesa_Estado REFERENCES dbo.EstadoMesa(Id),
    Activa       bit NOT NULL CONSTRAINT DF_Mesa_Activa DEFAULT 1
);

-- Menú
CREATE TABLE dbo.CategoriaProducto(
    Id     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CategoriaProducto PRIMARY KEY,
    Nombre nvarchar(100) NOT NULL CONSTRAINT UQ_Categoria_Nombre UNIQUE,
    Activa bit NOT NULL CONSTRAINT DF_Categoria_Activa DEFAULT 1
);

CREATE TABLE dbo.Producto(
    Id                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Producto PRIMARY KEY,
    Nombre              nvarchar(150) NOT NULL,
    Descripcion         nvarchar(500) NULL,
    CategoriaProductoId int NOT NULL CONSTRAINT FK_Producto_Categoria REFERENCES dbo.CategoriaProducto(Id),
    Precio              decimal(18,2) NOT NULL CONSTRAINT CK_Producto_Precio CHECK (Precio >= 0),
    Disponible          bit NOT NULL CONSTRAINT DF_Producto_Disponible DEFAULT 1,
    Activo              bit NOT NULL CONSTRAINT DF_Producto_Activo DEFAULT 1,
    CONSTRAINT UQ_Producto UNIQUE (Nombre, CategoriaProductoId)
);

-- Catálogos de órdenes
CREATE TABLE dbo.TipoOrden(
    Id                int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TipoOrden PRIMARY KEY,
    Codigo            nvarchar(30) NOT NULL CONSTRAINT UQ_TipoOrden_Codigo UNIQUE,
    Nombre            nvarchar(80) NOT NULL CONSTRAINT UQ_TipoOrden_Nombre UNIQUE,
    RequiereMesa      bit NOT NULL CONSTRAINT DF_TipoOrden_Mesa DEFAULT 0,
    RequiereDireccion bit NOT NULL CONSTRAINT DF_TipoOrden_Dir DEFAULT 0,
    Activo            bit NOT NULL CONSTRAINT DF_TipoOrden_Activo DEFAULT 1
);

CREATE TABLE dbo.EstadoOrden(
    Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EstadoOrden PRIMARY KEY,
    Codigo      nvarchar(30) NOT NULL CONSTRAINT UQ_EstadoOrden_Codigo UNIQUE,
    Nombre      nvarchar(80) NOT NULL CONSTRAINT UQ_EstadoOrden_Nombre UNIQUE,
    OrdenVisual int NOT NULL CONSTRAINT CK_EstadoOrden_Orden CHECK (OrdenVisual > 0),
    Color       nvarchar(20) NOT NULL CONSTRAINT DF_EstadoOrden_Color DEFAULT N'#6c757d',
    Activo      bit NOT NULL CONSTRAINT DF_EstadoOrden_Activo DEFAULT 1
);

CREATE TABLE dbo.MetodoPago(
    Id     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MetodoPago PRIMARY KEY,
    Nombre nvarchar(50) NOT NULL CONSTRAINT UQ_MetodoPago_Nombre UNIQUE,
    Activo bit NOT NULL CONSTRAINT DF_MetodoPago_Activo DEFAULT 1
);

CREATE TABLE dbo.EstadoPreorden(
    Id     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EstadoPreorden PRIMARY KEY,
    Codigo nvarchar(30) NOT NULL CONSTRAINT UQ_EstadoPreorden_Codigo UNIQUE,
    Nombre nvarchar(60) NOT NULL CONSTRAINT UQ_EstadoPreorden_Nombre UNIQUE
);

-- Promociones y combos
CREATE TABLE dbo.Promocion(
    Id            int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promocion PRIMARY KEY,
    Nombre        nvarchar(150) NOT NULL CONSTRAINT UQ_Promocion_Nombre UNIQUE,
    Descripcion   nvarchar(300) NULL,
    PrecioCombo   decimal(18,2) NOT NULL CONSTRAINT CK_Promocion_Precio CHECK (PrecioCombo >= 0),
    VigenciaDesde datetime2 NOT NULL,
    VigenciaHasta datetime2 NOT NULL,
    Activa        bit NOT NULL CONSTRAINT DF_Promocion_Activa DEFAULT 1,
    CONSTRAINT CK_Promocion_Vigencia CHECK (VigenciaHasta >= VigenciaDesde)
);

CREATE TABLE dbo.PromocionDetalle(
    PromocionId int NOT NULL CONSTRAINT FK_PromoDet_Promo REFERENCES dbo.Promocion(Id) ON DELETE CASCADE,
    ProductoId  int NOT NULL CONSTRAINT FK_PromoDet_Producto REFERENCES dbo.Producto(Id),
    Cantidad    int NOT NULL CONSTRAINT CK_PromoDet_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT PK_PromocionDetalle PRIMARY KEY (PromocionId, ProductoId)
);

-- Órdenes
CREATE TABLE dbo.Orden(
    Id               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orden PRIMARY KEY,
    TipoOrdenId      int NOT NULL CONSTRAINT FK_Orden_Tipo REFERENCES dbo.TipoOrden(Id),
    MesaId           int NULL CONSTRAINT FK_Orden_Mesa REFERENCES dbo.Mesa(Id),
    EstadoOrdenId    int NOT NULL CONSTRAINT FK_Orden_Estado REFERENCES dbo.EstadoOrden(Id),
    UsuarioId        int NOT NULL CONSTRAINT FK_Orden_Usuario REFERENCES dbo.Usuario(Id),
    Cliente          nvarchar(150) NULL,
    Telefono         nvarchar(30)  NULL,
    DireccionEntrega nvarchar(300) NULL,
    Fecha            datetime2 NOT NULL CONSTRAINT DF_Orden_Fecha DEFAULT SYSDATETIME(),
    FechaCierre      datetime2 NULL,
    Subtotal         decimal(18,2) NOT NULL CONSTRAINT DF_Orden_Subtotal DEFAULT 0,
    Descuento        decimal(18,2) NOT NULL CONSTRAINT DF_Orden_Descuento DEFAULT 0,
    Impuesto         decimal(18,2) NOT NULL CONSTRAINT DF_Orden_Impuesto DEFAULT 0,
    Total            decimal(18,2) NOT NULL CONSTRAINT DF_Orden_Total DEFAULT 0,
    Cerrada          bit NOT NULL CONSTRAINT DF_Orden_Cerrada DEFAULT 0,
    Anulada          bit NOT NULL CONSTRAINT DF_Orden_Anulada DEFAULT 0,
    MotivoAnulacion  nvarchar(300) NULL,
    CONSTRAINT CK_Orden_Montos CHECK (Subtotal >= 0 AND Descuento >= 0 AND Impuesto >= 0 AND Total >= 0 AND Descuento <= Subtotal),
    CONSTRAINT CK_Orden_Estado CHECK (NOT (Cerrada = 1 AND Anulada = 1)),
    CONSTRAINT CK_Orden_Cierre CHECK (Cerrada = 0 OR FechaCierre IS NOT NULL)
);

CREATE TABLE dbo.OrdenDetalle(
    Id             bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenDetalle PRIMARY KEY,
    OrdenId        bigint NOT NULL CONSTRAINT FK_OrdenDet_Orden REFERENCES dbo.Orden(Id),
    ProductoId     int NOT NULL CONSTRAINT FK_OrdenDet_Producto REFERENCES dbo.Producto(Id),
    Cantidad       int NOT NULL CONSTRAINT CK_OrdenDet_Cantidad CHECK (Cantidad > 0),
    PrecioUnitario decimal(18,2) NOT NULL CONSTRAINT CK_OrdenDet_Precio CHECK (PrecioUnitario >= 0),
    Nota           nvarchar(300) NULL,
    EstadoOrdenId  int NOT NULL CONSTRAINT FK_OrdenDet_Estado REFERENCES dbo.EstadoOrden(Id),
    Fecha          datetime2 NOT NULL CONSTRAINT DF_OrdenDet_Fecha DEFAULT SYSDATETIME()
);

-- Historial de estados de cada platillo (permite medir el tiempo de preparación)
CREATE TABLE dbo.OrdenDetalleEstadoHist(
    Id             bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenDetEstadoHist PRIMARY KEY,
    OrdenDetalleId bigint NOT NULL CONSTRAINT FK_OrdenDetHist_Det REFERENCES dbo.OrdenDetalle(Id) ON DELETE CASCADE,
    EstadoOrdenId  int NOT NULL CONSTRAINT FK_OrdenDetHist_Estado REFERENCES dbo.EstadoOrden(Id),
    Fecha          datetime2 NOT NULL CONSTRAINT DF_OrdenDetHist_Fecha DEFAULT SYSDATETIME(),
    UsuarioId      int NULL CONSTRAINT FK_OrdenDetHist_Usuario REFERENCES dbo.Usuario(Id)
);

-- Promociones aplicadas automáticamente a cada orden
CREATE TABLE dbo.OrdenPromocion(
    OrdenId     bigint NOT NULL CONSTRAINT FK_OrdenPromo_Orden REFERENCES dbo.Orden(Id) ON DELETE CASCADE,
    PromocionId int NOT NULL CONSTRAINT FK_OrdenPromo_Promo REFERENCES dbo.Promocion(Id),
    Veces       int NOT NULL CONSTRAINT CK_OrdenPromo_Veces CHECK (Veces > 0),
    Descuento   decimal(18,2) NOT NULL CONSTRAINT CK_OrdenPromo_Desc CHECK (Descuento >= 0),
    CONSTRAINT PK_OrdenPromocion PRIMARY KEY (OrdenId, PromocionId)
);

-- Preórdenes (reserva con platillos anticipados)
CREATE TABLE dbo.Preorden(
    Id               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Preorden PRIMARY KEY,
    MesaId           int NOT NULL CONSTRAINT FK_Preorden_Mesa REFERENCES dbo.Mesa(Id),
    Cliente          nvarchar(150) NOT NULL,
    Telefono         nvarchar(30) NULL,
    FechaHora        datetime2 NOT NULL,
    EstadoPreordenId int NOT NULL CONSTRAINT FK_Preorden_Estado REFERENCES dbo.EstadoPreorden(Id),
    OrdenId          bigint NULL CONSTRAINT FK_Preorden_Orden REFERENCES dbo.Orden(Id),
    Notas            nvarchar(300) NULL,
    UsuarioId        int NOT NULL CONSTRAINT FK_Preorden_Usuario REFERENCES dbo.Usuario(Id),
    FechaRegistro    datetime2 NOT NULL CONSTRAINT DF_Preorden_Registro DEFAULT SYSDATETIME()
);

CREATE TABLE dbo.PreordenDetalle(
    Id         bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreordenDetalle PRIMARY KEY,
    PreordenId bigint NOT NULL CONSTRAINT FK_PreordenDet_Preorden REFERENCES dbo.Preorden(Id) ON DELETE CASCADE,
    ProductoId int NOT NULL CONSTRAINT FK_PreordenDet_Producto REFERENCES dbo.Producto(Id),
    Cantidad   int NOT NULL CONSTRAINT CK_PreordenDet_Cantidad CHECK (Cantidad > 0),
    Nota       nvarchar(300) NULL,
    CONSTRAINT UQ_PreordenDetalle UNIQUE (PreordenId, ProductoId)
);

-- Pagos (una orden se paga una sola vez)
CREATE TABLE dbo.Pago(
    Id           bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pago PRIMARY KEY,
    OrdenId      bigint NOT NULL CONSTRAINT UQ_Pago_Orden UNIQUE CONSTRAINT FK_Pago_Orden REFERENCES dbo.Orden(Id),
    MetodoPagoId int NOT NULL CONSTRAINT FK_Pago_Metodo REFERENCES dbo.MetodoPago(Id),
    Monto        decimal(18,2) NOT NULL CONSTRAINT CK_Pago_Monto CHECK (Monto >= 0),
    Fecha        datetime2 NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT SYSDATETIME(),
    UsuarioId    int NOT NULL CONSTRAINT FK_Pago_Usuario REFERENCES dbo.Usuario(Id)
);

-- Bitácora (auditoría)
CREATE TABLE dbo.Auditoria(
    Id                bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Auditoria PRIMARY KEY,
    UsuarioId         int NULL,
    Usuario           nvarchar(150) NOT NULL,
    Accion            nvarchar(10)  NOT NULL CONSTRAINT CK_Auditoria_Accion CHECK (Accion IN (N'INSERT', N'UPDATE', N'DELETE')),
    Tabla             nvarchar(100) NOT NULL,
    RegistroId        nvarchar(100) NULL,
    Fecha             datetime2 NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT SYSDATETIME(),
    ValoresAnteriores nvarchar(max) NULL,
    ValoresNuevos     nvarchar(max) NULL,
    Detalle           nvarchar(500) NULL
);

-- Parámetros de negocio (nada de esto vive en el código)
CREATE TABLE dbo.Parametro(
    Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Parametro PRIMARY KEY,
    Clave       nvarchar(100) NOT NULL CONSTRAINT UQ_Parametro_Clave UNIQUE,
    Valor       nvarchar(500) NOT NULL,
    TipoDato    nvarchar(20)  NOT NULL CONSTRAINT DF_Parametro_Tipo DEFAULT N'TEXT'
                    CONSTRAINT CK_Parametro_Tipo CHECK (TipoDato IN (N'TEXT', N'INT', N'DECIMAL', N'BOOL', N'TIME')),
    Grupo       nvarchar(50)  NOT NULL CONSTRAINT DF_Parametro_Grupo DEFAULT N'General',
    Descripcion nvarchar(300) NULL
);
GO

-- Índices de apoyo a consultas y reportes
CREATE INDEX IX_Orden_Fecha        ON dbo.Orden(Fecha);
CREATE INDEX IX_Orden_FechaCierre  ON dbo.Orden(FechaCierre) WHERE Cerrada = 1;
CREATE INDEX IX_Orden_Mesa         ON dbo.Orden(MesaId) WHERE MesaId IS NOT NULL;
CREATE INDEX IX_Orden_Estado       ON dbo.Orden(EstadoOrdenId);
CREATE INDEX IX_OrdenDetalle_Orden ON dbo.OrdenDetalle(OrdenId);
CREATE INDEX IX_OrdenDetalle_Prod  ON dbo.OrdenDetalle(ProductoId);
CREATE INDEX IX_OrdenDetHist_Det   ON dbo.OrdenDetalleEstadoHist(OrdenDetalleId, EstadoOrdenId);
CREATE INDEX IX_Preorden_Mesa      ON dbo.Preorden(MesaId, FechaHora);
CREATE INDEX IX_Auditoria_Fecha    ON dbo.Auditoria(Fecha);
CREATE INDEX IX_Auditoria_Usuario  ON dbo.Auditoria(Usuario);
CREATE INDEX IX_Auditoria_Tabla    ON dbo.Auditoria(Tabla, Accion);
GO

/* ===================== 2. DATOS INICIALES ===================== */
-- Todo lo que sigue es configuración editable desde la aplicación.

INSERT dbo.Moneda(Codigo, Nombre, Simbolo) VALUES
 (N'GTQ', N'Quetzal guatemalteco', N'Q'),
 (N'USD', N'Dólar estadounidense', N'$'),
 (N'MXN', N'Peso mexicano', N'$'),
 (N'EUR', N'Euro', N'€');

INSERT dbo.Empresa(Id, Nombre, Nit, Direccion, Telefono, Email, MonedaId, ImpuestoNombre, ImpuestoPorcentaje, MensajeComprobante)
VALUES (1, N'Restaurante Demo', N'CF', N'Ciudad de Guatemala', N'', N'', 1, N'IVA', 12, N'¡Gracias por su visita!');

INSERT dbo.Rol(Nombre, Descripcion, EsAdministrador) VALUES
 (N'Administrador', N'Acceso total: configuración, catálogos, usuarios y bitácora', 1),
 (N'Mesero',        N'Mesas, órdenes y reservas',                                   0),
 (N'Cocina',        N'Tablero de cocina',                                           0),
 (N'Cajero',        N'Cobro de cuentas y consulta de ventas',                       0);

INSERT dbo.Modulo(Codigo, Nombre, Controlador, Icono, Orden, EsAdministracion) VALUES
 (N'MESAS',      N'Mesas',                N'Mesas',         N'bi-grid-3x3-gap', 10,  0),
 (N'ORDENES',    N'Órdenes',              N'Ordenes',       N'bi-receipt',      20,  0),
 (N'PREORDENES', N'Reservas',             N'Preordenes',    N'bi-calendar-check', 30, 0),
 (N'COCINA',     N'Cocina',               N'Cocina',        N'bi-fire',         40,  0),
 (N'MENU',       N'Menú',                 N'Menu',          N'bi-journal-text', 50,  0),
 (N'PROMOS',     N'Promociones',          N'Promociones',   N'bi-tags',         60,  0),
 (N'CAJA',       N'Caja',                 N'Caja',          N'bi-cash-stack',   70,  0),
 (N'REPORTES',   N'Reportes',             N'Reportes',      N'bi-bar-chart',    80,  0),
 (N'AUDITORIA',  N'Bitácora',             N'Auditoria',     N'bi-clock-history', 90, 1),
 (N'CONFIG',     N'Configuración',        N'Configuracion', N'bi-building-gear', 100, 1),
 (N'CATALOGOS',  N'Catálogos',            N'Catalogos',     N'bi-collection',   110, 1),
 (N'USUARIOS',   N'Usuarios',             N'Usuarios',      N'bi-people',       120, 1),
 (N'ROLES',      N'Roles y permisos',     N'Roles',         N'bi-shield-lock',  130, 1);

-- Permisos: el administrador recibe todo; los demás perfiles, lo que necesitan
INSERT dbo.RolPermiso(RolId, ModuloId, Ver, Crear, Editar, Eliminar)
SELECT r.Id, m.Id, 1, 1, 1, 1 FROM dbo.Rol r CROSS JOIN dbo.Modulo m WHERE r.EsAdministrador = 1;

INSERT dbo.RolPermiso(RolId, ModuloId, Ver, Crear, Editar, Eliminar)
SELECT r.Id, m.Id, v.Ver, v.Crear, v.Editar, v.Eliminar
FROM dbo.Rol r
JOIN (VALUES (N'Mesero', N'MESAS',      1,0,1,0),
             (N'Mesero', N'ORDENES',    1,1,1,0),
             (N'Mesero', N'PREORDENES', 1,1,1,1),
             (N'Mesero', N'COCINA',     1,0,0,0),
             (N'Mesero', N'MENU',       1,0,0,0),
             (N'Mesero', N'PROMOS',     1,0,0,0),
             (N'Cocina', N'COCINA',     1,0,1,0),
             (N'Cocina', N'MENU',       1,0,0,0),
             (N'Cajero', N'CAJA',       1,1,0,0),
             (N'Cajero', N'ORDENES',    1,0,0,1),
             (N'Cajero', N'MESAS',      1,0,0,0),
             (N'Cajero', N'REPORTES',   1,0,0,0)) v(Rol, Modulo, Ver, Crear, Editar, Eliminar) ON v.Rol = r.Nombre
JOIN dbo.Modulo m ON m.Codigo = v.Modulo;

INSERT dbo.EstadoMesa(Codigo, Nombre, Color) VALUES
 (N'LIBRE',     N'Libre',       N'#198754'),
 (N'OCUPADA',   N'Ocupada',     N'#dc3545'),
 (N'RESERVADA', N'Reservada',   N'#0d6efd'),
 (N'LIMPIEZA',  N'En limpieza', N'#f0ad00');

INSERT dbo.TipoOrden(Codigo, Nombre, RequiereMesa, RequiereDireccion) VALUES
 (N'MESA',     N'En mesa',     1, 0),
 (N'LLEVAR',   N'Para llevar', 0, 0),
 (N'DELIVERY', N'Delivery',    0, 1);

INSERT dbo.EstadoOrden(Codigo, Nombre, OrdenVisual, Color) VALUES
 (N'PENDIENTE',   N'Pendiente',      1, N'#f0ad00'),
 (N'PREPARACION', N'En preparación', 2, N'#0dcaf0'),
 (N'LISTO',       N'Listo',          3, N'#198754'),
 (N'ENTREGADO',   N'Entregado',      4, N'#6c757d'),
 (N'CERRADO',     N'Cerrado',        5, N'#212529');

INSERT dbo.EstadoPreorden(Codigo, Nombre) VALUES
 (N'ACTIVA',    N'Activa'),
 (N'CUMPLIDA',  N'Cumplida'),
 (N'CANCELADA', N'Cancelada');

INSERT dbo.MetodoPago(Nombre) VALUES (N'Efectivo'), (N'Tarjeta'), (N'Transferencia');

-- Parámetros del sistema (las claves son identificadores técnicos; los valores son del negocio)
INSERT dbo.Parametro(Clave, Valor, TipoDato, Grupo, Descripcion) VALUES
 (N'MESA.ESTADO_LIBRE',              N'LIBRE',       N'TEXT', N'Mesas',      N'Código del estado de mesa que significa "libre"'),
 (N'MESA.ESTADO_OCUPADA',            N'OCUPADA',     N'TEXT', N'Mesas',      N'Código del estado de mesa que significa "ocupada"'),
 (N'MESA.ESTADO_RESERVADA',          N'RESERVADA',   N'TEXT', N'Mesas',      N'Código del estado de mesa que significa "reservada"'),
 (N'MESA.ESTADO_LIMPIEZA',           N'LIMPIEZA',    N'TEXT', N'Mesas',      N'Código del estado de mesa que significa "en limpieza"'),
 (N'MESA.MINUTOS_VISIBLE_RESERVA',   N'120',         N'INT',  N'Mesas',      N'Minutos antes de la hora de una reserva en que la mesa pasa a "reservada"'),
 (N'ORDEN.ESTADO_INICIAL',           N'PENDIENTE',   N'TEXT', N'Órdenes',    N'Código del estado inicial de un platillo / orden'),
 (N'ORDEN.ESTADO_PREPARACION',       N'PREPARACION', N'TEXT', N'Órdenes',    N'Código del estado "en preparación"'),
 (N'ORDEN.ESTADO_LISTO',             N'LISTO',       N'TEXT', N'Órdenes',    N'Código del estado "listo" (fin de la preparación)'),
 (N'ORDEN.ESTADO_ENTREGADO',         N'ENTREGADO',   N'TEXT', N'Órdenes',    N'Código del estado "entregado"'),
 (N'ORDEN.ESTADO_CERRADO',           N'CERRADO',     N'TEXT', N'Órdenes',    N'Código del estado "cerrado" (cuenta cobrada)'),
 (N'PREORDEN.ESTADO_ACTIVA',         N'ACTIVA',      N'TEXT', N'Reservas',   N'Código del estado de reserva vigente'),
 (N'PREORDEN.ESTADO_CUMPLIDA',       N'CUMPLIDA',    N'TEXT', N'Reservas',   N'Código del estado de reserva ya atendida'),
 (N'PREORDEN.ESTADO_CANCELADA',      N'CANCELADA',   N'TEXT', N'Reservas',   N'Código del estado de reserva cancelada'),
 (N'PREORDEN.TIPO_ORDEN_CONVERSION', N'MESA',        N'TEXT', N'Reservas',   N'Código del tipo de orden que se crea al atender una reserva'),
 (N'PREORDEN.MINUTOS_ANTICIPACION',  N'30',          N'INT',  N'Reservas',   N'Anticipación mínima (minutos) para registrar una reserva'),
 (N'PREORDEN.DURACION_MINUTOS',      N'90',          N'INT',  N'Reservas',   N'Duración estimada de una reserva; evita traslapes en la misma mesa'),
 (N'RESTAURANTE.HORA_APERTURA',      N'08:00',       N'TIME', N'Horario',    N'Hora de apertura (las reservas deben estar dentro del horario)'),
 (N'RESTAURANTE.HORA_CIERRE',        N'22:00',       N'TIME', N'Horario',    N'Hora de cierre'),
 (N'CAJA.REQUIERE_ENTREGA_COMPLETA', N'false',       N'BOOL', N'Caja',       N'Si es verdadero, solo se cobran órdenes con todos los platillos entregados'),
 (N'COMPROBANTE.PREFIJO',            N'FAC',         N'TEXT', N'Caja',       N'Prefijo del número de comprobante'),
 (N'COMPROBANTE.DIGITOS',            N'6',           N'INT',  N'Caja',       N'Cantidad de dígitos del correlativo del comprobante'),
 (N'SEGURIDAD.MAX_INTENTOS',         N'5',           N'INT',  N'Seguridad',  N'Intentos fallidos de inicio de sesión antes de bloquear la cuenta'),
 (N'SEGURIDAD.MINUTOS_BLOQUEO',      N'15',          N'INT',  N'Seguridad',  N'Minutos que permanece bloqueada una cuenta'),
 (N'SEGURIDAD.LONGITUD_MIN_PASSWORD',N'8',           N'INT',  N'Seguridad',  N'Longitud mínima de las contraseñas'),
 (N'UI.REFRESCO_SEGUNDOS',           N'8',           N'INT',  N'Interfaz',   N'Cada cuántos segundos se actualizan el mapa de mesas y la cocina'),
 (N'UI.FILAS_BITACORA',              N'50',          N'INT',  N'Interfaz',   N'Filas por página en la consulta de bitácora'),
 (N'REPORTE.TOP_PLATILLOS',          N'10',          N'INT',  N'Reportes',   N'Cantidad de platillos en el reporte de más vendidos');

-- Datos de ejemplo (editables / eliminables desde la aplicación)
INSERT dbo.Zona(Nombre) VALUES (N'Salón principal'), (N'Terraza');

INSERT dbo.Mesa(Numero, ZonaId, Capacidad, EstadoMesaId)
SELECT v.Numero, z.Id, v.Capacidad, (SELECT Id FROM dbo.EstadoMesa WHERE Codigo = N'LIBRE')
FROM (VALUES (N'M01', N'Salón principal', 4), (N'M02', N'Salón principal', 4),
             (N'M03', N'Salón principal', 6), (N'T01', N'Terraza', 2), (N'T02', N'Terraza', 4)) v(Numero, Zona, Capacidad)
JOIN dbo.Zona z ON z.Nombre = v.Zona;

INSERT dbo.CategoriaProducto(Nombre) VALUES (N'Entradas'), (N'Platos fuertes'), (N'Bebidas'), (N'Postres');

INSERT dbo.Producto(Nombre, Descripcion, CategoriaProductoId, Precio)
SELECT v.Nombre, v.Descripcion, c.Id, v.Precio
FROM (VALUES (N'Nachos',              N'Nachos de la casa',          N'Entradas',       35.00),
             (N'Hamburguesa clásica', N'Carne, queso y vegetales',   N'Platos fuertes', 55.00),
             (N'Pollo a la plancha',  N'Con guarnición',             N'Platos fuertes', 62.00),
             (N'Limonada',            N'Natural',                    N'Bebidas',        18.00),
             (N'Gaseosa',             N'Botella personal',           N'Bebidas',        12.00),
             (N'Pastel de chocolate', N'Porción',                    N'Postres',        28.00)) v(Nombre, Descripcion, Categoria, Precio)
JOIN dbo.CategoriaProducto c ON c.Nombre = v.Categoria;

INSERT dbo.Promocion(Nombre, Descripcion, PrecioCombo, VigenciaDesde, VigenciaHasta)
VALUES (N'Combo hamburguesa + limonada', N'Hamburguesa clásica con limonada natural', 60.00, SYSDATETIME(), DATEADD(YEAR, 1, SYSDATETIME()));

INSERT dbo.PromocionDetalle(PromocionId, ProductoId, Cantidad)
SELECT p.Id, pr.Id, 1
FROM dbo.Promocion p
JOIN dbo.Producto pr ON pr.Nombre IN (N'Hamburguesa clásica', N'Limonada')
WHERE p.Nombre = N'Combo hamburguesa + limonada';
GO

/* ===================== 3. FUNCIONES Y PROCEDIMIENTOS ===================== */
/* Convenciones:
   - Todo procedimiento "público" abre una transacción (SET XACT_ABORT ON + TRY/CATCH),
     registra en la bitácora (dbo.Auditoria) y devuelve UNA fila con la columna Value
     (Id del registro principal afectado).
   - Los errores de negocio se lanzan con THROW (números 50000+) y la aplicación
     muestra el mensaje al usuario.
   - Los procedimientos *_Interno no manejan transacción: los invocan otros procedimientos. */

CREATE OR ALTER FUNCTION dbo.fn_Param(@Clave nvarchar(100)) RETURNS nvarchar(500)
AS BEGIN
    RETURN (SELECT Valor FROM dbo.Parametro WHERE Clave = @Clave);
END;
GO
CREATE OR ALTER FUNCTION dbo.fn_IdEstadoMesa(@ClaveParametro nvarchar(100)) RETURNS int
AS BEGIN
    RETURN (SELECT Id FROM dbo.EstadoMesa WHERE Codigo = dbo.fn_Param(@ClaveParametro));
END;
GO
CREATE OR ALTER FUNCTION dbo.fn_IdEstadoOrden(@ClaveParametro nvarchar(100)) RETURNS int
AS BEGIN
    RETURN (SELECT Id FROM dbo.EstadoOrden WHERE Codigo = dbo.fn_Param(@ClaveParametro));
END;
GO
CREATE OR ALTER FUNCTION dbo.fn_IdEstadoPreorden(@ClaveParametro nvarchar(100)) RETURNS int
AS BEGIN
    RETURN (SELECT Id FROM dbo.EstadoPreorden WHERE Codigo = dbo.fn_Param(@ClaveParametro));
END;
GO
CREATE OR ALTER FUNCTION dbo.fn_NombreUsuario(@UsuarioId int) RETURNS nvarchar(150)
AS BEGIN
    RETURN ISNULL((SELECT Nombre FROM dbo.Usuario WHERE Id = @UsuarioId), N'Sistema');
END;
GO

/* ---------- Bitácora ---------- */
CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_Registrar
    @UsuarioId  int,
    @Accion     nvarchar(10),
    @Tabla      nvarchar(100),
    @RegistroId nvarchar(100),
    @Anteriores nvarchar(max) = NULL,
    @Nuevos     nvarchar(max) = NULL,
    @Detalle    nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Auditoria(UsuarioId, Usuario, Accion, Tabla, RegistroId, ValoresAnteriores, ValoresNuevos, Detalle)
    VALUES (@UsuarioId, dbo.fn_NombreUsuario(@UsuarioId), @Accion, @Tabla, @RegistroId, @Anteriores, @Nuevos, @Detalle);
END;
GO

/* ---------- Mesas ---------- */
CREATE OR ALTER PROCEDURE dbo.sp_Mesa_CambiarEstado
    @MesaId int, @EstadoId int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Actual int, @Numero nvarchar(20);
        SELECT @Actual = EstadoMesaId, @Numero = Numero FROM dbo.Mesa WITH (UPDLOCK) WHERE Id = @MesaId;
        IF @Numero IS NULL THROW 50101, N'La mesa no existe.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.EstadoMesa WHERE Id = @EstadoId AND Activo = 1)
            THROW 50102, N'El estado indicado no existe o está inactivo.', 1;
        IF @EstadoId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIBRE')
           AND EXISTS (SELECT 1 FROM dbo.Orden WHERE MesaId = @MesaId AND Cerrada = 0 AND Anulada = 0)
            THROW 50103, N'No se puede liberar la mesa: tiene una orden abierta.', 1;
        IF @Actual <> @EstadoId
        BEGIN
            UPDATE dbo.Mesa SET EstadoMesaId = @EstadoId WHERE Id = @MesaId;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Estado=', (SELECT Nombre FROM dbo.EstadoMesa WHERE Id = @Actual));
            DECLARE @Nue nvarchar(max) = CONCAT(N'Estado=', (SELECT Nombre FROM dbo.EstadoMesa WHERE Id = @EstadoId));
            DECLARE @Det nvarchar(500) = CONCAT(N'Cambio de estado de la mesa ', @Numero);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Mesa', @MesaId, @Ant, @Nue, @Det;
        END;
        COMMIT;
        SELECT CAST(@MesaId AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Mesa_Guardar
    @Id int = NULL, @Numero nvarchar(20), @ZonaId int, @Capacidad int, @Activa bit = 1, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        SET @Numero = LTRIM(RTRIM(ISNULL(@Numero, N'')));
        IF @Numero = N'' THROW 50111, N'El número de mesa es obligatorio.', 1;
        IF @Capacidad IS NULL OR @Capacidad <= 0 THROW 50112, N'La capacidad debe ser mayor que cero.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Zona WHERE Id = @ZonaId) THROW 50113, N'La zona indicada no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Mesa WHERE Numero = @Numero AND Id <> ISNULL(@Id, 0))
            THROW 50114, N'Ya existe una mesa con ese número.', 1;
        IF @Id IS NULL
        BEGIN
            INSERT dbo.Mesa(Numero, ZonaId, Capacidad, EstadoMesaId, Activa)
            VALUES (@Numero, @ZonaId, @Capacidad, dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIBRE'), @Activa);
            SET @Id = SCOPE_IDENTITY();
            DECLARE @NuevoIns nvarchar(max) = CONCAT(N'Numero=', @Numero, N'; ZonaId=', @ZonaId, N'; Capacidad=', @Capacidad, N'; Activa=', @Activa);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Mesa', @Id, NULL, @NuevoIns, N'Alta de mesa';
        END
        ELSE
        BEGIN
            DECLARE @NumA nvarchar(20), @ZonaA int, @CapA int, @ActA bit;
            SELECT @NumA = Numero, @ZonaA = ZonaId, @CapA = Capacidad, @ActA = Activa FROM dbo.Mesa WITH (UPDLOCK) WHERE Id = @Id;
            IF @NumA IS NULL THROW 50115, N'La mesa no existe.', 1;
            IF @Activa = 0 AND EXISTS (SELECT 1 FROM dbo.Orden WHERE MesaId = @Id AND Cerrada = 0 AND Anulada = 0)
                THROW 50116, N'No se puede desactivar una mesa con orden abierta.', 1;
            UPDATE dbo.Mesa SET Numero = @Numero, ZonaId = @ZonaId, Capacidad = @Capacidad, Activa = @Activa WHERE Id = @Id;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Numero=', @NumA, N'; ZonaId=', @ZonaA, N'; Capacidad=', @CapA, N'; Activa=', @ActA);
            DECLARE @Nue nvarchar(max) = CONCAT(N'Numero=', @Numero, N'; ZonaId=', @ZonaId, N'; Capacidad=', @Capacidad, N'; Activa=', @Activa);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Mesa', @Id, @Ant, @Nue, N'Edición de mesa';
        END;
        COMMIT;
        SELECT CAST(@Id AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Baja de mesa: si tiene historial se desactiva (devuelve 0); si no, se elimina (devuelve 1)
CREATE OR ALTER PROCEDURE dbo.sp_Mesa_Eliminar
    @Id int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Numero nvarchar(20) = (SELECT Numero FROM dbo.Mesa WITH (UPDLOCK) WHERE Id = @Id);
        DECLARE @Eliminada bigint = 0;
        IF @Numero IS NULL THROW 50121, N'La mesa no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Orden WHERE MesaId = @Id AND Cerrada = 0 AND Anulada = 0)
            THROW 50122, N'No se puede dar de baja una mesa con orden abierta.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Orden WHERE MesaId = @Id) OR EXISTS (SELECT 1 FROM dbo.Preorden WHERE MesaId = @Id)
        BEGIN
            UPDATE dbo.Mesa SET Activa = 0 WHERE Id = @Id;
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Mesa', @Id, N'Activa=1', N'Activa=0', N'Baja lógica: la mesa tiene historial';
        END
        ELSE
        BEGIN
            DELETE dbo.Mesa WHERE Id = @Id;
            SET @Eliminada = 1;
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'DELETE', N'Mesa', @Id, @Numero, NULL, N'Baja de mesa';
        END;
        COMMIT;
        SELECT @Eliminada AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Sincroniza el estado de las mesas con las reservas próximas (lo invoca el mapa de mesas)
CREATE OR ALTER PROCEDURE dbo.sp_Mesa_SincronizarReservas
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @Libre int = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIBRE');
    DECLARE @Reservada int = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_RESERVADA');
    DECLARE @Activa int = dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_ACTIVA');
    DECLARE @Ventana int = ISNULL(TRY_CAST(dbo.fn_Param(N'MESA.MINUTOS_VISIBLE_RESERVA') AS int), 0);
    DECLARE @Duracion int = ISNULL(TRY_CAST(dbo.fn_Param(N'PREORDEN.DURACION_MINUTOS') AS int), 0);
    DECLARE @Cambios TABLE(MesaId int, Hacia int);
    BEGIN TRY
        BEGIN TRAN;
        UPDATE m SET EstadoMesaId = @Reservada
        OUTPUT inserted.Id, inserted.EstadoMesaId INTO @Cambios(MesaId, Hacia)
        FROM dbo.Mesa m
        WHERE m.Activa = 1 AND m.EstadoMesaId = @Libre
          AND EXISTS (SELECT 1 FROM dbo.Preorden p
                      WHERE p.MesaId = m.Id AND p.EstadoPreordenId = @Activa
                        AND p.FechaHora <= DATEADD(MINUTE, @Ventana, SYSDATETIME())
                        AND DATEADD(MINUTE, @Duracion, p.FechaHora) >= SYSDATETIME());

        UPDATE m SET EstadoMesaId = @Libre
        OUTPUT inserted.Id, inserted.EstadoMesaId INTO @Cambios(MesaId, Hacia)
        FROM dbo.Mesa m
        WHERE m.EstadoMesaId = @Reservada
          AND NOT EXISTS (SELECT 1 FROM dbo.Preorden p
                          WHERE p.MesaId = m.Id AND p.EstadoPreordenId = @Activa
                            AND p.FechaHora <= DATEADD(MINUTE, @Ventana, SYSDATETIME())
                            AND DATEADD(MINUTE, @Duracion, p.FechaHora) >= SYSDATETIME())
          AND NOT EXISTS (SELECT 1 FROM dbo.Orden o WHERE o.MesaId = m.Id AND o.Cerrada = 0 AND o.Anulada = 0);

        INSERT dbo.Auditoria(UsuarioId, Usuario, Accion, Tabla, RegistroId, ValoresNuevos, Detalle)
        SELECT NULL, N'Sistema', N'UPDATE', N'Mesa', CAST(c.MesaId AS nvarchar(100)),
               CONCAT(N'Estado=', e.Nombre), N'Cambio automático por reservas'
        FROM @Cambios c JOIN dbo.EstadoMesa e ON e.Id = c.Hacia;
        COMMIT;
        SELECT CAST((SELECT COUNT(*) FROM @Cambios) AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Consulta compleja: mapa de mesas con orden abierta y próxima reserva
CREATE OR ALTER PROCEDURE dbo.sp_Mesas_Mapa
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Activa int = dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_ACTIVA');
    DECLARE @Duracion int = ISNULL(TRY_CAST(dbo.fn_Param(N'PREORDEN.DURACION_MINUTOS') AS int), 0);
    SELECT m.Id AS MesaId, m.Numero, z.Nombre AS Zona, m.Capacidad,
           m.EstadoMesaId, e.Nombre AS Estado, e.Codigo AS EstadoCodigo, e.Color,
           o.Id AS OrdenId, o.Total AS OrdenTotal,
           DATEDIFF(MINUTE, o.Fecha, SYSDATETIME()) AS MinutosOcupada,
           r.FechaHora AS ProximaReserva, r.Cliente AS ClienteReserva
    FROM dbo.Mesa m
    JOIN dbo.Zona z ON z.Id = m.ZonaId
    JOIN dbo.EstadoMesa e ON e.Id = m.EstadoMesaId
    OUTER APPLY (SELECT TOP (1) x.Id, x.Total, x.Fecha
                 FROM dbo.Orden x
                 WHERE x.MesaId = m.Id AND x.Cerrada = 0 AND x.Anulada = 0
                 ORDER BY x.Fecha DESC) o
    OUTER APPLY (SELECT TOP (1) p.FechaHora, p.Cliente
                 FROM dbo.Preorden p
                 WHERE p.MesaId = m.Id AND p.EstadoPreordenId = @Activa
                   AND DATEADD(MINUTE, @Duracion, p.FechaHora) >= SYSDATETIME()
                 ORDER BY p.FechaHora) r
    WHERE m.Activa = 1
    ORDER BY z.Nombre, m.Numero;
END;
GO

/* ---------- Órdenes ---------- */
CREATE OR ALTER PROCEDURE dbo.sp_Orden_Recalcular
    @OrdenId bigint
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Orden WHERE Id = @OrdenId AND Cerrada = 0 AND Anulada = 0) RETURN;

    DECLARE @Subtotal decimal(18,2) =
        ISNULL((SELECT SUM(Cantidad * PrecioUnitario) FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId), 0);

    -- Unidades de cada producto todavía disponibles para formar combos
    DECLARE @Disp TABLE(ProductoId int PRIMARY KEY, Cantidad int NOT NULL, Precio decimal(18,4) NOT NULL);
    INSERT @Disp(ProductoId, Cantidad, Precio)
    SELECT ProductoId, SUM(Cantidad), SUM(Cantidad * PrecioUnitario) / SUM(Cantidad)
    FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId GROUP BY ProductoId;

    -- Se aplica primero la promoción que más ahorra; las unidades usadas no se reutilizan
    DECLARE @Aplicadas TABLE(PromocionId int PRIMARY KEY, Veces int NOT NULL, Descuento decimal(18,2) NOT NULL);
    DECLARE @Pid int, @Veces int, @Ahorro decimal(18,2);
    WHILE 1 = 1
    BEGIN
        SET @Pid = NULL;
        SELECT TOP (1) @Pid = x.Id, @Veces = x.Veces, @Ahorro = x.AhorroUnitario
        FROM (SELECT p.Id,
                     MIN(ISNULL(d.Cantidad, 0) / pd.Cantidad) AS Veces,
                     SUM(pd.Cantidad * ISNULL(d.Precio, pr.Precio)) - MAX(p.PrecioCombo) AS AhorroUnitario
              FROM dbo.Promocion p
              JOIN dbo.PromocionDetalle pd ON pd.PromocionId = p.Id
              JOIN dbo.Producto pr ON pr.Id = pd.ProductoId
              LEFT JOIN @Disp d ON d.ProductoId = pd.ProductoId
              WHERE p.Activa = 1 AND SYSDATETIME() BETWEEN p.VigenciaDesde AND p.VigenciaHasta
                AND NOT EXISTS (SELECT 1 FROM @Aplicadas a WHERE a.PromocionId = p.Id)
              GROUP BY p.Id) x
        WHERE x.Veces > 0 AND x.AhorroUnitario > 0
        ORDER BY x.Veces * x.AhorroUnitario DESC, x.Id;

        IF @Pid IS NULL BREAK;

        UPDATE d SET d.Cantidad = d.Cantidad - @Veces * pd.Cantidad
        FROM @Disp d
        JOIN dbo.PromocionDetalle pd ON pd.ProductoId = d.ProductoId AND pd.PromocionId = @Pid;

        INSERT @Aplicadas(PromocionId, Veces, Descuento) VALUES (@Pid, @Veces, @Veces * @Ahorro);
    END;

    DECLARE @Descuento decimal(18,2) = ISNULL((SELECT SUM(Descuento) FROM @Aplicadas), 0);
    DECLARE @Tasa decimal(5,2) = ISNULL((SELECT ImpuestoPorcentaje FROM dbo.Empresa WHERE Id = 1), 0);
    DECLARE @Base decimal(18,2) = @Subtotal - @Descuento;
    DECLARE @Impuesto decimal(18,2) = ROUND(@Base * @Tasa / 100, 2);
    DECLARE @Inicial int = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_INICIAL');

    DELETE dbo.OrdenPromocion WHERE OrdenId = @OrdenId;
    INSERT dbo.OrdenPromocion(OrdenId, PromocionId, Veces, Descuento)
    SELECT @OrdenId, PromocionId, Veces, Descuento FROM @Aplicadas;

    -- El estado de la orden es el del platillo menos avanzado
    UPDATE o SET Subtotal = @Subtotal, Descuento = @Descuento, Impuesto = @Impuesto, Total = @Base + @Impuesto,
                 EstadoOrdenId = ISNULL((SELECT TOP (1) d.EstadoOrdenId
                                         FROM dbo.OrdenDetalle d JOIN dbo.EstadoOrden e ON e.Id = d.EstadoOrdenId
                                         WHERE d.OrdenId = o.Id
                                         ORDER BY e.OrdenVisual, d.Id), @Inicial)
    FROM dbo.Orden o WHERE o.Id = @OrdenId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Orden_CrearInterno
    @TipoOrdenId int, @MesaId int, @UsuarioId int,
    @Cliente nvarchar(150), @Telefono nvarchar(30), @Direccion nvarchar(300),
    @OrdenId bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RequiereMesa bit, @RequiereDireccion bit, @TipoNombre nvarchar(80);
    SELECT @RequiereMesa = RequiereMesa, @RequiereDireccion = RequiereDireccion, @TipoNombre = Nombre
    FROM dbo.TipoOrden WHERE Id = @TipoOrdenId AND Activo = 1;
    IF @TipoNombre IS NULL THROW 50201, N'El tipo de orden no existe o está inactivo.', 1;

    SET @Cliente   = NULLIF(LTRIM(RTRIM(@Cliente)), N'');
    SET @Telefono  = NULLIF(LTRIM(RTRIM(@Telefono)), N'');
    SET @Direccion = NULLIF(LTRIM(RTRIM(@Direccion)), N'');
    IF @RequiereMesa = 0 SET @MesaId = NULL;
    IF @RequiereMesa = 1 AND @MesaId IS NULL THROW 50202, N'Este tipo de orden requiere seleccionar una mesa.', 1;
    IF @RequiereDireccion = 1 AND @Direccion IS NULL THROW 50203, N'Este tipo de orden requiere la dirección de entrega.', 1;

    IF @MesaId IS NOT NULL
    BEGIN
        DECLARE @MesaEstado int, @MesaActiva bit;
        SELECT @MesaEstado = EstadoMesaId, @MesaActiva = Activa FROM dbo.Mesa WITH (UPDLOCK, HOLDLOCK) WHERE Id = @MesaId;
        IF @MesaActiva IS NULL OR @MesaActiva = 0 THROW 50204, N'La mesa no existe o está inactiva.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Orden WHERE MesaId = @MesaId AND Cerrada = 0 AND Anulada = 0)
            THROW 50205, N'La mesa ya tiene una orden abierta.', 1;
        IF @MesaEstado = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIMPIEZA')
            THROW 50206, N'La mesa está en limpieza; márquela como libre antes de abrir una orden.', 1;
    END;

    DECLARE @Inicial int = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_INICIAL');
    IF @Inicial IS NULL THROW 50207, N'Falta configurar el estado inicial de las órdenes.', 1;

    INSERT dbo.Orden(TipoOrdenId, MesaId, EstadoOrdenId, UsuarioId, Cliente, Telefono, DireccionEntrega)
    VALUES (@TipoOrdenId, @MesaId, @Inicial, @UsuarioId, @Cliente, @Telefono, @Direccion);
    SET @OrdenId = SCOPE_IDENTITY();

    IF @MesaId IS NOT NULL
        UPDATE dbo.Mesa SET EstadoMesaId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_OCUPADA') WHERE Id = @MesaId;

    DECLARE @Nuevos nvarchar(max) = CONCAT(N'Tipo=', @TipoNombre, N'; MesaId=', @MesaId, N'; Cliente=', @Cliente);
    DECLARE @Detalle nvarchar(500) = CONCAT(N'Apertura de orden #', @OrdenId);
    EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Orden', @OrdenId, NULL, @Nuevos, @Detalle;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Orden_Crear
    @TipoOrdenId int, @MesaId int = NULL, @UsuarioId int,
    @Cliente nvarchar(150) = NULL, @Telefono nvarchar(30) = NULL, @Direccion nvarchar(300) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Id bigint;
        EXEC dbo.sp_Orden_CrearInterno @TipoOrdenId, @MesaId, @UsuarioId, @Cliente, @Telefono, @Direccion, @Id OUTPUT;
        COMMIT;
        SELECT @Id AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Orden_AgregarItem
    @OrdenId bigint, @ProductoId int, @Cantidad int, @Nota nvarchar(300) = NULL, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Cerrada bit, @Anulada bit;
        SELECT @Cerrada = Cerrada, @Anulada = Anulada FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @OrdenId;
        IF @Cerrada IS NULL THROW 50210, N'La orden no existe.', 1;
        IF @Cerrada = 1 OR @Anulada = 1 THROW 50211, N'La orden ya está cerrada o anulada.', 1;
        IF @Cantidad IS NULL OR @Cantidad <= 0 THROW 50212, N'La cantidad debe ser mayor que cero.', 1;

        DECLARE @Precio decimal(18,2), @Nombre nvarchar(150), @Disponible bit, @Activo bit;
        SELECT @Precio = Precio, @Nombre = Nombre, @Disponible = Disponible, @Activo = Activo
        FROM dbo.Producto WHERE Id = @ProductoId;
        IF @Nombre IS NULL THROW 50213, N'El platillo no existe.', 1;
        IF @Activo = 0 OR @Disponible = 0 THROW 50214, N'El platillo está agotado o inactivo.', 1;

        DECLARE @Inicial int = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_INICIAL');
        SET @Nota = NULLIF(LTRIM(RTRIM(@Nota)), N'');
        INSERT dbo.OrdenDetalle(OrdenId, ProductoId, Cantidad, PrecioUnitario, Nota, EstadoOrdenId)
        VALUES (@OrdenId, @ProductoId, @Cantidad, @Precio, @Nota, @Inicial);
        DECLARE @DetalleId bigint = SCOPE_IDENTITY();
        INSERT dbo.OrdenDetalleEstadoHist(OrdenDetalleId, EstadoOrdenId, UsuarioId) VALUES (@DetalleId, @Inicial, @UsuarioId);

        EXEC dbo.sp_Orden_Recalcular @OrdenId;

        DECLARE @Nuevos nvarchar(max) = CONCAT(N'OrdenId=', @OrdenId, N'; Producto=', @Nombre, N'; Cantidad=', @Cantidad, N'; Precio=', @Precio);
        DECLARE @Detalle nvarchar(500) = CONCAT(N'Platillo agregado a la orden #', @OrdenId);
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'OrdenDetalle', @DetalleId, NULL, @Nuevos, @Detalle;
        COMMIT;
        SELECT @DetalleId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Orden_QuitarItem
    @DetalleId bigint, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @OrdenId bigint, @Estado int, @Producto nvarchar(150), @Cantidad int;
        SELECT @OrdenId = d.OrdenId, @Estado = d.EstadoOrdenId, @Producto = p.Nombre, @Cantidad = d.Cantidad
        FROM dbo.OrdenDetalle d JOIN dbo.Producto p ON p.Id = d.ProductoId WHERE d.Id = @DetalleId;
        IF @OrdenId IS NULL THROW 50220, N'El platillo de la orden no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @OrdenId AND (Cerrada = 1 OR Anulada = 1))
            THROW 50221, N'La orden ya está cerrada o anulada.', 1;
        IF @Estado <> dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_INICIAL')
            THROW 50222, N'Solo se pueden quitar platillos que aún no pasaron a preparación.', 1;

        DELETE dbo.OrdenDetalle WHERE Id = @DetalleId;
        EXEC dbo.sp_Orden_Recalcular @OrdenId;

        DECLARE @Anteriores nvarchar(max) = CONCAT(N'OrdenId=', @OrdenId, N'; Producto=', @Producto, N'; Cantidad=', @Cantidad);
        DECLARE @Detalle nvarchar(500) = CONCAT(N'Platillo quitado de la orden #', @OrdenId);
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'DELETE', N'OrdenDetalle', @DetalleId, @Anteriores, NULL, @Detalle;
        COMMIT;
        SELECT @OrdenId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Orden_CambiarEstadoItem
    @DetalleId bigint, @EstadoId int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @OrdenId bigint, @Actual int, @Producto nvarchar(150);
        SELECT @OrdenId = d.OrdenId, @Actual = d.EstadoOrdenId, @Producto = p.Nombre
        FROM dbo.OrdenDetalle d JOIN dbo.Producto p ON p.Id = d.ProductoId WHERE d.Id = @DetalleId;
        IF @OrdenId IS NULL THROW 50230, N'El platillo de la orden no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @OrdenId AND (Cerrada = 1 OR Anulada = 1))
            THROW 50231, N'La orden ya está cerrada o anulada.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE Id = @EstadoId AND Activo = 1)
            THROW 50232, N'El estado indicado no existe o está inactivo.', 1;
        IF @EstadoId = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_CERRADO')
            THROW 50233, N'El estado de cierre solo se asigna al cobrar la cuenta.', 1;

        IF @Actual <> @EstadoId
        BEGIN
            UPDATE dbo.OrdenDetalle SET EstadoOrdenId = @EstadoId WHERE Id = @DetalleId;
            INSERT dbo.OrdenDetalleEstadoHist(OrdenDetalleId, EstadoOrdenId, UsuarioId) VALUES (@DetalleId, @EstadoId, @UsuarioId);
            EXEC dbo.sp_Orden_Recalcular @OrdenId;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Estado=', (SELECT Nombre FROM dbo.EstadoOrden WHERE Id = @Actual));
            DECLARE @Nue nvarchar(max) = CONCAT(N'Estado=', (SELECT Nombre FROM dbo.EstadoOrden WHERE Id = @EstadoId));
            DECLARE @Det nvarchar(500) = CONCAT(@Producto, N' (orden #', @OrdenId, N')');
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'OrdenDetalle', @DetalleId, @Ant, @Nue, @Det;
        END;
        COMMIT;
        SELECT @DetalleId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Cambia de estado todos los platillos de una orden que aún no estén en ese estado
CREATE OR ALTER PROCEDURE dbo.sp_Orden_CambiarEstado
    @OrdenId bigint, @EstadoId int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS (SELECT 1 FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @OrdenId) THROW 50240, N'La orden no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Orden WHERE Id = @OrdenId AND (Cerrada = 1 OR Anulada = 1))
            THROW 50241, N'La orden ya está cerrada o anulada.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.EstadoOrden WHERE Id = @EstadoId AND Activo = 1)
            THROW 50242, N'El estado indicado no existe o está inactivo.', 1;
        IF @EstadoId = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_CERRADO')
            THROW 50243, N'El estado de cierre solo se asigna al cobrar la cuenta.', 1;

        DECLARE @Cambios TABLE(Id bigint);
        UPDATE dbo.OrdenDetalle SET EstadoOrdenId = @EstadoId
        OUTPUT inserted.Id INTO @Cambios(Id)
        WHERE OrdenId = @OrdenId AND EstadoOrdenId <> @EstadoId;

        INSERT dbo.OrdenDetalleEstadoHist(OrdenDetalleId, EstadoOrdenId, UsuarioId)
        SELECT Id, @EstadoId, @UsuarioId FROM @Cambios;

        IF EXISTS (SELECT 1 FROM @Cambios)
        BEGIN
            EXEC dbo.sp_Orden_Recalcular @OrdenId;
            DECLARE @Nue nvarchar(max) = CONCAT(N'Estado=', (SELECT Nombre FROM dbo.EstadoOrden WHERE Id = @EstadoId));
            DECLARE @Det nvarchar(500) = CONCAT(N'Cambio de estado de toda la orden #', @OrdenId);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Orden', @OrdenId, NULL, @Nue, @Det;
        END;
        COMMIT;
        SELECT @OrdenId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Orden_Anular
    @OrdenId bigint, @Motivo nvarchar(300), @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Cerrada bit, @Anulada bit, @MesaId int;
        SELECT @Cerrada = Cerrada, @Anulada = Anulada, @MesaId = MesaId FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @OrdenId;
        IF @Cerrada IS NULL THROW 50250, N'La orden no existe.', 1;
        IF @Cerrada = 1 OR @Anulada = 1 THROW 50251, N'La orden ya está cerrada o anulada.', 1;
        SET @Motivo = NULLIF(LTRIM(RTRIM(@Motivo)), N'');
        IF @Motivo IS NULL THROW 50252, N'Indique el motivo de la anulación.', 1;

        UPDATE dbo.Orden SET Anulada = 1, MotivoAnulacion = @Motivo WHERE Id = @OrdenId;
        IF @MesaId IS NOT NULL
            UPDATE dbo.Mesa SET EstadoMesaId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIBRE') WHERE Id = @MesaId;

        DECLARE @Nue nvarchar(max) = CONCAT(N'Anulada=1; Motivo=', @Motivo);
        DECLARE @Det nvarchar(500) = CONCAT(N'Anulación de la orden #', @OrdenId);
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Orden', @OrdenId, N'Anulada=0', @Nue, @Det;
        COMMIT;
        SELECT @OrdenId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Cierre de cuenta: recalcula con promociones vigentes, registra el pago, cierra la orden y libera la mesa
CREATE OR ALTER PROCEDURE dbo.sp_Orden_Cerrar
    @OrdenId bigint, @MetodoPagoId int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Cerrada bit, @Anulada bit, @MesaId int;
        SELECT @Cerrada = Cerrada, @Anulada = Anulada, @MesaId = MesaId FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @OrdenId;
        IF @Cerrada IS NULL THROW 50260, N'La orden no existe.', 1;
        IF @Cerrada = 1 OR @Anulada = 1 THROW 50261, N'La orden ya está cerrada o anulada.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.MetodoPago WHERE Id = @MetodoPagoId AND Activo = 1)
            THROW 50262, N'El método de pago no existe o está inactivo.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId)
            THROW 50263, N'La orden no tiene platillos; anúlela en lugar de cobrarla.', 1;
        IF LOWER(ISNULL(dbo.fn_Param(N'CAJA.REQUIERE_ENTREGA_COMPLETA'), N'false')) = N'true'
           AND EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId
                       AND EstadoOrdenId <> dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_ENTREGADO'))
            THROW 50264, N'Todos los platillos deben estar entregados antes de cobrar.', 1;

        EXEC dbo.sp_Orden_Recalcular @OrdenId;
        DECLARE @Total decimal(18,2), @Subtotal decimal(18,2), @Descuento decimal(18,2), @Impuesto decimal(18,2);
        SELECT @Total = Total, @Subtotal = Subtotal, @Descuento = Descuento, @Impuesto = Impuesto FROM dbo.Orden WHERE Id = @OrdenId;

        UPDATE dbo.Orden
        SET Cerrada = 1, FechaCierre = SYSDATETIME(), EstadoOrdenId = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_CERRADO')
        WHERE Id = @OrdenId;

        INSERT dbo.Pago(OrdenId, MetodoPagoId, Monto, UsuarioId) VALUES (@OrdenId, @MetodoPagoId, @Total, @UsuarioId);
        DECLARE @PagoId bigint = SCOPE_IDENTITY();

        IF @MesaId IS NOT NULL
            UPDATE dbo.Mesa SET EstadoMesaId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIMPIEZA') WHERE Id = @MesaId;

        DECLARE @Nue nvarchar(max) = CONCAT(N'Cerrada=1; Subtotal=', @Subtotal, N'; Descuento=', @Descuento, N'; Impuesto=', @Impuesto, N'; Total=', @Total);
        DECLARE @Det nvarchar(500) = CONCAT(N'Cierre de cuenta de la orden #', @OrdenId);
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Orden', @OrdenId, N'Cerrada=0', @Nue, @Det;
        DECLARE @NuevoPago nvarchar(max) = CONCAT(N'OrdenId=', @OrdenId, N'; MetodoPagoId=', @MetodoPagoId, N'; Monto=', @Total);
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Pago', @PagoId, NULL, @NuevoPago, N'Registro de pago';
        COMMIT;
        SELECT @PagoId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO


/* ---------- Preórdenes (reservas con platillos anticipados) ---------- */
-- @DetalleJson: [{"productoId":1,"cantidad":2,"nota":"sin cebolla"}, ...]
CREATE OR ALTER PROCEDURE dbo.sp_Preorden_Crear
    @MesaId int, @Cliente nvarchar(150), @Telefono nvarchar(30) = NULL, @FechaHora datetime2,
    @Notas nvarchar(300) = NULL, @DetalleJson nvarchar(max) = NULL, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        SET @Cliente  = LTRIM(RTRIM(ISNULL(@Cliente, N'')));
        SET @Telefono = NULLIF(LTRIM(RTRIM(@Telefono)), N'');
        SET @Notas    = NULLIF(LTRIM(RTRIM(@Notas)), N'');
        IF @Cliente = N'' THROW 50301, N'El nombre del cliente es obligatorio.', 1;

        DECLARE @MesaEstado int, @MesaActiva bit, @Numero nvarchar(20);
        SELECT @MesaEstado = EstadoMesaId, @MesaActiva = Activa, @Numero = Numero FROM dbo.Mesa WITH (UPDLOCK, HOLDLOCK) WHERE Id = @MesaId;
        IF @MesaActiva IS NULL OR @MesaActiva = 0 THROW 50302, N'La mesa no existe o está inactiva.', 1;

        DECLARE @Anticipacion int = ISNULL(TRY_CAST(dbo.fn_Param(N'PREORDEN.MINUTOS_ANTICIPACION') AS int), 0);
        DECLARE @Duracion int = ISNULL(TRY_CAST(dbo.fn_Param(N'PREORDEN.DURACION_MINUTOS') AS int), 0);
        DECLARE @Ventana int = ISNULL(TRY_CAST(dbo.fn_Param(N'MESA.MINUTOS_VISIBLE_RESERVA') AS int), 0);
        IF @FechaHora < DATEADD(MINUTE, @Anticipacion, SYSDATETIME())
            THROW 50303, N'La reserva debe registrarse con la anticipación mínima configurada.', 1;

        DECLARE @Apertura time = TRY_CAST(dbo.fn_Param(N'RESTAURANTE.HORA_APERTURA') AS time);
        DECLARE @Cierre time = TRY_CAST(dbo.fn_Param(N'RESTAURANTE.HORA_CIERRE') AS time);
        DECLARE @Hora time = CAST(@FechaHora AS time);
        IF @Apertura IS NOT NULL AND @Cierre IS NOT NULL AND (@Hora < @Apertura OR @Hora > @Cierre)
            THROW 50304, N'La hora de la reserva está fuera del horario de atención.', 1;

        DECLARE @Activa int = dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_ACTIVA');
        IF EXISTS (SELECT 1 FROM dbo.Preorden
                   WHERE MesaId = @MesaId AND EstadoPreordenId = @Activa
                     AND ABS(DATEDIFF(MINUTE, FechaHora, @FechaHora)) < @Duracion)
            THROW 50305, N'La mesa ya tiene una reserva en ese horario.', 1;

        IF @DetalleJson IS NOT NULL AND LTRIM(RTRIM(@DetalleJson)) <> N'' AND ISJSON(@DetalleJson) = 0
            THROW 50306, N'El detalle de platillos no es válido.', 1;

        INSERT dbo.Preorden(MesaId, Cliente, Telefono, FechaHora, EstadoPreordenId, Notas, UsuarioId)
        VALUES (@MesaId, @Cliente, @Telefono, @FechaHora, @Activa, @Notas, @UsuarioId);
        DECLARE @Id bigint = SCOPE_IDENTITY();

        IF ISJSON(@DetalleJson) = 1
        BEGIN
            INSERT dbo.PreordenDetalle(PreordenId, ProductoId, Cantidad, Nota)
            SELECT @Id, j.ProductoId, SUM(j.Cantidad), MAX(j.Nota)
            FROM OPENJSON(@DetalleJson)
                 WITH (ProductoId int '$.productoId', Cantidad int '$.cantidad', Nota nvarchar(300) '$.nota') j
            JOIN dbo.Producto p ON p.Id = j.ProductoId AND p.Activo = 1
            WHERE j.Cantidad > 0
            GROUP BY j.ProductoId;
        END;

        IF @MesaEstado = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIBRE') AND @FechaHora <= DATEADD(MINUTE, @Ventana, SYSDATETIME())
            UPDATE dbo.Mesa SET EstadoMesaId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_RESERVADA') WHERE Id = @MesaId;

        DECLARE @Nuevos nvarchar(max) = CONCAT(N'Mesa=', @Numero, N'; Cliente=', @Cliente, N'; FechaHora=', CONVERT(nvarchar(16), @FechaHora, 120),
                                               N'; Platillos=', (SELECT COUNT(*) FROM dbo.PreordenDetalle WHERE PreordenId = @Id));
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Preorden', @Id, NULL, @Nuevos, N'Registro de reserva';
        COMMIT;
        SELECT @Id AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Preorden_Cancelar
    @PreordenId bigint, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @MesaId int, @Estado int;
        SELECT @MesaId = MesaId, @Estado = EstadoPreordenId FROM dbo.Preorden WITH (UPDLOCK) WHERE Id = @PreordenId;
        IF @MesaId IS NULL THROW 50310, N'La reserva no existe.', 1;
        IF @Estado <> dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_ACTIVA') THROW 50311, N'Solo se pueden cancelar reservas activas.', 1;

        UPDATE dbo.Preorden SET EstadoPreordenId = dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_CANCELADA') WHERE Id = @PreordenId;

        -- Si la mesa estaba reservada y ya no hay otra reserva próxima, se libera
        IF EXISTS (SELECT 1 FROM dbo.Mesa WHERE Id = @MesaId AND EstadoMesaId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_RESERVADA'))
           AND NOT EXISTS (SELECT 1 FROM dbo.Preorden p
                           WHERE p.MesaId = @MesaId AND p.EstadoPreordenId = dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_ACTIVA')
                             AND p.FechaHora <= DATEADD(MINUTE, ISNULL(TRY_CAST(dbo.fn_Param(N'MESA.MINUTOS_VISIBLE_RESERVA') AS int), 0), SYSDATETIME()))
            UPDATE dbo.Mesa SET EstadoMesaId = dbo.fn_IdEstadoMesa(N'MESA.ESTADO_LIBRE') WHERE Id = @MesaId;

        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Preorden', @PreordenId, N'Estado=Activa', N'Estado=Cancelada', N'Cancelación de reserva';
        COMMIT;
        SELECT @PreordenId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Atiende una reserva: abre la orden de la mesa con los platillos reservados
CREATE OR ALTER PROCEDURE dbo.sp_Preorden_Convertir
    @PreordenId bigint, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @MesaId int, @Estado int, @Cliente nvarchar(150), @Telefono nvarchar(30);
        SELECT @MesaId = MesaId, @Estado = EstadoPreordenId, @Cliente = Cliente, @Telefono = Telefono
        FROM dbo.Preorden WITH (UPDLOCK) WHERE Id = @PreordenId;
        IF @MesaId IS NULL THROW 50320, N'La reserva no existe.', 1;
        IF @Estado <> dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_ACTIVA') THROW 50321, N'Solo se pueden atender reservas activas.', 1;

        DECLARE @Tipo int = (SELECT Id FROM dbo.TipoOrden WHERE Codigo = dbo.fn_Param(N'PREORDEN.TIPO_ORDEN_CONVERSION'));
        IF @Tipo IS NULL THROW 50322, N'Falta configurar el tipo de orden para atender reservas.', 1;

        DECLARE @OrdenId bigint;
        EXEC dbo.sp_Orden_CrearInterno @Tipo, @MesaId, @UsuarioId, @Cliente, @Telefono, NULL, @OrdenId OUTPUT;

        DECLARE @Inicial int = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_INICIAL');
        INSERT dbo.OrdenDetalle(OrdenId, ProductoId, Cantidad, PrecioUnitario, Nota, EstadoOrdenId)
        SELECT @OrdenId, d.ProductoId, d.Cantidad, p.Precio, d.Nota, @Inicial
        FROM dbo.PreordenDetalle d JOIN dbo.Producto p ON p.Id = d.ProductoId
        WHERE d.PreordenId = @PreordenId AND p.Activo = 1 AND p.Disponible = 1;

        INSERT dbo.OrdenDetalleEstadoHist(OrdenDetalleId, EstadoOrdenId, UsuarioId)
        SELECT Id, @Inicial, @UsuarioId FROM dbo.OrdenDetalle WHERE OrdenId = @OrdenId;

        EXEC dbo.sp_Orden_Recalcular @OrdenId;

        UPDATE dbo.Preorden
        SET EstadoPreordenId = dbo.fn_IdEstadoPreorden(N'PREORDEN.ESTADO_CUMPLIDA'), OrdenId = @OrdenId
        WHERE Id = @PreordenId;

        DECLARE @Nue nvarchar(max) = CONCAT(N'Estado=Cumplida; OrdenId=', @OrdenId);
        EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Preorden', @PreordenId, N'Estado=Activa', @Nue, N'Reserva atendida';
        COMMIT;
        SELECT @OrdenId AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* ---------- Menú ---------- */
CREATE OR ALTER PROCEDURE dbo.sp_Producto_Guardar
    @Id int = NULL, @Nombre nvarchar(150), @Descripcion nvarchar(500) = NULL, @CategoriaId int,
    @Precio decimal(18,2), @Disponible bit = 1, @Activo bit = 1, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        SET @Nombre = LTRIM(RTRIM(ISNULL(@Nombre, N'')));
        SET @Descripcion = NULLIF(LTRIM(RTRIM(@Descripcion)), N'');
        IF @Nombre = N'' THROW 50401, N'El nombre del platillo es obligatorio.', 1;
        IF @Precio IS NULL OR @Precio < 0 THROW 50402, N'El precio no puede ser negativo.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Id = @CategoriaId) THROW 50403, N'La categoría indicada no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Producto WHERE Nombre = @Nombre AND CategoriaProductoId = @CategoriaId AND Id <> ISNULL(@Id, 0))
            THROW 50404, N'Ya existe un platillo con ese nombre en la categoría.', 1;

        IF @Id IS NULL
        BEGIN
            INSERT dbo.Producto(Nombre, Descripcion, CategoriaProductoId, Precio, Disponible, Activo)
            VALUES (@Nombre, @Descripcion, @CategoriaId, @Precio, @Disponible, @Activo);
            SET @Id = SCOPE_IDENTITY();
            DECLARE @NuevoIns nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; CategoriaId=', @CategoriaId, N'; Precio=', @Precio, N'; Disponible=', @Disponible, N'; Activo=', @Activo);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Producto', @Id, NULL, @NuevoIns, N'Alta de platillo';
        END
        ELSE
        BEGIN
            DECLARE @NomA nvarchar(150), @CatA int, @PreA decimal(18,2), @DisA bit, @ActA bit;
            SELECT @NomA = Nombre, @CatA = CategoriaProductoId, @PreA = Precio, @DisA = Disponible, @ActA = Activo
            FROM dbo.Producto WITH (UPDLOCK) WHERE Id = @Id;
            IF @NomA IS NULL THROW 50405, N'El platillo no existe.', 1;
            UPDATE dbo.Producto
            SET Nombre = @Nombre, Descripcion = @Descripcion, CategoriaProductoId = @CategoriaId,
                Precio = @Precio, Disponible = @Disponible, Activo = @Activo
            WHERE Id = @Id;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Nombre=', @NomA, N'; CategoriaId=', @CatA, N'; Precio=', @PreA, N'; Disponible=', @DisA, N'; Activo=', @ActA);
            DECLARE @Nue nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; CategoriaId=', @CategoriaId, N'; Precio=', @Precio, N'; Disponible=', @Disponible, N'; Activo=', @Activo);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Producto', @Id, @Ant, @Nue, N'Edición de platillo';
        END;
        COMMIT;
        SELECT CAST(@Id AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_CambiarDisponibilidad
    @Id int, @Disponible bit, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Actual bit, @Nombre nvarchar(150);
        SELECT @Actual = Disponible, @Nombre = Nombre FROM dbo.Producto WITH (UPDLOCK) WHERE Id = @Id;
        IF @Nombre IS NULL THROW 50410, N'El platillo no existe.', 1;
        IF @Actual <> @Disponible
        BEGIN
            UPDATE dbo.Producto SET Disponible = @Disponible WHERE Id = @Id;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Disponible=', @Actual);
            DECLARE @Nue nvarchar(max) = CONCAT(N'Disponible=', @Disponible);
            DECLARE @Det nvarchar(500) = CONCAT(N'Disponibilidad de ', @Nombre);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Producto', @Id, @Ant, @Nue, @Det;
        END;
        COMMIT;
        SELECT CAST(@Id AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Baja de platillo: con historial se desactiva (devuelve 0); sin historial se elimina (devuelve 1)
CREATE OR ALTER PROCEDURE dbo.sp_Producto_Eliminar
    @Id int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Nombre nvarchar(150) = (SELECT Nombre FROM dbo.Producto WITH (UPDLOCK) WHERE Id = @Id);
        DECLARE @Eliminado bigint = 0;
        IF @Nombre IS NULL THROW 50420, N'El platillo no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE ProductoId = @Id)
           OR EXISTS (SELECT 1 FROM dbo.PreordenDetalle WHERE ProductoId = @Id)
           OR EXISTS (SELECT 1 FROM dbo.PromocionDetalle WHERE ProductoId = @Id)
        BEGIN
            UPDATE dbo.Producto SET Activo = 0 WHERE Id = @Id;
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Producto', @Id, N'Activo=1', N'Activo=0', N'Baja lógica: el platillo tiene historial';
        END
        ELSE
        BEGIN
            DELETE dbo.Producto WHERE Id = @Id;
            SET @Eliminado = 1;
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'DELETE', N'Producto', @Id, @Nombre, NULL, N'Baja de platillo';
        END;
        COMMIT;
        SELECT @Eliminado AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* ---------- Promociones y combos ---------- */
-- @DetalleJson: [{"productoId":1,"cantidad":1}, ...]
CREATE OR ALTER PROCEDURE dbo.sp_Promocion_Guardar
    @Id int = NULL, @Nombre nvarchar(150), @Descripcion nvarchar(300) = NULL, @PrecioCombo decimal(18,2),
    @VigenciaDesde datetime2, @VigenciaHasta datetime2, @Activa bit = 1, @DetalleJson nvarchar(max), @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        SET @Nombre = LTRIM(RTRIM(ISNULL(@Nombre, N'')));
        SET @Descripcion = NULLIF(LTRIM(RTRIM(@Descripcion)), N'');
        IF @Nombre = N'' THROW 50501, N'El nombre de la promoción es obligatorio.', 1;
        IF @PrecioCombo IS NULL OR @PrecioCombo < 0 THROW 50502, N'El precio del combo no puede ser negativo.', 1;
        IF @VigenciaHasta < @VigenciaDesde THROW 50503, N'La vigencia final debe ser posterior a la inicial.', 1;
        IF @DetalleJson IS NULL OR ISJSON(@DetalleJson) = 0 THROW 50504, N'Indique los platillos que componen el combo.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Promocion WHERE Nombre = @Nombre AND Id <> ISNULL(@Id, 0))
            THROW 50505, N'Ya existe una promoción con ese nombre.', 1;

        DECLARE @Componentes TABLE(ProductoId int PRIMARY KEY, Cantidad int NOT NULL);
        INSERT @Componentes(ProductoId, Cantidad)
        SELECT j.ProductoId, SUM(j.Cantidad)
        FROM OPENJSON(@DetalleJson) WITH (ProductoId int '$.productoId', Cantidad int '$.cantidad') j
        JOIN dbo.Producto p ON p.Id = j.ProductoId
        WHERE j.Cantidad > 0
        GROUP BY j.ProductoId;
        IF NOT EXISTS (SELECT 1 FROM @Componentes) THROW 50506, N'El combo debe incluir al menos un platillo.', 1;
        IF (SELECT SUM(Cantidad) FROM @Componentes) < 2 THROW 50507, N'Un combo debe sumar al menos dos unidades.', 1;

        DECLARE @Resumen nvarchar(max) =
            (SELECT STRING_AGG(CONCAT(c.Cantidad, N' x ', p.Nombre), N', ') FROM @Componentes c JOIN dbo.Producto p ON p.Id = c.ProductoId);

        IF @Id IS NULL
        BEGIN
            INSERT dbo.Promocion(Nombre, Descripcion, PrecioCombo, VigenciaDesde, VigenciaHasta, Activa)
            VALUES (@Nombre, @Descripcion, @PrecioCombo, @VigenciaDesde, @VigenciaHasta, @Activa);
            SET @Id = SCOPE_IDENTITY();
            DECLARE @NuevoIns nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; Precio=', @PrecioCombo, N'; Componentes=', @Resumen);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Promocion', @Id, NULL, @NuevoIns, N'Alta de promoción';
        END
        ELSE
        BEGIN
            DECLARE @NomA nvarchar(150), @PreA decimal(18,2);
            SELECT @NomA = Nombre, @PreA = PrecioCombo FROM dbo.Promocion WITH (UPDLOCK) WHERE Id = @Id;
            IF @NomA IS NULL THROW 50508, N'La promoción no existe.', 1;
            DECLARE @ResumenA nvarchar(max) =
                (SELECT STRING_AGG(CONCAT(d.Cantidad, N' x ', p.Nombre), N', ') FROM dbo.PromocionDetalle d JOIN dbo.Producto p ON p.Id = d.ProductoId WHERE d.PromocionId = @Id);
            UPDATE dbo.Promocion
            SET Nombre = @Nombre, Descripcion = @Descripcion, PrecioCombo = @PrecioCombo,
                VigenciaDesde = @VigenciaDesde, VigenciaHasta = @VigenciaHasta, Activa = @Activa
            WHERE Id = @Id;
            DELETE dbo.PromocionDetalle WHERE PromocionId = @Id;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Nombre=', @NomA, N'; Precio=', @PreA, N'; Componentes=', @ResumenA);
            DECLARE @Nue nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; Precio=', @PrecioCombo, N'; Componentes=', @Resumen);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Promocion', @Id, @Ant, @Nue, N'Edición de promoción';
        END;

        INSERT dbo.PromocionDetalle(PromocionId, ProductoId, Cantidad) SELECT @Id, ProductoId, Cantidad FROM @Componentes;
        COMMIT;
        SELECT CAST(@Id AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- Baja de promoción: si ya se aplicó en órdenes se desactiva (devuelve 0); si no, se elimina (devuelve 1)
CREATE OR ALTER PROCEDURE dbo.sp_Promocion_Eliminar
    @Id int, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Nombre nvarchar(150) = (SELECT Nombre FROM dbo.Promocion WITH (UPDLOCK) WHERE Id = @Id);
        DECLARE @Eliminada bigint = 0;
        IF @Nombre IS NULL THROW 50510, N'La promoción no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.OrdenPromocion WHERE PromocionId = @Id)
        BEGIN
            UPDATE dbo.Promocion SET Activa = 0 WHERE Id = @Id;
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Promocion', @Id, N'Activa=1', N'Activa=0', N'Baja lógica: la promoción ya se aplicó en órdenes';
        END
        ELSE
        BEGIN
            DELETE dbo.Promocion WHERE Id = @Id;
            SET @Eliminada = 1;
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'DELETE', N'Promocion', @Id, @Nombre, NULL, N'Baja de promoción';
        END;
        COMMIT;
        SELECT @Eliminada AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* ---------- Usuarios ---------- */
-- La contraseña llega ya cifrada (hash BCrypt generado por la aplicación). NULL = no cambiarla.
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_Guardar
    @Id int = NULL, @Nombre nvarchar(120), @Email nvarchar(150), @PasswordHash nvarchar(200) = NULL,
    @RolId int, @Activo bit = 1, @DebeCambiarPassword bit = 0, @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        SET @Nombre = LTRIM(RTRIM(ISNULL(@Nombre, N'')));
        SET @Email = LOWER(LTRIM(RTRIM(ISNULL(@Email, N''))));
        IF @Nombre = N'' THROW 50601, N'El nombre es obligatorio.', 1;
        IF @Email NOT LIKE N'%_@_%._%' THROW 50602, N'El correo electrónico no es válido.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Id = @RolId AND Activo = 1) THROW 50603, N'El rol indicado no existe o está inactivo.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE Email = @Email AND Id <> ISNULL(@Id, 0))
            THROW 50604, N'Ya existe un usuario con ese correo.', 1;

        IF @Id IS NULL
        BEGIN
            IF @PasswordHash IS NULL THROW 50605, N'La contraseña es obligatoria para un usuario nuevo.', 1;
            INSERT dbo.Usuario(Nombre, Email, PasswordHash, RolId, Activo, DebeCambiarPassword)
            VALUES (@Nombre, @Email, @PasswordHash, @RolId, @Activo, @DebeCambiarPassword);
            SET @Id = SCOPE_IDENTITY();
            DECLARE @NuevoIns nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; Email=', @Email, N'; RolId=', @RolId, N'; Activo=', @Activo);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Usuario', @Id, NULL, @NuevoIns, N'Alta de usuario';
        END
        ELSE
        BEGIN
            DECLARE @NomA nvarchar(120), @EmaA nvarchar(150), @RolA int, @ActA bit;
            SELECT @NomA = Nombre, @EmaA = Email, @RolA = RolId, @ActA = Activo FROM dbo.Usuario WITH (UPDLOCK) WHERE Id = @Id;
            IF @NomA IS NULL THROW 50606, N'El usuario no existe.', 1;

            -- Siempre debe quedar al menos un administrador activo
            IF EXISTS (SELECT 1 FROM dbo.Usuario u JOIN dbo.Rol r ON r.Id = u.RolId WHERE u.Id = @Id AND u.Activo = 1 AND r.EsAdministrador = 1)
               AND (@Activo = 0 OR NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE Id = @RolId AND EsAdministrador = 1))
               AND NOT EXISTS (SELECT 1 FROM dbo.Usuario u JOIN dbo.Rol r ON r.Id = u.RolId WHERE u.Id <> @Id AND u.Activo = 1 AND r.EsAdministrador = 1)
                THROW 50607, N'Debe existir al menos un administrador activo.', 1;

            UPDATE dbo.Usuario
            SET Nombre = @Nombre, Email = @Email, RolId = @RolId, Activo = @Activo,
                PasswordHash = ISNULL(@PasswordHash, PasswordHash),
                DebeCambiarPassword = CASE WHEN @PasswordHash IS NULL THEN DebeCambiarPassword ELSE @DebeCambiarPassword END,
                IntentosFallidos = CASE WHEN @Activo = 1 AND @ActA = 0 THEN 0 ELSE IntentosFallidos END,
                BloqueadoHasta = CASE WHEN @PasswordHash IS NOT NULL THEN NULL ELSE BloqueadoHasta END
            WHERE Id = @Id;
            DECLARE @Ant nvarchar(max) = CONCAT(N'Nombre=', @NomA, N'; Email=', @EmaA, N'; RolId=', @RolA, N'; Activo=', @ActA);
            DECLARE @Nue nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; Email=', @Email, N'; RolId=', @RolId, N'; Activo=', @Activo,
                                                CASE WHEN @PasswordHash IS NULL THEN N'' ELSE N'; PasswordHash=***' END);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Usuario', @Id, @Ant, @Nue, N'Edición de usuario';
        END;
        COMMIT;
        SELECT CAST(@Id AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

/* ---------- Reportes ---------- */
CREATE OR ALTER PROCEDURE dbo.sp_Reporte_VentasPeriodo
    @Desde date, @Hasta date
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(o.FechaCierre AS date) AS Fecha,
           COUNT(*)         AS Ordenes,
           SUM(o.Subtotal)  AS Subtotal,
           SUM(o.Descuento) AS Descuento,
           SUM(o.Impuesto)  AS Impuesto,
           SUM(o.Total)     AS Total
    FROM dbo.Orden o
    WHERE o.Cerrada = 1 AND o.FechaCierre >= @Desde AND o.FechaCierre < DATEADD(DAY, 1, CAST(@Hasta AS datetime2))
    GROUP BY CAST(o.FechaCierre AS date)
    ORDER BY CAST(o.FechaCierre AS date);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_MasVendidos
    @Desde date, @Hasta date, @Top int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top)
           p.Nombre AS Producto, c.Nombre AS Categoria,
           SUM(d.Cantidad) AS Cantidad,
           SUM(d.Cantidad * d.PrecioUnitario) AS Importe
    FROM dbo.OrdenDetalle d
    JOIN dbo.Orden o ON o.Id = d.OrdenId
    JOIN dbo.Producto p ON p.Id = d.ProductoId
    JOIN dbo.CategoriaProducto c ON c.Id = p.CategoriaProductoId
    WHERE o.Cerrada = 1 AND o.FechaCierre >= @Desde AND o.FechaCierre < DATEADD(DAY, 1, CAST(@Hasta AS datetime2))
    GROUP BY p.Id, p.Nombre, c.Nombre
    ORDER BY SUM(d.Cantidad) DESC, p.Nombre;
END;
GO

-- Tiempo desde que el platillo se registra (estado inicial) hasta que queda listo
CREATE OR ALTER PROCEDURE dbo.sp_Reporte_TiempoPreparacion
    @Desde date, @Hasta date
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Inicial int = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_INICIAL');
    DECLARE @Listo int = dbo.fn_IdEstadoOrden(N'ORDEN.ESTADO_LISTO');
    SELECT p.Nombre AS Producto,
           COUNT(*) AS Platillos,
           CAST(AVG(CAST(DATEDIFF(SECOND, hi.Fecha, hl.Fecha) AS decimal(18,2))) / 60.0 AS decimal(18,2)) AS MinutosPromedio,
           CAST(MAX(DATEDIFF(SECOND, hi.Fecha, hl.Fecha)) / 60.0 AS decimal(18,2)) AS MinutosMaximo
    FROM dbo.OrdenDetalle d
    JOIN dbo.Orden o ON o.Id = d.OrdenId
    JOIN dbo.Producto p ON p.Id = d.ProductoId
    CROSS APPLY (SELECT MIN(h.Fecha) AS Fecha FROM dbo.OrdenDetalleEstadoHist h WHERE h.OrdenDetalleId = d.Id AND h.EstadoOrdenId = @Inicial) hi
    CROSS APPLY (SELECT MIN(h.Fecha) AS Fecha FROM dbo.OrdenDetalleEstadoHist h WHERE h.OrdenDetalleId = d.Id AND h.EstadoOrdenId = @Listo) hl
    WHERE o.Anulada = 0 AND hi.Fecha IS NOT NULL AND hl.Fecha IS NOT NULL
      AND hi.Fecha >= @Desde AND hi.Fecha < DATEADD(DAY, 1, CAST(@Hasta AS datetime2))
    GROUP BY p.Id, p.Nombre
    ORDER BY MinutosPromedio DESC, p.Nombre;
END;
GO

PRINT N'Base de datos RestauranteControlDB creada correctamente.';
GO

