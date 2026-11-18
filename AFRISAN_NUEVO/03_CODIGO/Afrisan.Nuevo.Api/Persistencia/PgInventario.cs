using Afrisan.Nuevo.Contratos;
namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgInventario(PgDatabase database,FotosPrivadas fotos)
{
    internal const string CilindrosSql = """
        SELECT c."Id_Cilindro" AS "Id",c."CilindroQR" AS "Codigo",c."NumeroSerie",c."Id_Refrigerante" AS "RefrigeranteId",
          r."CodigoRefrigerante" AS "RefrigeranteCodigo",r."NombreRefrigerante" AS "RefrigeranteNombre",
          c."Capacidadgr" AS "CapacidadGr",c."TaraGr",c."Estado",c."FechaRegistro",c."Ubicacion",c."Id_Sucursal" AS "SucursalId",
          s."NombreSucursal" AS "Sucursal",u."Correo_Usuario" AS "Tecnico",c."Id_UltimoPesaje" AS "UltimoPesajeId"
        FROM afrisan."Cilindros" c JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
        LEFT JOIN afrisan."Sucursales" s USING("Id_Sucursal") LEFT JOIN afrisan."Usuario" u ON u."Id_Usuario"=c."Id_Tecnico"
        """;
    internal const string PesajesSql = """
        SELECT p."Id_Pesaje" AS "Id",p."Id_Cilindro" AS "CilindroId",p."Tipo",p."PesoBrutoGr" AS "Peso",
          p."Fecha",u."Correo_Usuario" AS "Usuario",p."Observacion"
        FROM afrisan."PesajeVigente" p JOIN afrisan."Usuario" u USING("Id_Usuario")
        """;
    internal static async Task<CilindroResponse[]> Map(PgUnit unit, CilindroRow[] rows)
    {
        if(rows.Length==0) return [];
        var pesajes=await unit.Query<PesajeRow>(PesajesSql+" WHERE p.\"Id_Cilindro\"=ANY(@p0)",rows.Select(c=>c.Id).ToArray());
        return rows.Select(c=>{
            PesajeResponse? Peso(PesajeRow? p)=>p is null?null:new(p.Peso,p.Fecha,p.Usuario,p.Observacion);
            return new CilindroResponse(c.Id,c.Codigo,c.NumeroSerie,c.RefrigeranteId,c.RefrigeranteCodigo,c.RefrigeranteNombre,
                c.CapacidadGr,c.TaraGr,c.Estado,c.FechaRegistro,Peso(pesajes.FirstOrDefault(p=>p.CilindroId==c.Id&&p.Tipo=="Inicial")),
                c.Ubicacion,c.Sucursal,c.Tecnico,Peso(pesajes.FirstOrDefault(p=>p.Id==c.UltimoPesajeId)),c.SucursalId);
        }).ToArray();
    }
    public async Task<RefrigeranteResponse[]> Refrigerantes()
    {
        await using var u=await database.Open();
        return await u.Query<RefrigeranteResponse>("SELECT \"Id_Refrigerante\" AS \"Id\",\"CodigoRefrigerante\" AS \"Codigo\",\"NombreRefrigerante\" AS \"Nombre\" FROM afrisan.\"Refrigerantes\" WHERE \"Activo\" ORDER BY \"CodigoRefrigerante\"");
    }
    public async Task<RefrigeranteResponse> CrearRefrigerante(CrearRefrigeranteRequest dto,int actor)
    {
        if(string.IsNullOrWhiteSpace(dto.Codigo)||string.IsNullOrWhiteSpace(dto.Nombre)) throw new ReglaInventarioException("Código y nombre son obligatorios.");
        await using var u=await database.Open(actor);
        var row=await u.One<RefrigeranteResponse>("""
            INSERT INTO afrisan."Refrigerantes"("CodigoRefrigerante","NombreRefrigerante") VALUES(@p0,@p1)
            RETURNING "Id_Refrigerante" AS "Id","CodigoRefrigerante" AS "Codigo","NombreRefrigerante" AS "Nombre"
            """,dto.Codigo.Trim().ToUpperInvariant(),dto.Nombre.Trim());
        await u.Commit(); return row;
    }
    public async Task<CilindroResponse[]> Cilindros()
    {
        await using var u=await database.Open(); return await Map(u,await u.Query<CilindroRow>(CilindrosSql+" ORDER BY c.\"Id_Cilindro\" DESC"));
    }
    public async Task<CilindroResponse> Cilindro(string codigo)
    {
        await using var u=await database.Open();
        return (await Map(u,await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0",codigo.Trim().ToUpperInvariant()))).SingleOrDefault()
            ?? throw new ReglaInventarioException("No se encontró el cilindro.",404);
    }
    // Consulta de la ficha: primero por código y, si no existe, por número de serie (único sin distinguir mayúsculas).
    public async Task<CilindroResponse> Buscar(string texto)
    {
        var buscado=texto.Trim().ToUpperInvariant();
        await using var u=await database.Open();
        var rows=await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0",buscado);
        if(rows.Length==0) rows=await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(btrim(c.\"NumeroSerie\"))=@p0",buscado);
        return (await Map(u,rows)).SingleOrDefault() ?? throw new ReglaInventarioException("No se encontró un cilindro con ese código o número de serie.",404);
    }
    public async Task<CilindroResponse> CrearCilindro(CrearCilindroRequest dto,int actor,string correo)
    {
        if(dto.UbicacionInicial is not("Bodega" or "Sucursal")) throw new ReglaInventarioException("Ubicación inicial inválida.");
        if(dto.UbicacionInicial=="Sucursal"&&dto.SucursalId is null) throw new ReglaInventarioException("Selecciona una sucursal registrada.");
        if(dto.PesoInicialGr is not null&&dto.Foto is null) throw new ReglaInventarioException("Adjunta la foto de la balanza con el peso inicial.");
        await using var u=await database.Open(actor);
        // El código corto (AF-00001) lo asigna la base con su secuencia (D-47).
        var id=await u.One<long>("""
            INSERT INTO afrisan."Cilindros"("NumeroSerie","Id_Refrigerante","Capacidadgr","TaraGr","Ubicacion","Id_Sucursal","Id_Registrador")
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6) RETURNING "Id_Cilindro" AS "Value"
            """,Null(dto.NumeroSerie),dto.RefrigeranteId,dto.CapacidadGr,dto.TaraGr,dto.UbicacionInicial,dto.UbicacionInicial=="Sucursal"?dto.SucursalId:null,actor);
        if(dto.PesoInicialGr is { } peso) await PesoInicialConFoto(u,id,peso,actor,correo,dto.Foto!,Null(dto.Observacion));
        var result=(await Map(u,await u.Query<CilindroRow>(CilindrosSql+" WHERE c.\"Id_Cilindro\"=@p0",id))).Single();
        await u.Commit(); return result;
    }
    public async Task<CilindroResponse> Pesar(string codigo,RegistrarPesajeInicialRequest dto,int actor,string correo)
    {
        if(dto.Foto is null) throw new ReglaInventarioException("Adjunta la foto de la balanza con el peso inicial.");
        await using var u=await database.Open(actor);
        var c=(await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0 FOR UPDATE OF c",codigo.Trim().ToUpperInvariant())).SingleOrDefault()
            ?? throw new ReglaInventarioException("No se encontró el cilindro.",404);
        if(c.Ubicacion!="Bodega"||c.UltimoPesajeId is not null) throw new ReglaInventarioException("El pesaje inicial no reemplaza pesajes anteriores ni se registra fuera de Bodega.",409);
        await PesoInicialConFoto(u,c.Id,dto.PesoBrutoGr,actor,correo,dto.Foto,Null(dto.Observacion));
        var result=(await Map(u,await u.Query<CilindroRow>(CilindrosSql+" WHERE c.\"Id_Cilindro\"=@p0",c.Id))).Single();
        await u.Commit();return result;
    }
    // D-48: el pesaje se valida primero (disparador) y después se guarda su foto; ambos se confirman juntos.
    private async Task PesoInicialConFoto(PgUnit u,long cilindro,long peso,int actor,string correo,FotoPesajeRequest foto,string? observacion)
    {
        var codigo=await u.One<string>("SELECT \"CilindroQR\" AS \"Value\" FROM afrisan.\"Cilindros\" WHERE \"Id_Cilindro\"=@p0",cilindro);
        var pesaje=await InsertarPesaje(u,cilindro,null,"Inicial",peso,actor,observacion);
        // DEF-15: si la base rechaza la evidencia, se borra la foto nueva.
        await fotos.GuardarYUsar(pesaje,foto.Id,"Inicial",codigo,foto.Nombre,foto.ContentType,foto.Base64,correo,null,
            archivo=>PgTrabajo.Evidencia(u,archivo,pesaje,actor));
    }
    /// <summary>Todos los pesajes del cilindro con sus fotos, para verificar cada monto (D-48).</summary>
    public async Task<PesajeEvidenciaResponse[]> Pesajes(string codigo)
    {
        await using var u=await database.Open();
        var id=await IdCilindro(u,codigo);
        var pesajes=await u.Query<PesajeEvidenciaRow>("""
            SELECT p."Id_Pesaje" AS "Id",p."Tipo",p."PesoBrutoGr",p."Fecha",us."Nombre"||' '||us."Apellido" AS "Usuario",o."CodigoOT" AS "Orden"
            FROM afrisan."Pesaje" p JOIN afrisan."Usuario" us USING("Id_Usuario") LEFT JOIN afrisan."OrdenTrabajo" o USING("Id_OrdenTrabajo")
            WHERE p."Id_Cilindro"=@p0 ORDER BY p."Fecha" DESC
            """,id);
        var archivos=(await u.Query<FotoPesajeRow>("""
            SELECT e."Id_Evidencia" AS "Id",e."Id_Pesaje" AS "PesajeId",e."NombreArchivo" AS "Nombre",e."ContentType"
            FROM afrisan."EvidenciaPesaje" e JOIN afrisan."Pesaje" p USING("Id_Pesaje") WHERE p."Id_Cilindro"=@p0 ORDER BY e."Fecha"
            """,id)).ToLookup(f=>f.PesajeId);
        return pesajes.Select(p=>new PesajeEvidenciaResponse(p.Id,p.Tipo,p.PesoBrutoGr,p.Fecha,p.Usuario,p.Orden,
            archivos[p.Id].Select(f=>new FotoPesajeInfo(f.Id,f.Nombre,f.ContentType)).ToArray())).ToArray();
    }
    /// <summary>Una foto de pesaje, solo si pertenece a un pesaje de ese cilindro.</summary>
    public async Task<(byte[] Bytes,string Mime)> FotoPesaje(string codigo,Guid evidencia)
    {
        await using var u=await database.Open();
        var id=await IdCilindro(u,codigo);
        var e=(await u.Query<ArchivoEvidenciaRow>("""
            SELECT e."BucketPrivado" AS "Bucket",e."RutaObjeto" AS "Ruta",e."ContentType",e."SHA256" AS "Sha256"
            FROM afrisan."EvidenciaPesaje" e JOIN afrisan."Pesaje" p USING("Id_Pesaje") WHERE e."Id_Evidencia"=@p0 AND p."Id_Cilindro"=@p1
            """,evidencia,id)).SingleOrDefault()??throw new ReglaInventarioException("Foto no encontrada.",404);
        return await fotos.Leer(e.Ruta,e.ContentType,e.Sha256,e.Bucket);
    }
    private static async Task<long> IdCilindro(PgUnit u,string codigo)=>
        (await u.Query<long>("SELECT \"Id_Cilindro\" AS \"Value\" FROM afrisan.\"Cilindros\" WHERE upper(\"CilindroQR\")=@p0",codigo.Trim().ToUpperInvariant())).SingleOrDefault() is var id and >0
            ? id : throw new ReglaInventarioException("No se encontró el cilindro.",404);
    public async Task<EventoResponse[]> Eventos(string codigo)
    {
        await using var u=await database.Open();
        var c=(await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0",codigo.Trim().ToUpperInvariant())).SingleOrDefault()
            ?? throw new ReglaInventarioException("No se encontró el cilindro.",404);
        return await u.Query<EventoResponse>("""
            SELECT h."Fecha",'Ubicación / estado'::text AS "Accion",u."Correo_Usuario" AS "Usuario",
              h."Ubicacion"||' / '||h."Estado" AS "Descripcion"
            FROM afrisan."HistorialCilindro" h JOIN afrisan."Usuario" u USING("Id_Usuario")
            WHERE h."Id_Cilindro"=@p0 ORDER BY h."Id_Historial"
            """,c.Id);
    }
    internal static Task<Guid> InsertarPesaje(PgUnit u,long cilindro,long? ot,string tipo,long peso,int actor,string? observacion=null)=>u.One<Guid>("""
        INSERT INTO afrisan."Pesaje"("Id_Cilindro","Id_OrdenTrabajo","Tipo","PesoBrutoGr","Id_Usuario","Observacion")
        VALUES(@p0,@p1,@p2,@p3,@p4,@p5) RETURNING "Id_Pesaje" AS "Value"
        """,cilindro,ot,tipo,peso,actor,observacion);
    internal static string? Null(string? text)=>string.IsNullOrWhiteSpace(text)?null:text.Trim();
}
public sealed record PesajeEvidenciaRow(Guid Id,string Tipo,long PesoBrutoGr,DateTimeOffset Fecha,string Usuario,string? Orden);
public sealed record FotoPesajeRow(Guid Id,Guid PesajeId,string Nombre,string ContentType);
public sealed record ArchivoEvidenciaRow(string Bucket,string Ruta,string ContentType,string Sha256);
