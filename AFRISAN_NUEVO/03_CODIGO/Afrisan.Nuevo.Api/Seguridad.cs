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

    public static IServiceCollection AddLimiteLogin(this IServiceCollection services, int porMinuto)
    {
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Demasiados intentos",
                    detail: "Hay demasiados intentos de inicio de sesión desde esta red. Espera un minuto e inténtalo otra vez.")
                    .ExecuteAsync(context.HttpContext);
            };
            o.AddPolicy(PoliticaLogin, http => porMinuto <= 0
                ? RateLimitPartition.GetNoLimiter("sin-limite")
                : RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }
}
