using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.PowerBi;

public sealed partial class PgPowerBi
{
    public async Task<BiResumenResponse> ResumenSql(int actor,DateOnly? desde,DateOnly? hasta,
        int? sucursalId,long? refrigeranteId,int? tecnicoId)
    {
        await using var u = await database.Open();
        await u.Exec("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ, READ ONLY");
        await u.Query<string>("SELECT set_config('afrisan.id_usuario', @p0, true) AS \"Value\"",actor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var generado = await u.One<DateTimeOffset>("SELECT transaction_timestamp() AS \"Value\"");
        var fin = hasta ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(generado,
            TimeZoneInfo.FindSystemTimeZoneById("America/Santiago")).DateTime);
        var inicio = desde ?? fin.AddDays(-29);
        if (inicio > fin || sucursalId is <= 0 || refrigeranteId is <= 0 || tecnicoId is <= 0)
            throw new ReglaInventarioException("Revisa el rango de fechas y los filtros.");
        object?[] filtros = [inicio,fin,sucursalId,refrigeranteId,tecnicoId];
        // EXISTS conserva el grano de la OT aunque solicite más de un gas.
        const string conjunto = """
            WITH filtro AS (SELECT @p0::date desde,@p1::date hasta,@p2::integer sucursal,
              @p3::bigint gas,@p4::integer tecnico),
            elegibles AS (SELECT o.* FROM afrisan."OrdenTrabajo" o CROSS JOIN filtro f
              WHERE (f.sucursal IS NULL OR o."Id_Sucursal"=f.sucursal)
                AND (f.tecnico IS NULL OR o."Id_Tecnico"=f.tecnico)),
            ordenes AS (SELECT o.* FROM elegibles o CROSS JOIN filtro f
              WHERE (o."FechaCreacion" AT TIME ZONE 'America/Santiago')::date BETWEEN f.desde AND f.hasta
                AND EXISTS(SELECT 1 FROM afrisan."DetalleOT" d WHERE d."Id_OrdenTrabajo"=o."Id_OrdenTrabajo"
                  AND (f.gas IS NULL OR d."Id_Refrigerante"=f.gas))),
            inventario AS (SELECT c."Estado",c."Id_UltimoPesaje" FROM afrisan."Cilindros" c CROSS JOIN filtro f
              WHERE (f.sucursal IS NULL OR c."Id_Sucursal"=f.sucursal)
                AND (f.gas IS NULL OR c."Id_Refrigerante"=f.gas)
                AND (f.tecnico IS NULL OR c."Id_Tecnico"=f.tecnico)),
            operaciones AS (SELECT op."Tipo",g."CodigoRefrigerante" gas,o."Id_Sucursal" sucursal,
                (op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date dia,
                (p."PesoBrutoGr"-c."TaraGr")/1000.0 kilos,
                (op."Tipo"='Reemplazo' AND NOT EXISTS(SELECT 1 FROM afrisan."RecepcionBodega" r WHERE r."Id_Operacion"=op."Id_Operacion")) pendiente
              FROM afrisan."OperacionTerreno" op JOIN elegibles o USING("Id_OrdenTrabajo")
              JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado"
              JOIN afrisan."Refrigerantes" g USING("Id_Refrigerante")
              JOIN afrisan."PesajeVigente" p ON p."Id_Pesaje"=op."Id_PesajeInstalado" CROSS JOIN filtro f
              WHERE op."Estado"='Registrada'
                AND (op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date BETWEEN f.desde AND f.hasta
                AND (f.gas IS NULL OR c."Id_Refrigerante"=f.gas)),
            incidencias AS (SELECT i."FechaRevision" FROM afrisan."Incidencia" i JOIN elegibles o USING("Id_OrdenTrabajo") CROSS JOIN filtro f
              WHERE (i."Fecha" AT TIME ZONE 'America/Santiago')::date BETWEEN f.desde AND f.hasta
                AND EXISTS(SELECT 1 FROM afrisan."DetalleOT" d WHERE d."Id_OrdenTrabajo"=o."Id_OrdenTrabajo"
                  AND (f.gas IS NULL OR d."Id_Refrigerante"=f.gas)))
            """;
        var c = await u.One<BiConteosSql>(conjunto + """
            SELECT (SELECT count(*)::int FROM inventario) AS "Cilindros",
              (SELECT count(*)::int FROM inventario WHERE "Id_UltimoPesaje" IS NULL) AS "SinPesaje",
              (SELECT count(*)::int FROM ordenes WHERE "Estado" NOT IN ('Cerrada','Anulada')) AS "Abiertas",
              (SELECT count(*)::int FROM ordenes WHERE "Estado"='Cerrada') AS "Cerradas",
              (SELECT count(*)::int FROM ordenes WHERE "Estado"='Anulada') AS "Anuladas",
              (SELECT coalesce(sum(kilos),0) FROM operaciones) AS "Kilos",
              (SELECT count(*)::int FROM operaciones WHERE pendiente) AS "Pendientes",
              (SELECT count(*)::int FROM incidencias WHERE "FechaRevision" IS NULL) AS "Incidencias"
            """,filtros);
        var inv = await u.Query<BiGrupoResponse>(conjunto + "SELECT \"Estado\" AS \"Nombre\",count(*)::numeric AS \"Valor\" FROM inventario GROUP BY 1 ORDER BY 2 DESC,1",filtros);
        var ord = await u.Query<BiGrupoResponse>(conjunto + "SELECT \"Estado\" AS \"Nombre\",count(*)::numeric AS \"Valor\" FROM ordenes GROUP BY 1 ORDER BY 2 DESC,1",filtros);
        var ops = await u.Query<BiGrupoResponse>(conjunto + "SELECT gas AS \"Nombre\",sum(kilos) AS \"Valor\" FROM operaciones GROUP BY 1 ORDER BY 1",filtros);
        // D-45: consumo por sucursal = kg instalados (regla de los límites) junto a los kg solicitados en OT no
        // anuladas, por semana (lunes a domingo) y por mes, con los mismos filtros de los indicadores.
        var consumo = await u.Query<BiConsumoResponse>(conjunto + """
            ,solicitados AS (SELECT o."Id_Sucursal" sucursal,(o."FechaCreacion" AT TIME ZONE 'America/Santiago')::date dia,
                d."GrSolicitados"/1000.0 kilos
              FROM ordenes o JOIN afrisan."DetalleOT" d ON d."Id_OrdenTrabajo"=o."Id_OrdenTrabajo" CROSS JOIN filtro f
              WHERE o."Estado"<>'Anulada' AND (f.gas IS NULL OR d."Id_Refrigerante"=f.gas)),
            base AS (SELECT sucursal,dia,kilos inst,0::numeric sol FROM operaciones
              UNION ALL SELECT sucursal,dia,0::numeric,kilos FROM solicitados),
            periodos AS (SELECT 'Semana' periodo,date_trunc('week',dia::timestamp)::date inicio,sucursal,inst,sol FROM base
              UNION ALL SELECT 'Mes',date_trunc('month',dia::timestamp)::date,sucursal,inst,sol FROM base)
            SELECT p.periodo AS "Periodo",p.inicio AS "Inicio",s."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",
              s."Id_Cliente" AS "ClienteId",cl."NombreEmpresa" AS "Cliente",sum(p.inst) AS "KgInstalados",sum(p.sol) AS "KgSolicitados"
            FROM periodos p JOIN afrisan."Sucursales" s ON s."Id_Sucursal"=p.sucursal
            JOIN afrisan."Cliente" cl ON cl."Id_Cliente"=s."Id_Cliente"
            GROUP BY 1,2,3,4,5,6 ORDER BY 1,2,6,4
            """,filtros);
        var sucursales = await u.Query<BiOpcionResponse>("""
            SELECT DISTINCT s."Id_Sucursal"::bigint AS "Id",s."NombreSucursal" AS "Nombre"
            FROM afrisan."OrdenTrabajo" o JOIN afrisan."Sucursales" s USING("Id_Sucursal") ORDER BY 2
            """);
        var gases = await u.Query<BiOpcionResponse>("""
            SELECT r."Id_Refrigerante" AS "Id",r."CodigoRefrigerante" AS "Nombre" FROM afrisan."Refrigerantes" r
            WHERE EXISTS(SELECT 1 FROM afrisan."DetalleOT" d WHERE d."Id_Refrigerante"=r."Id_Refrigerante")
              OR EXISTS(SELECT 1 FROM afrisan."Cilindros" c WHERE c."Id_Refrigerante"=r."Id_Refrigerante") ORDER BY 2
            """);
        var tecnicos = await u.Query<BiOpcionResponse>("""
            SELECT DISTINCT t."Id_Usuario"::bigint AS "Id",t."Nombre"||' '||t."Apellido" AS "Nombre"
            FROM afrisan."OrdenTrabajo" o JOIN afrisan."Usuario" t ON t."Id_Usuario"=o."Id_Tecnico" ORDER BY 2
            """);
        return new(generado,inicio,fin,c.Cilindros,c.SinPesaje,c.Abiertas,c.Cerradas,c.Kilos,c.Pendientes,c.Incidencias,
            inv,ord,ops,sucursales,gases,tecnicos,c.Anuladas,consumo);
    }
}
public sealed record BiConteosSql(int Cilindros,int SinPesaje,int Abiertas,int Cerradas,int Anuladas,decimal Kilos,int Pendientes,int Incidencias);
