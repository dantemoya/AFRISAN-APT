using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgCostos(PgDatabase database)
{
    private const string PrecioSql = """
        SELECT p."Id_Precio" AS "Id",p."Id_Refrigerante" AS "RefrigeranteId",r."CodigoRefrigerante" AS "Refrigerante",
          p."PrecioGrRefrigerante" AS "PrecioPorGramo",p."Moneda",(p."VigenteDesde" AT TIME ZONE 'America/Santiago')::date AS "VigenteDesde"
        FROM afrisan."PreciosRefrigerantes" p JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
        """;
    private const string CambioSql = """
        SELECT "Id_Cambio" AS "Id","Moneda","ClpPorUnidad","FechaValor" FROM afrisan."TiposCambio"
        """;
    private const string CostoSql = """
        SELECT c."Id_Costos" AS "Id",coalesce(c."Id_Solicitud",'00000000-0000-0000-0000-000000000000'::uuid) AS "SolicitudId",
          c."Id_Operacion" AS "OperacionId",ot."CodigoOT" AS "Orden",ot."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",
          ci."Id_Refrigerante" AS "RefrigeranteId",r."CodigoRefrigerante" AS "Refrigerante",c."Cantidad" AS "Gramos",
          c."TarifaUnitaria" AS "PrecioPorGramo",c."Moneda",c."PrecioTotal" AS "Total",c."FactorClp",c."TotalClp",tc."FechaValor" AS "FechaCambio",c."FechaValoracion" AS "Fecha"
        FROM afrisan."Costos" c JOIN afrisan."OperacionTerreno" op ON op."Id_Operacion"=c."Id_Operacion" AND c."Concepto"='Refrigerante' AND c."Unidad"='g' JOIN afrisan."OrdenTrabajo" ot USING("Id_OrdenTrabajo")
        JOIN afrisan."Sucursales" s USING("Id_Sucursal") JOIN afrisan."Cilindros" ci ON ci."Id_Cilindro"=op."Id_Instalado"
        JOIN afrisan."Refrigerantes" r ON r."Id_Refrigerante"=ci."Id_Refrigerante" LEFT JOIN afrisan."TiposCambio" tc USING("Id_Cambio")
        """;
    public static DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,"America/Santiago").DateTime);
    public static DateTimeOffset InicioChile(DateOnly d)
    {
        var zona=TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");var local=d.ToDateTime(TimeOnly.MinValue);
        // En el cambio de horario puede no existir la medianoche local.
        while(zona.IsInvalidTime(local))local=local.AddMinutes(1);
        return new(TimeZoneInfo.ConvertTimeToUtc(local,zona));
    }
    private static void Id(Guid id) { if(id==Guid.Empty)throw new ReglaInventarioException("Falta el identificador de la solicitud."); }
    private static void Escala(decimal n,int escala) { if(decimal.Round(n,escala)!=n)throw new ReglaInventarioException($"El valor admite hasta {escala} decimales."); }
    public async Task<PrecioResponse[]> Precios(){await using var u=await database.Open();return await u.Query<PrecioResponse>(PrecioSql+" ORDER BY p.\"VigenteDesde\" DESC,p.\"Id_Precio\" DESC");}
    public async Task<CambioResponse[]> Cambios(){await using var u=await database.Open();return await u.Query<CambioResponse>(CambioSql+" ORDER BY \"FechaValor\" DESC,\"Id_Cambio\" DESC");}
    public async Task<CostoResponse[]> Costos(){await using var u=await database.Open();return await u.Query<CostoResponse>(CostoSql+" ORDER BY c.\"FechaValoracion\" DESC,c.\"Id_Costos\" DESC");}
    public async Task<OperacionValorableResponse[]> Operaciones()
    {
        await using var u=await database.Open();return await u.Query<OperacionValorableResponse>("""
            SELECT op."Id_Operacion" AS "Id",ot."CodigoOT" AS "Orden",ot."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",
              c."Id_Refrigerante" AS "RefrigeranteId",r."CodigoRefrigerante" AS "Refrigerante",(p."PesoBrutoGr"-c."TaraGr")::numeric AS "GrInstalados",op."FechaConfirmacion" AS "Confirmada"
            FROM afrisan."OperacionTerreno" op JOIN afrisan."OrdenTrabajo" ot USING("Id_OrdenTrabajo") JOIN afrisan."Sucursales" s USING("Id_Sucursal")
            JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado" JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
            JOIN afrisan."PesajeVigente" p ON p."Id_Pesaje"=op."Id_PesajeInstalado" WHERE op."Estado"='Registrada' ORDER BY op."FechaConfirmacion" DESC
            """);
    }
    public async Task<PrecioResponse> RegistrarPrecio(PrecioRequest dto,int actor)
    {
        Id(dto.SolicitudId);Escala(dto.PrecioPorGramo,8);
        if(dto.VigenteDesde==default)throw new ReglaInventarioException("Indica la fecha de vigencia.");
        await using var u=await database.Open(actor);
        await u.Exec("""
            INSERT INTO afrisan."PreciosRefrigerantes" ("Id_Solicitud","Id_Refrigerante","PrecioGrRefrigerante","Moneda","VigenteDesde","Id_Registrador")
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5) ON CONFLICT("Id_Solicitud") DO NOTHING
            """,dto.SolicitudId,dto.RefrigeranteId,dto.PrecioPorGramo,dto.Moneda,InicioChile(dto.VigenteDesde),actor);
        var r=await u.One<PrecioResponse>(PrecioSql+" WHERE p.\"Id_Solicitud\"=@p0",dto.SolicitudId);
        if((r.RefrigeranteId,r.PrecioPorGramo,r.Moneda,r.VigenteDesde)!=(dto.RefrigeranteId,dto.PrecioPorGramo,dto.Moneda,dto.VigenteDesde))throw new ReglaInventarioException("La solicitud ya se utilizó con otro precio.",409);
        await u.Commit();return r;
    }
    public async Task<CambioResponse> RegistrarCambio(CambioRequest dto,int actor)
    {
        Id(dto.SolicitudId);Escala(dto.ClpPorUnidad,8);
        if(dto.FechaValor==default||dto.FechaValor>Hoy)throw new ReglaInventarioException("Indica una fecha de valor de hoy o anterior.");
        await using var u=await database.Open(actor);
        await u.Exec("""
            INSERT INTO afrisan."TiposCambio" ("Id_Solicitud","Moneda","ClpPorUnidad","FechaValor","Id_Registrador") VALUES(@p0,@p1,@p2,@p3,@p4) ON CONFLICT("Id_Solicitud") DO NOTHING
            """,dto.SolicitudId,dto.Moneda,dto.ClpPorUnidad,dto.FechaValor,actor);
        var r=await u.One<CambioResponse>(CambioSql+" WHERE \"Id_Solicitud\"=@p0",dto.SolicitudId);
        if((r.Moneda,r.ClpPorUnidad,r.FechaValor)!=(dto.Moneda,dto.ClpPorUnidad,dto.FechaValor))throw new ReglaInventarioException("La solicitud ya se utilizó con otro valor de cambio.",409);
        await u.Commit();return r;
    }
    public async Task<CostoResponse> Valorar(ValorarRefrigeranteRequest dto,int actor)
    {
        Id(dto.SolicitudId);Id(dto.OperacionId);Escala(dto.Gramos,6);
        await using var u=await database.Open(actor);
        var op=(await u.Query<IdRow>("SELECT \"Id_OrdenTrabajo\" AS \"Id\" FROM afrisan.\"OperacionTerreno\" WHERE \"Id_Operacion\"=@p0 AND \"Estado\"='Registrada' FOR UPDATE",dto.OperacionId)).SingleOrDefault();
        if(op is null)throw new ReglaInventarioException("Operación confirmada no encontrada.",404);
        var anterior=(await u.Query<CostoResponse>(CostoSql+" WHERE c.\"Id_Solicitud\"=@p0",dto.SolicitudId)).SingleOrDefault();
        if(anterior is not null)
        {
            var coincide=await u.One<bool>("SELECT EXISTS(SELECT 1 FROM afrisan.\"Costos\" WHERE \"Id_Solicitud\"=@p0 AND \"Id_Operacion\"=@p1 AND \"Id_Precio\"=@p2 AND \"Cantidad\"=@p3 AND \"Id_Cambio\" IS NOT DISTINCT FROM @p4::bigint) AS \"Value\"",dto.SolicitudId,dto.OperacionId,dto.PrecioId,dto.Gramos,dto.CambioId);
            if(!coincide)throw new ReglaInventarioException("La solicitud ya se utilizó con otra valoración.",409);
            return anterior;
        }
        var precio=(await u.Query<PrecioResponse>(PrecioSql+" WHERE p.\"Id_Precio\"=@p0 AND p.\"VigenteDesde\"<=now()",dto.PrecioId)).SingleOrDefault()??throw new ReglaInventarioException("Selecciona un precio vigente.");
        decimal factor=1;
        if(precio.Moneda=="CLP") { if(dto.CambioId is not null)throw new ReglaInventarioException("CLP usa factor 1."); }
        else
        {
            var cambio=(await u.Query<CambioResponse>(CambioSql+" WHERE \"Id_Cambio\"=@p0 AND \"Moneda\"=@p1",dto.CambioId,precio.Moneda)).SingleOrDefault()??throw new ReglaInventarioException("Ingresa y selecciona el valor manual de cambio de esa moneda.");
            factor=cambio.ClpPorUnidad;
        }
        await u.Exec("""
            INSERT INTO afrisan."Costos" ("Id_Solicitud","Id_Operacion","NumeroLinea","Concepto","Descripcion","Cantidad","Unidad","TarifaUnitaria","Id_Precio","Moneda","Id_Cambio","FactorClp","Id_Registrador")
            SELECT @p0,@p1,coalesce(max("NumeroLinea"),0)+1,'Refrigerante','Valoración de refrigerante; cantidad declarada',@p2,'g',@p3,@p4,@p5,@p6,@p7,@p8
            FROM afrisan."Costos" WHERE "Id_Operacion"=@p1
            """,dto.SolicitudId,dto.OperacionId,dto.Gramos,precio.PrecioPorGramo,precio.Id,precio.Moneda,dto.CambioId,factor,actor);
        var result=await u.One<CostoResponse>(CostoSql+" WHERE c.\"Id_Solicitud\"=@p0",dto.SolicitudId);await u.Commit();return result;
    }
}
