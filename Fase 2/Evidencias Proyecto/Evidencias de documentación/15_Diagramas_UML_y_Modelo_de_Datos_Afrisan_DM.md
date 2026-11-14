# Diagramas UML y modelo de datos vigente

Proyecto APT AFRISAN · Responsable: Dante Moya · Corte: 14/11/2026

Los diagramas describen la solución vigente (`AFRISAN_NUEVO`). Están escritos en Mermaid,
que GitHub dibuja al abrir este archivo. Los diagramas de la Fase 1 (vistas 4+1, componentes,
despliegue y MER) están en `Fase 1/Documentación del proyecto/` y corresponden al diseño
inicial, basado en la recarga de equipos.

## 1. Casos de uso

Mermaid no tiene un diagrama de casos de uso propio; se usa un diagrama de flujo con la
misma notación: actores a la izquierda y casos de uso dentro del límite del sistema.

```mermaid
flowchart LR
    T(("Técnico"))
    B(("Bodega"))
    AU(("Autorizador"))
    J(("Jefatura"))
    AD(("Administrador"))
    subgraph SIS["Sistema AFRISAN"]
        UC01(["Consultar la ficha y el historial de un cilindro por QR"])
        UC02(["Registrar cilindros y su pesaje inicial"])
        UC03(["Mantener el catálogo de refrigerantes"])
        UC04(["Crear y autorizar una OT"])
        UC05(["Retirar cilindros de Bodega con peso y foto"])
        UC06(["Registrar el reemplazo o agregado en la sucursal"])
        UC07(["Trabajar sin conexión y sincronizar"])
        UC08(["Recibir y aprobar los cilindros en Bodega"])
        UC09(["Revisar y cerrar la OT"])
        UC10(["Solicitar la corrección de un peso"])
        UC11(["Resolver correcciones e incidencias"])
        UC12(["Definir límites por sucursal y gas"])
        UC13(["Revisar excesos de límite"])
        UC14(["Registrar precios y valorar costos"])
        UC15(["Ver indicadores en Power BI"])
        UC16(["Gestionar usuarios, clientes y sucursales"])
        UC17(["Dar de baja un cilindro"])
    end
    T --- UC01 & UC05 & UC06 & UC07 & UC10
    B --- UC01 & UC02 & UC03 & UC08 & UC10 & UC12
    AU --- UC01 & UC04 & UC09 & UC11
    J --- UC01 & UC03 & UC12 & UC13 & UC15
    AD --- UC01 & UC02 & UC03 & UC14 & UC15 & UC16 & UC17
```

Los permisos de cada caso de uso están en la [especificación de la API](../../../AFRISAN_NUEVO/03_CODIGO/API.md).

## 2. Componentes

```mermaid
flowchart TB
    subgraph WEB["Afrisan.Nuevo.Web · Blazor WebAssembly"]
        PAG["Páginas Razor por rol"]
        SES["SesionService<br/>sesión JWT y reconexión"]
        LOC["TrabajoLocalService<br/>borradores en IndexedDB"]
        CLI["InventarioClient<br/>cliente HTTP"]
        LQR["Lector QR (jsQR)"]
    end
    subgraph CON["Afrisan.Nuevo.Contratos"]
        DTO["DTO compartidos y validaciones"]
    end
    subgraph API["Afrisan.Nuevo.Api · ASP.NET Core 8"]
        EPT["Endpoints con autorización por rol"]
        SEG["Seguridad<br/>JWT, HTTPS y límite de intentos"]
        REP["Repositorios PostgreSQL<br/>PgInventario, PgTrabajo, PgUsuarios,<br/>PgRectificaciones, PgMantenedores, PgBajas,<br/>PgCostos, PgLimites"]
        MEM["Almacenes en memoria<br/>solo en Development"]
        FOT["SupabaseFotos"]
        PBI["Power BI<br/>token, tablas y proyecto PBIP"]
    end
    BD[("PostgreSQL 16<br/>esquema afrisan")]
    ST[("Supabase Storage")]
    PAG --> SES & LOC & CLI & LQR
    CLI --> DTO
    EPT --> DTO
    CLI -- "HTTPS + JSON" --> EPT
    EPT --> SEG & REP & MEM & FOT & PBI
    REP --> BD
    PBI --> BD
    FOT --> ST
```

