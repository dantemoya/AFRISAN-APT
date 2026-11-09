using System.ComponentModel.DataAnnotations;
namespace Afrisan.Nuevo.Contratos;

public sealed record ClienteResponse(int Id, string Rut, string Nombre);
public sealed record CrearClienteRequest
{
    [Required, StringLength(20)] public string Rut { get; init; } = "";
    [Required, StringLength(200)] public string Nombre { get; init; } = "";
}
public sealed record SucursalResponse(int Id, int ClienteId, string Cliente, string Nombre, string Direccion);
public sealed record CrearSucursalRequest
{
    [Range(1, int.MaxValue)] public int ClienteId { get; init; }
    [Required, StringLength(200)] public string Nombre { get; init; } = "";
    [Required, StringLength(400)] public string Direccion { get; init; } = "";
}
public sealed record UsuarioResponse(int Id, string Nombre, string Apellido, string Correo, string Rol, bool Activo, bool Bloqueado = false);

/// <summary>Activa o desactiva una cuenta. Desactivar cierra sus sesiones abiertas.</summary>
public sealed record CambiarEstadoUsuarioRequest
{
    public bool Activo { get; init; }
}

/// <summary>Cambia el rol de una cuenta. Cierra sus sesiones abiertas.</summary>
public sealed record CambiarRolUsuarioRequest
{
    [Required] public string Rol { get; init; } = "";
}

/// <summary>Contraseña nueva definida por el Administrador. Quita el bloqueo y cierra las sesiones abiertas.</summary>
public sealed record RestablecerClaveRequest
{
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; init; } = "";
}
public sealed record CrearUsuarioRequest
{
    [Required, StringLength(100)] public string Nombre { get; init; } = "";
    [Required, StringLength(100)] public string Apellido { get; init; } = "";
    [Required, EmailAddress, StringLength(254)] public string Correo { get; init; } = "";
    [Required] public string Rol { get; init; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; init; } = "";
}
