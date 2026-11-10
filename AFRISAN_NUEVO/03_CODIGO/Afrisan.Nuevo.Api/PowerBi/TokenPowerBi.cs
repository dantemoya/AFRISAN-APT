using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Afrisan.Nuevo.Api.Persistencia;
using Afrisan.Nuevo.Contratos;
using Microsoft.IdentityModel.Tokens;

namespace Afrisan.Nuevo.Api.PowerBi;

// Conexión directa de Power BI a la API (D-32). La sesión normal sigue durando 2 horas;
// este token dura 30 días, pero solo permite leer las tablas de Power BI.
// Hereda la "version" de la cuenta: cambiar su contraseña, su rol o desactivarla lo revoca.
public static class TokenPowerBi
{
    public const string Alcance = "alcance";
    public const string Valor = "powerbi";
    public const string RutaTablas = "/api/v1/power-bi/tablas";
    public static readonly TimeSpan Duracion = TimeSpan.FromDays(30);
    public static readonly string[] Tablas = ["inventario", "ordenes", "detalle", "operaciones", "despachos", "incidencias", "costos"];

    // Datos de la cuenta que se copian de la sesión ya validada. No se copia el correo.
    private static readonly string[] Copiados = [ClaimTypes.NameIdentifier, ClaimTypes.Name, ClaimTypes.Role, "version"];

    public static bool EsTokenPowerBi(ClaimsPrincipal usuario) => usuario.HasClaim(Alcance, Valor);

    public static bool Permitido(HttpRequest solicitud) =>
        HttpMethods.IsGet(solicitud.Method) && solicitud.Path.StartsWithSegments(RutaTablas);

    public static TokenPowerBiResponse Emitir(ClaimsPrincipal sesion, byte[] firma, string emisor, string audiencia, DateTimeOffset ahora)
    {
        var claims = sesion.Claims.Where(c => Copiados.Contains(c.Type)).Select(c => new Claim(c.Type, c.Value)).ToList();
        claims.Add(new(Alcance, Valor));
        claims.Add(new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        var vence = ahora.Add(Duracion);
        var jwt = new JwtSecurityToken(emisor, audiencia, claims, notBefore: ahora.UtcDateTime, expires: vence.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(firma), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), vence, Tablas);
    }

    /// <summary>Un token de Power BI solo puede hacer GET a /api/v1/power-bi/tablas/…; todo lo demás responde 403.</summary>
    public static IApplicationBuilder UseAlcancePowerBi(this IApplicationBuilder app) => app.Use(async (contexto, siguiente) =>
    {
        if (EsTokenPowerBi(contexto.User) && !Permitido(contexto.Request))
        {
            await Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acceso no permitido",
                detail: "El token de Power BI solo permite leer las tablas de /api/v1/power-bi/tablas.").ExecuteAsync(contexto);
            return;
        }
        await siguiente(contexto);
    });

    public static void MapConexionPowerBi(this WebApplication app, bool persistente, byte[] firma, string emisor, string audiencia)
    {
        var grupo = app.MapGroup("/api/v1/power-bi").RequireAuthorization(p => p.RequireRole(RolesBi.Consulta.Split(',')));
        grupo.AddEndpointFilter(async (contexto, siguiente) =>
        {
            contexto.HttpContext.Response.Headers.CacheControl = "no-store";
            contexto.HttpContext.Response.Headers.XContentTypeOptions = "nosniff";
            return await siguiente(contexto);
        });
        grupo.MapPost("/token", (ClaimsPrincipal usuario, ILoggerFactory registros) =>
        {
            var token = Emitir(usuario, firma, emisor, audiencia, DateTimeOffset.UtcNow);
            // Se registra quién lo pidió y cuándo vence; nunca el token.
            registros.CreateLogger("AFRISAN.PowerBi").LogInformation("Token de Power BI emitido para la cuenta {Cuenta}; vence {Vence:O}.",
                usuario.FindFirstValue(ClaimTypes.NameIdentifier), token.Vence);
            return token;
        });
        grupo.MapGet("/tablas/{nombre}", async (string nombre, HttpContext contexto, ClaimsPrincipal usuario) =>
        {
            // El modo memoria no tiene datos para Power BI: responde las tablas vacías.
            var datos = persistente
                ? await contexto.RequestServices.GetRequiredService<PgPowerBi>().Datos(PgDatabase.Actor(usuario))
                : new BiDatos(DateTimeOffset.UtcNow, [], [], [], [], [], [], []);
            return Tabla(datos, nombre);
        });
    }

    public static IResult Tabla(BiDatos datos, string nombre) => nombre.ToLowerInvariant() switch
    {
        "inventario" => Results.Ok(datos.Inventario),
        "ordenes" => Results.Ok(datos.Ordenes),
        "detalle" => Results.Ok(datos.Detalle),
        "operaciones" => Results.Ok(datos.Operaciones),
        "despachos" => Results.Ok(datos.Despachos),
        "incidencias" => Results.Ok(datos.Incidencias),
        "costos" => Results.Ok(datos.Costos ?? []),
        _ => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tabla no disponible",
            detail: "Usa una de estas tablas: " + string.Join(", ", Tablas) + ".")
    };
}