## 3. Despliegue

El diagrama de despliegue está en la sección 6 del [README](../../../README.md). La API y la
web irán en Microsoft Azure (D-35); la base de datos y las fotos siguen en Supabase.

## 4. Secuencia: retiro de un cilindro desde Bodega

Rutas relativas a `/api/v1/ordenes-trabajo`.

```mermaid
sequenceDiagram
    actor T as Técnico
    participant W as Web (Blazor)
    participant A as API
    participant S as Storage privado
    participant D as PostgreSQL
    T->>W: Escanea el QR, ingresa el peso y toma la foto
    W->>A: POST /{id}/entregas (JWT)
    A->>A: Valida el rol, la OT asignada, la foto y el peso (tara y capacidad)
    alt La validación falla
        A-->>W: 409 con el motivo, sin subir la foto
    else La validación pasa
        A->>S: Guarda la foto y calcula su hash SHA-256
        A->>D: Transacción: pesaje, evidencia y retiro
        D->>D: Los disparadores validan el estado y registran el historial
        D-->>A: Registro aceptado
        A-->>W: 200 con la OT actualizada
        W-->>T: El cilindro queda en custodia del técnico
    end
```

## 5. Secuencia: de la operación en terreno al cierre de la OT

```mermaid
sequenceDiagram
    actor T as Técnico
    actor B as Bodega
    actor AU as Autorizador
    participant A as API
    participant D as PostgreSQL
    T->>A: PUT /{id}/operaciones y POST /{id}/operaciones/{op}/fotos
    T->>A: POST /{id}/operaciones/{op}/confirmar
    T->>A: POST /{id}/termino-tecnico
    A->>D: La OT pasa a revisión de Bodega
    B->>A: POST /{id}/recepciones (peso recibido independiente)
    B->>A: POST /{id}/aprobar-bodega
    A->>D: La OT pasa al Autorizador
    AU->>A: POST /{id}/revision-autorizador
    AU->>A: POST /{id}/cerrar
    A->>D: Cierra la OT, y la base impide cambios fuera de orden
```

## 6. Modelo entidad-relación vigente

El modelo tiene 32 tablas en el esquema `afrisan` (PostgreSQL 16 en Supabase). Los diagramas
muestran solo las claves primarias (PK) y foráneas (FK); las columnas, los tipos, las reglas
CHECK, los índices y los disparadores están en el
[diccionario de datos](../../../AFRISAN_NUEVO/02_BASE_DATOS/DICCIONARIO_ESQUEMA_AFRISAN.md).
Las relaciones se dibujan como uno a muchos; `|o` indica que la clave foránea admite nulos.
Los diagramas se generaron desde el diccionario el 14/11/2026.

### 6.1 Operación (20 tablas)

