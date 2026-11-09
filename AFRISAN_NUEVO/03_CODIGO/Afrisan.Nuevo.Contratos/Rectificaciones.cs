using System.ComponentModel.DataAnnotations;

namespace Afrisan.Nuevo.Contratos;

public sealed record SolicitarRectificacionRequest
{
    public Guid Id { get; init; }
    public Guid PesajeId { get; init; }
    [Range(0, long.MaxValue)] public long PesoPropuestoGr { get; init; }
    [Required, StringLength(2000, MinimumLength = 5)] public string Motivo { get; init; } = "";
    [Required] public FotoPesajeRequest? Foto { get; init; }
}
public sealed record ResolverRectificacionRequest
{
    public bool Aprobar { get; init; }
    [Required, StringLength(2000, MinimumLength = 5)] public string Respuesta { get; init; } = "";
}
public sealed record PesajeRectificableResponse(Guid Id, string Cilindro, string Tipo,
    long PesoOriginalGr, long PesoVigenteGr, DateTimeOffset Fecha);
public sealed record RectificacionResponse(Guid Id, Guid PesajeId, string Cilindro, string Tipo,
    long PesoAnteriorGr, long PesoPropuestoGr, string Motivo, string Solicitante, DateTimeOffset Fecha,
    string Estado, string? Respuesta, string? Revisor, DateTimeOffset? FechaRevision);
