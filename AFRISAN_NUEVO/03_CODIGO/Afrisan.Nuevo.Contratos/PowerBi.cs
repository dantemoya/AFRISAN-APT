namespace Afrisan.Nuevo.Contratos;

public static class RolesBi
{
    public const string Consulta = "Administrador,Jefatura";
}

public sealed record PowerBiConfiguracionResponse(bool Configurado, string? EmbedUrl, string? AbrirUrl);

/// <summary>Token de solo lectura para conectar Power BI a la API: 30 días y solo las tablas.</summary>
public sealed record TokenPowerBiResponse(string Token, DateTimeOffset Vence, string[] Tablas);

public sealed record PowerBiDemoResponse(bool Configurado, string? UrlPublica, DateTimeOffset? Actualizada);

public sealed record ConfigurarPowerBiDemoRequest
{
    [System.ComponentModel.DataAnnotations.MaxLength(4096)]
    public string UrlPublica { get; init; } = "";
}

// La aplicación no recibe credenciales Microsoft ni URLs de inserción privada.
public static class EnlacePowerBiPublico
{
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        valor = valor.Trim();
        const string mensaje = "Usa el enlace público de Power BI con formato https://app.powerbi.com/view?r=...";
        if (valor.Length > 4096 || !Uri.TryCreate(valor, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort
            || !string.Equals(uri.Host, "app.powerbi.com", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment)
            || uri.AbsolutePath != "/view")
            throw new System.ComponentModel.DataAnnotations.ValidationException(mensaje);
        var ids = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => Uri.UnescapeDataString(p[0]) == "r")
            .Select(p => p.Length == 2 ? Uri.UnescapeDataString(p[1].Replace('+', ' ')) : "").ToArray();
        if (ids.Length != 1 || string.IsNullOrWhiteSpace(ids[0])
            || ids[0].Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '+' or '/' or '=')))
            throw new System.ComponentModel.DataAnnotations.ValidationException(mensaje);
        return "https://app.powerbi.com/view?r=" + Uri.EscapeDataString(ids[0]);
    }
}
public sealed record BiGrupoResponse(string Nombre, decimal Valor);
public sealed record BiOpcionResponse(long Id, string Nombre);
public sealed record BiResumenResponse(DateTimeOffset GeneradoUtc, DateOnly Desde, DateOnly Hasta,
    int Cilindros, int SinPesaje, int OrdenesAbiertas, int OrdenesCerradas,
    decimal KgInstalados, int RetiradosPendientes, int IncidenciasAbiertas,
    BiGrupoResponse[] InventarioPorEstado, BiGrupoResponse[] OrdenesPorEstado,
    BiGrupoResponse[] InstaladosPorRefrigerante, BiOpcionResponse[] Sucursales,
    BiOpcionResponse[] Refrigerantes, BiOpcionResponse[] Tecnicos);

// Tablas del paquete Power BI. Sin correos, fotografías ni credenciales.
public sealed record BiInventario(long CilindroId, string Codigo, long RefrigeranteId,
    string Refrigerante, string Estado, string Ubicacion, int? SucursalId, string? Sucursal,
    int? TecnicoId, string? Tecnico, decimal? KgSegunUltimoPesaje, DateTimeOffset? FechaPesaje);
public sealed record BiOrden(long OtId, string Codigo, int SucursalId, string Sucursal,
    int TecnicoId, string Tecnico, string Estado, DateTimeOffset FechaCreacion,
    DateOnly DiaCreacion, DateTimeOffset? FechaCierre);
public sealed record BiDetalle(long OtId, long RefrigeranteId, string Refrigerante,
    decimal KgSolicitados, decimal KgAutorizados);
public sealed record BiOperacion(Guid OperacionId, long OtId, string Tipo,
    long RefrigeranteId, string Refrigerante, string CilindroInstalado, string? CilindroRetirado,
    DateTimeOffset FechaConfirmacion, DateOnly DiaConfirmacion, decimal KgInstalados,
    decimal? KgRetirados, DateTimeOffset? FechaRecepcion, decimal? KgRecepcion,
    bool PendienteRecepcion);
public sealed record BiDespacho(Guid DespachoId, long OtId, string Cilindro,
    long RefrigeranteId, string Refrigerante, DateTimeOffset Fecha, DateOnly Dia,
    decimal KgDespachados, string Estado, DateTimeOffset? FechaDevolucion,
    decimal? KgDevueltos);
public sealed record BiIncidencia(Guid IncidenciaId, long OtId, string Tipo,
    DateTimeOffset Fecha, DateOnly Dia, bool Abierta, DateTimeOffset? FechaRevision);
public sealed record BiCosto(long CostoId,Guid OperacionId,long OtId,long RefrigeranteId,string Refrigerante,
    decimal Gramos,decimal PrecioPorGramo,string Moneda,decimal Total,decimal? FactorClp,decimal? TotalClp,
    DateTimeOffset FechaValoracion,DateOnly DiaValoracion,DateOnly? FechaCambio);
