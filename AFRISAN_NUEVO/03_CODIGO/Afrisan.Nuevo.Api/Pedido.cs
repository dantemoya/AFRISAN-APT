using System.Globalization;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api;

/// <summary>
/// Datos del pedido de una OT (D-49): quién la pidió en el cliente, su prioridad y su fecha
/// programada. La misma regla se aplica en PostgreSQL y en el modo en memoria; la base de datos
/// además la vuelve a comprobar con su disparador.
/// </summary>
public static class Pedido
{
    private static readonly TimeZoneInfo Chile = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

    public static DateOnly Hoy() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Chile).DateTime);

    public static (string Solicitante, string Prioridad, DateOnly FechaProgramada) Validar(CrearOtRequest dto)
    {
        var solicitante = dto.Solicitante?.Trim() ?? "";
        if (solicitante.Length is < 3 or > 150)
            throw new ReglaInventarioException("Indica quién pidió el trabajo en el cliente (3 a 150 caracteres).");
        if (!Prioridades.Todas.Contains(dto.Prioridad))
            throw new ReglaInventarioException("Elige la prioridad: Baja, Normal, Alta o Urgente.");
        if (dto.FechaProgramada is not { } fecha)
            throw new ReglaInventarioException("Indica la fecha programada de la visita.");
        if (fecha < Hoy())
            throw new ReglaInventarioException("La fecha programada no puede ser anterior a hoy.");
        return (solicitante, dto.Prioridad, fecha);
    }

    /// <summary>Texto legible para el historial de la OT (DEF-10).</summary>
    public static string Detalle(string solicitante, string prioridad, DateOnly fecha) =>
        $"Prioridad {prioridad} · programada para el {fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} · solicitada por {solicitante}";
}
