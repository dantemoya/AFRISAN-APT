using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Afrisan.Nuevo.Api;

// Límite de intentos de inicio de sesión por dirección IP (OWASP A07).
// Complementa el bloqueo de la cuenta tras 5 contraseñas incorrectas: frena el
// recorrido de muchas cuentas desde un mismo origen y el costo de calcular hashes.
public static class Seguridad
{
    public const string PoliticaLogin = "login";

    /// <summary>
    /// Fuera de Development se aplican 10 intentos por minuto. En Development queda
    /// desactivado, salvo que se configure <c>Seguridad:LoginPorMinuto</c>, para no
    /// interferir con las baterías locales que inician muchas sesiones seguidas.
    /// </summary>
    public static int LoginPorMinuto(IConfiguration configuration, IHostEnvironment environment) =>
        configuration.GetValue<int?>("Seguridad:LoginPorMinuto") ?? (environment.IsDevelopment() ? 0 : 10);

    // Límite global por usuario autenticado (o por IP si no hay sesión): frena el abuso de un
    // token válido contra listados y subidas de fotos. Generoso para no molestar al uso normal.
    // En Development queda desactivado para no interferir con las baterías locales.
    public static int ApiPorMinuto(IConfiguration configuration, IHostEnvironment environment) =>
        configuration.GetValue<int?>("Seguridad:ApiPorMinuto") ?? (environment.IsDevelopment() ? 0 : 300);

    public static IServiceCollection AddLimiteLogin(this IServiceCollection services, int porMinuto, int apiPorMinuto = 0)
    {
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Demasiados intentos",
                    detail: "Hay demasiadas solicitudes desde esta sesión o red. Espera un minuto e inténtalo otra vez.")
                    .ExecuteAsync(context.HttpContext);
            };
            o.AddPolicy(PoliticaLogin, http => porMinuto <= 0
                ? RateLimitPartition.GetNoLimiter("sin-limite")
                : RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                // El sondeo de salud no se limita; el resto se parte por usuario o por IP.
                if (apiPorMinuto <= 0 || http.Request.Path.StartsWithSegments("/health"))
                    return RateLimitPartition.GetNoLimiter("sin-limite");
                var clave = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } id
                    ? "u:" + id : "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "desconocido");
                return RateLimitPartition.GetFixedWindowLimiter(clave,
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = apiPorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });
    }
}