```mermaid
erDiagram
    Rol {
        smallint Id_Rol PK
    }
    Usuario {
        integer Id_Usuario PK
        smallint Id_Rol FK
    }
    Cliente {
        integer Id_Cliente PK
    }
    Sucursales {
        integer Id_Sucursal PK
        integer Id_Cliente FK
    }
    Refrigerantes {
        bigint Id_Refrigerante PK
    }
    Cilindros {
        bigint Id_Cilindro PK
        bigint Id_Refrigerante FK
        integer Id_Sucursal FK
        integer Id_Tecnico FK
        uuid Id_UltimoPesaje FK
        integer Id_Registrador FK
    }
    OrdenTrabajo {
        bigint Id_OrdenTrabajo PK
        integer Id_Sucursal FK
        integer Id_Tecnico FK
        integer Id_Autorizador FK
    }
    DetalleOT {
        bigint Id_OrdenTrabajo PK,FK
        bigint Id_Refrigerante PK,FK
    }
    RetiroPrevisto {
        bigint Id_OrdenTrabajo PK,FK
        bigint Id_Cilindro PK,FK
    }
    Pesaje {
        uuid Id_Pesaje PK
        bigint Id_Cilindro FK
        bigint Id_OrdenTrabajo FK
        integer Id_Usuario FK
    }
    EvidenciaPesaje {
        uuid Id_Evidencia PK
        uuid Id_Pesaje FK
        integer Id_Usuario FK
    }
    Despacho {
        uuid Id_Despacho PK
        bigint Id_OrdenTrabajo FK
        bigint Id_Cilindro FK
        uuid Id_Pesaje FK
        integer Id_Tecnico FK
    }
    OperacionTerreno {
        uuid Id_Operacion PK
        bigint Id_OrdenTrabajo FK
        bigint Id_Instalado FK
        bigint Id_Retirado FK
        uuid Id_Despacho FK
        uuid Id_PesajeInstalado FK
        uuid Id_PesajeRetirado FK
        integer Id_Tecnico FK
        integer Id_EntregaInformadaPor FK
    }
    CustodiaActual {
        bigint Id_Cilindro PK,FK
        bigint Id_OrdenTrabajo FK
        integer Id_Tecnico FK
        uuid Id_Despacho FK
        uuid Id_Operacion FK
    }
    RecepcionBodega {
        uuid Id_Recepcion PK
        bigint Id_OrdenTrabajo FK
        bigint Id_Cilindro FK
        uuid Id_Operacion FK
        uuid Id_DespachoSinUso FK
        uuid Id_Pesaje FK
        integer Id_Bodeguero FK
    }
    RevisionOT {
        uuid Id_Revision PK
        bigint Id_OrdenTrabajo FK
        integer Id_Usuario FK
    }
    Incidencia {
        uuid Id_Incidencia PK
        bigint Id_OrdenTrabajo FK
        uuid Id_Operacion FK
        integer Id_Usuario FK
        integer Id_Revisor FK
    }
    HistorialCilindro {
        bigint Id_Historial PK
        bigint Id_Cilindro FK
        integer Id_Sucursal FK
        integer Id_Tecnico FK
        uuid Id_Pesaje FK
        integer Id_Usuario FK
    }
    EventoOT {
        bigint Id_Evento PK
        bigint Id_OrdenTrabajo FK
        integer Id_Usuario FK
    }
    RectificacionPesaje {
        uuid Id PK
        bigint Id_OrdenTrabajo FK
        uuid Id_Pesaje FK
        integer Id_Solicitante FK
        integer Id_Revisor FK
    }
    Rol ||--o{ Usuario : "Id_Rol"
    Cliente ||--o{ Sucursales : "Id_Cliente"
    Refrigerantes ||--o{ Cilindros : "Id_Refrigerante"
    Sucursales |o--o{ Cilindros : "Id_Sucursal"
    Usuario |o--o{ Cilindros : "Id_Tecnico"
    Pesaje |o--o{ Cilindros : "Id_UltimoPesaje"
    Usuario ||--o{ Cilindros : "Id_Registrador"
    Sucursales ||--o{ OrdenTrabajo : "Id_Sucursal"
    Usuario ||--o{ OrdenTrabajo : "Id_Tecnico"
    Usuario ||--o{ OrdenTrabajo : "Id_Autorizador"
    OrdenTrabajo ||--o{ DetalleOT : "Id_OrdenTrabajo"
    Refrigerantes ||--o{ DetalleOT : "Id_Refrigerante"
    OrdenTrabajo ||--o{ RetiroPrevisto : "Id_OrdenTrabajo"
    Cilindros ||--o{ RetiroPrevisto : "Id_Cilindro"
    Cilindros ||--o{ Pesaje : "Id_Cilindro"
    OrdenTrabajo |o--o{ Pesaje : "Id_OrdenTrabajo"
    Usuario ||--o{ Pesaje : "Id_Usuario"
    Pesaje ||--o{ EvidenciaPesaje : "Id_Pesaje"
    Usuario ||--o{ EvidenciaPesaje : "Id_Usuario"
    OrdenTrabajo ||--o{ Despacho : "Id_OrdenTrabajo"
    Cilindros ||--o{ Despacho : "Id_Cilindro"
    Pesaje ||--o{ Despacho : "Id_Pesaje"
    Usuario ||--o{ Despacho : "Id_Tecnico"
    OrdenTrabajo ||--o{ OperacionTerreno : "Id_OrdenTrabajo"
    Cilindros |o--o{ OperacionTerreno : "Id_Instalado"
    Cilindros |o--o{ OperacionTerreno : "Id_Retirado"
    Despacho |o--o{ OperacionTerreno : "Id_Despacho"
    Pesaje |o--o{ OperacionTerreno : "Id_PesajeInstalado"
    Pesaje |o--o{ OperacionTerreno : "Id_PesajeRetirado"
    Usuario ||--o{ OperacionTerreno : "Id_Tecnico"
    Usuario |o--o{ OperacionTerreno : "Id_EntregaInformadaPor"
    Cilindros ||--o{ CustodiaActual : "Id_Cilindro"
    OrdenTrabajo ||--o{ CustodiaActual : "Id_OrdenTrabajo"
    Usuario ||--o{ CustodiaActual : "Id_Tecnico"
    Despacho |o--o{ CustodiaActual : "Id_Despacho"
    OperacionTerreno |o--o{ CustodiaActual : "Id_Operacion"
    OrdenTrabajo ||--o{ RecepcionBodega : "Id_OrdenTrabajo"
    Cilindros ||--o{ RecepcionBodega : "Id_Cilindro"
    OperacionTerreno |o--o{ RecepcionBodega : "Id_Operacion"
    Despacho |o--o{ RecepcionBodega : "Id_DespachoSinUso"
    Pesaje ||--o{ RecepcionBodega : "Id_Pesaje"
    Usuario ||--o{ RecepcionBodega : "Id_Bodeguero"
    OrdenTrabajo ||--o{ RevisionOT : "Id_OrdenTrabajo"
    Usuario ||--o{ RevisionOT : "Id_Usuario"
    OrdenTrabajo ||--o{ Incidencia : "Id_OrdenTrabajo"
    OperacionTerreno |o--o{ Incidencia : "Id_Operacion"
    Usuario ||--o{ Incidencia : "Id_Usuario"
    Usuario |o--o{ Incidencia : "Id_Revisor"
    Cilindros ||--o{ HistorialCilindro : "Id_Cilindro"
    Sucursales |o--o{ HistorialCilindro : "Id_Sucursal"
    Usuario |o--o{ HistorialCilindro : "Id_Tecnico"
    Pesaje |o--o{ HistorialCilindro : "Id_Pesaje"
    Usuario ||--o{ HistorialCilindro : "Id_Usuario"
    OrdenTrabajo ||--o{ EventoOT : "Id_OrdenTrabajo"
    Usuario ||--o{ EventoOT : "Id_Usuario"
    OrdenTrabajo ||--o{ RectificacionPesaje : "Id_OrdenTrabajo"
    Pesaje ||--o{ RectificacionPesaje : "Id_Pesaje"
    Usuario ||--o{ RectificacionPesaje : "Id_Solicitante"
    Usuario |o--o{ RectificacionPesaje : "Id_Revisor"
```

