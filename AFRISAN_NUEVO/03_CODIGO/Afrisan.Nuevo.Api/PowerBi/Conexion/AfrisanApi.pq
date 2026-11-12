// Lee las tablas de Power BI directamente desde la API de AFRISAN (token de solo lectura, 30 días).
// Parámetros: UrlApi (dirección base, termina en "/") y TokenPowerBi. La dirección base va sin
// transformar en Web.Contents y cada tabla en RelativePath: así Power BI la trata como un origen fijo
// y puede programar la actualización. La API responde los nombres en camelCase; aquí se
// devuelven con los mismos nombres y tipos que usa el modelo (CilindroId, OtId, etc.).
let
    Camel = (n as text) as text => Text.Lower(Text.Start(n, 1)) & Text.Range(n, 1),
    Convertir = (v as any, t as type) as any =>
        if v = null then null
        else if Type.Is(t, type date) then Date.FromText(Text.Start(Text.From(v), 10))
        else if Type.Is(t, type datetime) then DateTimeZone.RemoveZone(DateTimeZone.ToUtc(DateTimeZone.FromText(Text.From(v))))
        else if Type.Is(t, type number) then Number.From(v)
        else if Type.Is(t, type logical) then v
        else Text.From(v),
    Leer = (Ruta as text, Tipos as list) as table =>
        let
            Datos = Json.Document(Web.Contents(UrlApi, [
                RelativePath = Ruta,
                Headers = [Authorization = "Bearer " & TokenPowerBi, Accept = "application/json"]
            ])),
            Nombres = List.Transform(Tipos, each _{0}),
            Filas = List.Transform(Datos, (r) => List.Transform(Tipos, (c) =>
                Convertir(Record.FieldOrDefault(r, Camel(c{0}), Record.FieldOrDefault(r, c{0}, null)), c{1}))),
            Tabla = #table(Nombres, Filas)
        in
            Table.TransformColumnTypes(Tabla, Tipos, "en-US"),
    Inventario = Leer("inventario", {
        {"CilindroId",type text},{"Codigo",type text},{"RefrigeranteId",type text},
        {"Refrigerante",type text},{"Estado",type text},{"Ubicacion",type text},
        {"SucursalId",type text},{"Sucursal",type text},{"TecnicoId",type text},{"Tecnico",type text},
        {"KgSegunUltimoPesaje",type number},{"FechaPesaje",type datetime}}),
    Ordenes = Leer("ordenes", {
        {"OtId",type text},{"Codigo",type text},{"SucursalId",type text},{"Sucursal",type text},
        {"TecnicoId",type text},{"Tecnico",type text},{"Estado",type text},
        {"FechaCreacion",type datetime},{"DiaCreacion",type date},{"FechaCierre",type datetime}}),
    DetalleOT = Leer("detalle", {
        {"OtId",type text},{"RefrigeranteId",type text},{"Refrigerante",type text},
        {"KgSolicitados",type number},{"KgAutorizados",type number}}),
    Operaciones = Leer("operaciones", {
        {"OperacionId",type text},{"OtId",type text},{"Tipo",type text},
        {"RefrigeranteId",type text},{"Refrigerante",type text},
        {"CilindroInstalado",type text},{"CilindroRetirado",type text},
        {"FechaConfirmacion",type datetime},{"DiaConfirmacion",type date},
        {"KgInstalados",type number},{"KgRetirados",type number},
        {"FechaRecepcion",type datetime},{"KgRecepcion",type number},{"PendienteRecepcion",type logical}}),
    Despachos = Leer("despachos", {
        {"DespachoId",type text},{"OtId",type text},{"Cilindro",type text},
        {"RefrigeranteId",type text},{"Refrigerante",type text},
        {"Fecha",type datetime},{"Dia",type date},{"KgDespachados",type number},
        {"Estado",type text},{"FechaDevolucion",type datetime},{"KgDevueltos",type number}}),
    Incidencias = Leer("incidencias", {
        {"IncidenciaId",type text},{"OtId",type text},{"Tipo",type text},
        {"Fecha",type datetime},{"Dia",type date},{"Abierta",type logical},{"FechaRevision",type datetime}}),
    Costos = Leer("costos", {
        {"CostoId",type text},{"OperacionId",type text},{"OtId",type text},{"RefrigeranteId",type text},{"Refrigerante",type text},
        {"Gramos",type number},{"PrecioPorGramo",type number},{"Moneda",type text},{"Total",type number},
        {"FactorClp",type number},{"TotalClp",type number},{"FechaValoracion",type datetime},{"DiaValoracion",type date},{"FechaCambio",type date}})
in
    [Inventario=Inventario, Ordenes=Ordenes, DetalleOT=DetalleOT,
     Operaciones=Operaciones, Despachos=Despachos, Incidencias=Incidencias, Costos=Costos]
