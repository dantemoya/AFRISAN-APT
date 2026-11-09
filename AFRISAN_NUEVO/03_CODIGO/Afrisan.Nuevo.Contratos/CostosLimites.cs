using System.ComponentModel.DataAnnotations;

namespace Afrisan.Nuevo.Contratos;

public static class RolesControl
{
    public const string Costos = "Administrador,Jefatura";
    public const string Limites = "Bodega,Autorizador,Jefatura";
    public const string EditarLimites = "Bodega,Jefatura";
}
public sealed record PrecioRequest
{
    public Guid SolicitudId { get; init; }
    [Range(1,long.MaxValue)] public long RefrigeranteId { get; init; }
    [Range(typeof(decimal),"0","999999",ParseLimitsInInvariantCulture=true)] public decimal PrecioPorGramo { get; init; }
    [Required,RegularExpression("^(CLP|EUR|USD|UF)$")] public string Moneda { get; init; } = "CLP";
    public DateOnly VigenteDesde { get; init; }
}
public sealed record CambioRequest
{
    public Guid SolicitudId { get; init; }
    [Required,RegularExpression("^(EUR|USD|UF)$")] public string Moneda { get; init; } = "USD";
    [Range(typeof(decimal),"0.00000001","9999999",ParseLimitsInInvariantCulture=true)] public decimal ClpPorUnidad { get; init; }
    public DateOnly FechaValor { get; init; }
}
public sealed record ValorarRefrigeranteRequest
{
    public Guid SolicitudId { get; init; }
    public Guid OperacionId { get; init; }
    [Range(1,long.MaxValue)] public long PrecioId { get; init; }
    [Range(typeof(decimal),"0.000001","999999999",ParseLimitsInInvariantCulture=true)] public decimal Gramos { get; init; }
    public long? CambioId { get; init; }
}
public sealed record PrecioResponse(long Id,long RefrigeranteId,string Refrigerante,decimal PrecioPorGramo,string Moneda,DateOnly VigenteDesde);
public sealed record CambioResponse(long Id,string Moneda,decimal ClpPorUnidad,DateOnly FechaValor);
public sealed record OperacionValorableResponse(Guid Id,string Orden,int SucursalId,string Sucursal,long RefrigeranteId,string Refrigerante,decimal GrInstalados,DateTimeOffset Confirmada);
public sealed record CostoResponse(long Id,Guid SolicitudId,Guid OperacionId,string Orden,int SucursalId,string Sucursal,long RefrigeranteId,string Refrigerante,decimal Gramos,decimal PrecioPorGramo,string Moneda,decimal Total,decimal? FactorClp,decimal? TotalClp,DateOnly? FechaCambio,DateTimeOffset Fecha);
public sealed record DefinirLimiteRequest
{
    [Range(1,int.MaxValue)] public int SucursalId { get; init; }
    [Range(1,long.MaxValue)] public long RefrigeranteId { get; init; }
    [Required,RegularExpression("^(Semana|Mes)$")] public string Periodicidad { get; init; } = "Mes";
    [Range(1,long.MaxValue)] public long GrLimite { get; init; }
    public DateOnly VigenteDesde { get; init; }
    [Range(0,int.MaxValue)] public int Version { get; init; }
}
public sealed record BloquearLimiteRequest
{
    [Range(1,int.MaxValue)] public int Version { get; init; }
    public bool Bloqueado { get; init; }
}
public sealed record MedirLimiteRequest
{
    public Guid SolicitudId { get; init; }
    [Range(1,long.MaxValue)] public long LimiteId { get; init; }
    public DateOnly Fecha { get; init; }
    [MaxLength(2000)] public string? Observaciones { get; init; }
}
public sealed record RevisarExcesoRequest
{
    [Required,StringLength(2000,MinimumLength=5)] public string Conclusion { get; init; } = "";
}
public sealed record LimiteResponse(long Id,int SucursalId,string Sucursal,long? RefrigeranteId,string? Refrigerante,string Periodicidad,long? GrLimite,DateOnly VigenteDesde,bool Bloqueado,int Version);
public sealed record AvanceLimiteResponse(long LimiteId,long HistorialId,int SucursalId,string Sucursal,long RefrigeranteId,string Refrigerante,string Periodicidad,int Version,DateOnly Inicio,DateOnly Fin,long GrLimite,long GrInstalados,int Operaciones)
{
    public long GrExceso => Math.Max(0,GrInstalados-GrLimite);
}
public sealed record MedicionResponse(long Id,Guid SolicitudId,long LimiteId,int SucursalId,string Sucursal,long? RefrigeranteId,string? Refrigerante,string Periodicidad,int Version,DateOnly Inicio,DateOnly Fin,long GrLimite,long GrInstalados,string? Observaciones,string? Conclusion,DateTimeOffset Fecha)
{
    public long GrExceso => Math.Max(0,GrInstalados-GrLimite);
}