### 6.2 Soporte, auditoría y bajas (12 tablas)

Las tablas de la operación aparecen solo con el nombre cuando se relacionan con estas.

```mermaid
erDiagram
    LimiteSucursal {
        bigint Id_Limite PK
        integer Id_Sucursal FK
        integer Id_UltimoEditor FK
        bigint Id_Refrigerante FK
    }
    LimiteSucursalHistorial {
        bigint Id_HistorialLimite PK
        bigint Id_Limite FK
        integer Id_Sucursal FK
        integer Id_Actor FK
        bigint Id_Refrigerante FK
    }
    MedicionLimite {
        bigint Id_Medicion PK
        bigint Id_HistorialLimite FK
        integer Id_Registrador FK
    }
    MedicionLimiteOperaciones {
        bigint Id_Medicion PK,FK
        uuid Id_Operacion PK,FK
    }
    RevisionExceso {
        bigint Id_RevisionExceso PK
        bigint Id_Medicion FK
        integer Id_Revisor FK
    }
    PreciosRefrigerantes {
        bigint Id_Precio PK
        bigint Id_Refrigerante FK
        integer Id_Registrador FK
    }
    TiposCambio {
        bigint Id_Cambio PK
        integer Id_Registrador FK
    }
    Costos {
        bigint Id_Costos PK
        uuid Id_Operacion FK
        bigint Id_Precio FK
        integer Id_Registrador FK
        bigint Id_Cambio FK
    }
    CertificadosDisposicion {
        bigint Id_Certificado PK
        integer Id_UsuarioRegistro FK
        integer Id_UsuarioCertifica FK
    }
    CertificadosCilindros {
        bigint Id_Certificado PK,FK
        bigint Id_Cilindro PK,FK
    }
    Auditoria {
        bigint Id_Auditoria PK
        integer Id_Usuario FK
    }
    BajaCilindro {
        uuid Id_Baja PK
        bigint Id_Cilindro FK
        integer Id_Usuario FK
    }
    Sucursales ||--o{ LimiteSucursal : "Id_Sucursal"
    Usuario ||--o{ LimiteSucursal : "Id_UltimoEditor"
    Refrigerantes |o--o{ LimiteSucursal : "Id_Refrigerante"
    LimiteSucursal ||--o{ LimiteSucursalHistorial : "Id_Limite"
    Sucursales ||--o{ LimiteSucursalHistorial : "Id_Sucursal"
    Usuario ||--o{ LimiteSucursalHistorial : "Id_Actor"
    Refrigerantes |o--o{ LimiteSucursalHistorial : "Id_Refrigerante"
    LimiteSucursalHistorial ||--o{ MedicionLimite : "Id_HistorialLimite"
    Usuario ||--o{ MedicionLimite : "Id_Registrador"
    MedicionLimite ||--o{ MedicionLimiteOperaciones : "Id_Medicion"
    OperacionTerreno ||--o{ MedicionLimiteOperaciones : "Id_Operacion"
    MedicionLimite ||--o{ RevisionExceso : "Id_Medicion"
    Usuario ||--o{ RevisionExceso : "Id_Revisor"
    Refrigerantes ||--o{ PreciosRefrigerantes : "Id_Refrigerante"
    Usuario ||--o{ PreciosRefrigerantes : "Id_Registrador"
    Usuario ||--o{ TiposCambio : "Id_Registrador"
    OperacionTerreno ||--o{ Costos : "Id_Operacion"
    PreciosRefrigerantes |o--o{ Costos : "Id_Precio"
    Usuario ||--o{ Costos : "Id_Registrador"
    TiposCambio |o--o{ Costos : "Id_Cambio"
    Usuario ||--o{ CertificadosDisposicion : "Id_UsuarioRegistro"
    Usuario |o--o{ CertificadosDisposicion : "Id_UsuarioCertifica"
    CertificadosDisposicion ||--o{ CertificadosCilindros : "Id_Certificado"
    Cilindros ||--o{ CertificadosCilindros : "Id_Cilindro"
    Usuario |o--o{ Auditoria : "Id_Usuario"
    Cilindros ||--o{ BajaCilindro : "Id_Cilindro"
    Usuario ||--o{ BajaCilindro : "Id_Usuario"
```
