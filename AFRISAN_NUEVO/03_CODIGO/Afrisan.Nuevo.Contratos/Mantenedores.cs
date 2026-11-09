using System.ComponentModel.DataAnnotations;

namespace Afrisan.Nuevo.Contratos;

public sealed record ClienteMantenedorResponse(int Id,string Rut,string Nombre,bool Activo,int Version);
public sealed record SucursalMantenedorResponse(int Id,int ClienteId,string Cliente,string Nombre,string Direccion,
    bool Activo,bool ClienteActivo,int Version);
public sealed record RefrigeranteMantenedorResponse(long Id,string Codigo,string Nombre,bool Activo,int Version);
public sealed record EditarClienteRequest
{
    [Required,StringLength(20)] public string Rut { get; init; } = "";
    [Required,StringLength(200)] public string Nombre { get; init; } = "";
    [Range(1,int.MaxValue)] public int Version { get; init; }
}
public sealed record EditarSucursalRequest
{
    [Required,StringLength(200)] public string Nombre { get; init; } = "";
    [Required,StringLength(400)] public string Direccion { get; init; } = "";
    [Range(1,int.MaxValue)] public int Version { get; init; }
}
public sealed record EditarRefrigeranteRequest
{
    [Required,StringLength(40)] public string Codigo { get; init; } = "";
    [Required,StringLength(150)] public string Nombre { get; init; } = "";
    [Range(1,int.MaxValue)] public int Version { get; init; }
}
public sealed record EstadoMantenedorRequest
{
    public bool Activo { get; init; }
    [Range(1,int.MaxValue)] public int Version { get; init; }
}
public sealed record BajaCilindroRequest
{
    public Guid Id { get; init; }
    [Required] public string EstadoDestino { get; init; } = "Chatarra";
    [Required,StringLength(2000,MinimumLength=5)] public string Motivo { get; init; } = "";
}
public sealed record BajaCilindroResponse(Guid Id,long CilindroId,string Codigo,string Refrigerante,
    string EstadoDestino,string Motivo,int UsuarioId,string Usuario,DateTimeOffset Fecha);
