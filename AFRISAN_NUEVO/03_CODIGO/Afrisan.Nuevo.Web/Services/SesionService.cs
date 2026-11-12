using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Afrisan.Nuevo.Contratos;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Afrisan.Nuevo.Web.Services;

public sealed class SesionService(HttpClient http, IJSRuntime js) : AuthenticationStateProvider
{
    private const string Key = "afrisan_nuevo.sesion";
    private const string InstanciaKey = "afrisan_nuevo.instancia";
    private const string EntornoKey = "afrisan_nuevo.entorno";
    private readonly ClaimsPrincipal anonimo = new(new ClaimsIdentity());
    public SesionResponse? Sesion { get; private set; }
    public EntornoResponse? Entorno { get; private set; }
    public bool SinConexion { get; private set; }
    /// <summary>True mientras se reintenta en segundo plano la conexión con la API.</summary>
    public bool Reconectando { get; private set; }
    /// <summary>Se dispara cuando cambia el estado de conexión, para que las pantallas se actualicen.</summary>
    public event Action? CambioConexion;
    private CancellationTokenSource? reconexion;
    public bool Autenticado => Sesion is not null && Sesion.Vence > DateTimeOffset.UtcNow;
    public bool PuedeRegistrar => Sesion?.Rol is "Bodega" or "Administrador";
    public bool PuedeCatalogo => Sesion?.Rol is "Bodega" or "Jefatura" or "Administrador";

    public async Task InicializarAsync()
    {
        try
        {
            Aplicar(null);
            await MigrarSesionAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Entorno = await http.GetFromJsonAsync<EntornoResponse>("entorno", timeout.Token);
            SinConexion = false;
            var anterior = await js.InvokeAsync<string?>("localStorage.getItem", InstanciaKey);
            if (anterior != Entorno?.Instancia) await js.InvokeVoidAsync("localStorage.removeItem", Key);
            await js.InvokeVoidAsync("localStorage.setItem", InstanciaKey, Entorno?.Instancia);
            await js.InvokeVoidAsync("localStorage.setItem", EntornoKey, JsonSerializer.Serialize(Entorno));
            var guardada = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (guardada is not null)
            {
                var sesion = JsonSerializer.Deserialize<SesionResponse>(guardada);
                if (sesion is not null && sesion.Vence > DateTimeOffset.UtcNow) Aplicar(sesion);
                else await js.InvokeVoidAsync("localStorage.removeItem", Key);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or JSException)
        {
            SinConexion = true;
            Entorno = null; Aplicar(null);
            // La sesión offline permite solo revisar/guardar datos locales.
            // Las autorizaciones del servidor siguen comprobando el JWT.
            try
            {
                var guardada = await js.InvokeAsync<string?>("localStorage.getItem", Key);
                var cache = await js.InvokeAsync<string?>("localStorage.getItem", EntornoKey);
                var local = guardada is null ? null : JsonSerializer.Deserialize<SesionResponse>(guardada);
                if (local is not null && local.Vence > DateTimeOffset.UtcNow && !string.IsNullOrWhiteSpace(local.Usuario))
                {
                    Entorno = cache is null ? null : JsonSerializer.Deserialize<EntornoResponse>(cache);
                    Aplicar(local);
                }
            }
            catch (Exception localError) when (localError is JsonException or JSException) { Aplicar(null); }
        }
    }

    /// <summary>
    /// Si la API no respondió (por ejemplo, porque aún está arrancando), la vuelve a buscar en segundo
    /// plano y se conecta sola en cuanto responde. Solo reinicializa la sesión cuando la API ya contesta,
    /// para no cerrar una sesión de trabajo sin conexión en cada intento.
    /// </summary>
    public void IniciarReconexion()
    {
        if (!SinConexion || reconexion is not null) return;
        reconexion = new CancellationTokenSource();
        _ = ReconectarAsync(reconexion.Token);
    }

    private async Task ReconectarAsync(CancellationToken cancelacion)
    {
        Reconectando = true;
        CambioConexion?.Invoke();
        var espera = TimeSpan.FromSeconds(1);
        try
        {
            while (SinConexion && !cancelacion.IsCancellationRequested)
            {
                await Task.Delay(espera, cancelacion);
                if (await ApiRespondeAsync(cancelacion)) await InicializarAsync();
                if (espera < TimeSpan.FromSeconds(4)) espera += espera;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Reconectando = false;
            reconexion = null;
            CambioConexion?.Invoke();
        }
    }

    private async Task<bool> ApiRespondeAsync(CancellationToken cancelacion)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var respuesta = await http.GetAsync("entorno", timeout.Token);
            return respuesta.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { return false; }
    }

    public async Task IngresarAsync(SesionResponse sesion)
    {
        await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(sesion));
        Aplicar(sesion);
    }

    public async Task SalirAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", Key);
        await js.InvokeVoidAsync("sessionStorage.removeItem", Key);
        Aplicar(null);
    }

    private async Task MigrarSesionAsync()
    {
        // Compatibilidad con pestañas de la versión anterior. La caducidad del JWT se conserva.
        foreach (var key in new[] { Key, InstanciaKey, EntornoKey })
        {
            var anterior = await js.InvokeAsync<string?>("sessionStorage.getItem", key);
            var actual = await js.InvokeAsync<string?>("localStorage.getItem", key);
            if (actual is null && anterior is not null)
                await js.InvokeVoidAsync("localStorage.setItem", key, anterior);
            await js.InvokeVoidAsync("sessionStorage.removeItem", key);
        }
    }

    private void Aplicar(SesionResponse? sesion)
    {
        Sesion = sesion;
        http.DefaultRequestHeaders.Authorization = sesion is null ? null : new AuthenticationHeaderValue("Bearer", sesion.Token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Las comprobaciones definitivas siempre las hace la API al validar el JWT.
        ClaimsPrincipal user = Autenticado ? new(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, Sesion!.Nombre), new Claim(ClaimTypes.Role, Sesion.Rol)
        ], "demo", ClaimTypes.Name, ClaimTypes.Role)) : anonimo;
        return Task.FromResult(new AuthenticationState(user));
    }
}
