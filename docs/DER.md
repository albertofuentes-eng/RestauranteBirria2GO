# Modelo de datos (3FN)

```mermaid
erDiagram
  Moneda ||--o{ Empresa : "moneda"
  Rol ||--o{ Usuario : tiene
  Rol ||--o{ RolPermiso : otorga
  Modulo ||--o{ RolPermiso : "se asigna"
  Zona ||--o{ Mesa : agrupa
  EstadoMesa ||--o{ Mesa : "estado"
  CategoriaProducto ||--o{ Producto : clasifica
  TipoOrden ||--o{ Orden : "tipo"
  EstadoOrden ||--o{ Orden : "estado"
  Mesa ||--o{ Orden : "atiende"
  Usuario ||--o{ Orden : registra
  Orden ||--|{ OrdenDetalle : contiene
  Producto ||--o{ OrdenDetalle : "se vende"
  OrdenDetalle ||--o{ OrdenDetalleEstadoHist : "historial"
  Orden ||--o{ OrdenPromocion : aplica
  Promocion ||--o{ OrdenPromocion : "se aplica"
  Promocion ||--|{ PromocionDetalle : compone
  Producto ||--o{ PromocionDetalle : "forma parte"
  Mesa ||--o{ Preorden : reserva
  EstadoPreorden ||--o{ Preorden : "estado"
  Preorden ||--o{ PreordenDetalle : "platillos"
  Producto ||--o{ PreordenDetalle : "elige"
  Orden ||--o| Pago : "se paga con"
  MetodoPago ||--o{ Pago : "método"
```
Tablas independientes: `Empresa` (una fila), `Parametro`, `Auditoria`.
Normalización: los atributos descriptivos viven en su catálogo (símbolo de moneda en `Moneda`, estados y métodos de pago en tablas propias); los totales de `Orden` son un resultado congelado al momento de la cuenta y los precios de `OrdenDetalle` conservan el precio vendido.
