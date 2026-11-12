using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

namespace Afrisan.Nuevo.Api.PowerBi;

/// <summary>
/// Arma el proyecto de Power BI (PBIP) ya conectado a la API: en vez de leer CSV, lee las tablas de
/// /api/v1/power-bi/tablas con un token de solo lectura incluido. El usuario solo descomprime, abre
/// AFRISAN.pbip y pulsa Actualizar.
/// </summary>
public static class PowerBiProyectoConectado
{
    private const string PrefijoInforme = "AFRISAN.Informe.";
    private const string Carpeta = "AFRISAN_PowerBI_Conectado/";

    public static byte[] Crear(string urlTablas, string token, DateTimeOffset vence)
    {
        var assembly = typeof(PowerBiProyectoConectado).Assembly;
        string Leer(string recurso)
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(recurso)
                ?? throw new InvalidOperationException($"Falta el recurso {recurso}."));
            return reader.ReadToEnd();
        }

        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Texto(string nombre, string contenido)
            {
                using var stream = zip.CreateEntry(Carpeta + nombre).Open();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(contenido);
            }

            foreach (var recurso in assembly.GetManifestResourceNames().Where(n => n.StartsWith(PrefijoInforme, StringComparison.Ordinal)))
            {
                var nombre = recurso[PrefijoInforme.Length..].Replace('\\', '/');
                var contenido = Leer(recurso);
                if (nombre.EndsWith("model.bim", StringComparison.Ordinal))
                    contenido = Conectar(contenido, urlTablas, token, Leer("AFRISAN.Conexion.AfrisanApi.pq"));
                Texto(nombre, contenido);
            }
            Texto("LEEME.txt", $"""
                AFRISAN - Power BI conectado a la API

                1. Descomprime esta carpeta fuera del repositorio (contiene un token de acceso).
                2. Con la API encendida, abre AFRISAN.pbip en Power BI Desktop.
                3. Pulsa Actualizar. Si pide credenciales para la dirección de la API, elige "Anónimo",
                   aplica la configuración a la dirección completa (la que termina en /tablas/) y pulsa
                   Conectar. Si pregunta por la privacidad, elige "Organizativo".
                4. Si aparece un error 401, el token ya no sirve (venció, cambió la contraseña de la cuenta
                   o la API se reinició en modo de revisión): descarga el proyecto otra vez.

                Origen de los datos: {urlTablas}
                El token solo permite leer las tablas y vence el {vence.ToLocalTime():dd/MM/yyyy HH:mm}.
                Para renovarlo, descarga otra vez el proyecto desde AFRISAN > Power BI.
                No compartas esta carpeta ni el PBIX que guardes desde ella.
                """);
        }
        return output.ToArray();
    }

    /// <summary>Reemplaza las consultas que leen CSV por la consulta que lee la API con el token.</summary>
    public static string Conectar(string modelBim, string urlTablas, string token, string consultaApi)
    {
        var raiz = JsonNode.Parse(modelBim) ?? throw new InvalidOperationException("model.bim vacío.");
        var modelo = raiz["model"]!.AsObject();
        static string Texto(string valor) => "\"" + valor.Replace("\"", "\"\"") + "\"";
        const string Meta = " meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]";
        modelo["expressions"] = new JsonArray(
            new JsonObject { ["name"] = "UrlApi", ["kind"] = "m", ["expression"] = Texto(urlTablas) + Meta },
            new JsonObject { ["name"] = "TokenPowerBi", ["kind"] = "m", ["expression"] = Texto(token) + Meta },
            new JsonObject { ["name"] = "Afrisan", ["kind"] = "m", ["expression"] = consultaApi.Replace("\r\n", "\n") });
        if (modelo["annotations"] is JsonArray anotaciones)
            foreach (var anotacion in anotaciones.OfType<JsonObject>())
                if (anotacion["name"]?.GetValue<string>() == "PBI_QueryOrder" && anotacion["value"] is JsonValue valor)
                    anotacion["value"] = valor.GetValue<string>().Replace("\"CarpetaAfrisan\"", "\"UrlApi\",\"TokenPowerBi\"");
        return raiz.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
