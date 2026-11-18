#:package Npgsql@8.0.9
#:property JsonSerializerIsReflectionEnabledByDefault=true
// CDP-61: respaldo lógico del esquema afrisan de Supabase, de solo lectura y en una instantánea consistente.
// Exporta cada tabla a CSV (con encabezado) y deja un manifiesto con filas y SHA-256 de cada archivo.
// La conexión de propietario se lee de los User Secrets de la API y no se muestra.
// El respaldo contiene datos personales y hashes de contraseñas: la carpeta de salida debe quedar fuera de Git.
// Uso: dotnet run respaldo.cs -- <carpeta de salida>
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

var salida = args.Length > 0 ? args[0] : throw new ArgumentException("Indica la carpeta de salida.");
Directory.CreateDirectory(salida);
var secretos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "Afrisan-Nuevo-Api-Supabase-2026", "secrets.json");
using var json = JsonDocument.Parse(File.ReadAllText(secretos));
var conexion = json.RootElement.GetProperty("ConnectionStrings:Supabase").GetString()!;
var reloj = Stopwatch.StartNew();
await using var cn = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(conexion) { ApplicationName = "afrisan-respaldo" }.ConnectionString);
await cn.OpenAsync();
await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
await using (var ro = new NpgsqlCommand("SET TRANSACTION READ ONLY", cn, tx)) await ro.ExecuteNonQueryAsync();
var tablas = new List<string>();
await using (var cmd = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='afrisan' ORDER BY tablename", cn, tx))
await using (var r = await cmd.ExecuteReaderAsync()) while (await r.ReadAsync()) tablas.Add(r.GetString(0));
var manifiesto = new List<object>();
long total = 0;
foreach (var t in tablas)
{
    var columnas = new List<string>();
    await using (var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_schema='afrisan' AND table_name=@t AND is_generated='NEVER' ORDER BY ordinal_position", cn, tx))
    {
        cmd.Parameters.AddWithValue("t", t);
        await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) columnas.Add(r.GetString(0));
    }
    var lista = string.Join(",", columnas.Select(c => "\"" + c.Replace("\"", "\"\"") + "\""));
    var archivo = Path.Combine(salida, t + ".csv");
    using (var lector = await cn.BeginTextExportAsync($"COPY (SELECT {lista} FROM afrisan.\"{t}\" ORDER BY 1) TO STDOUT (FORMAT csv, HEADER)"))
        await File.WriteAllTextAsync(archivo, await lector.ReadToEndAsync(), new UTF8Encoding(false));
    long filas;
    await using (var cmd = new NpgsqlCommand($"SELECT count(*) FROM afrisan.\"{t}\"", cn, tx)) filas = (long)(await cmd.ExecuteScalarAsync())!;
    total += filas;
    manifiesto.Add(new { tabla = t, filas, columnas, sha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(archivo))) });
}
DateTimeOffset instante;
await using (var cmd = new NpgsqlCommand("SELECT now()", cn, tx)) instante = (DateTimeOffset)(DateTime)(await cmd.ExecuteScalarAsync())!;
await tx.CommitAsync();
reloj.Stop();
await File.WriteAllTextAsync(Path.Combine(salida, "manifiesto.json"), JsonSerializer.Serialize(new { instante_utc = instante, segundos = reloj.Elapsed.TotalSeconds, tablas = manifiesto.Count, filas = total, detalle = manifiesto }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Respaldo: {manifiesto.Count} tablas, {total} filas, {reloj.Elapsed.TotalSeconds:F1} s.");
