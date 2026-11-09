using Afrisan.Nuevo.Api.Persistencia;

namespace Afrisan.Nuevo.Pruebas;

// Inicio de día en America/Santiago, usado por precios y límites (incluye cambios de horario).
public sealed class CostosTests
{
    private static readonly TimeZoneInfo Santiago = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

    public static IEnumerable<object[]> Dias2026()
    {
        for (var d = new DateOnly(2026, 1, 1); d.Year == 2026; d = d.AddDays(1)) yield return [d];
    }

    [Theory]
    [MemberData(nameof(Dias2026))]
    public void InicioChile_es_el_primer_instante_del_dia_local(DateOnly dia)
    {
        var inicio = PgCostos.InicioChile(dia);

        Assert.Equal(dia, DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(inicio, Santiago).DateTime));
        Assert.Equal(dia.AddDays(-1), DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(inicio.AddMinutes(-1), Santiago).DateTime));
    }

    [Fact]
    public void Hoy_corresponde_a_la_fecha_chilena()
    {
        var esperado = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Santiago).DateTime);
        Assert.InRange(PgCostos.Hoy.DayNumber, esperado.DayNumber - 1, esperado.DayNumber);
    }
}
