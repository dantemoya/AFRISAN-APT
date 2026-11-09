using Microsoft.AnalysisServices.Tabular;
var db = JsonSerializer.DeserializeDatabase(File.ReadAllText(args[0]));
var resultado=System.Text.Json.JsonSerializer.Serialize(new {fecha_utc=DateTimeOffset.UtcNow,estado="CORRECTO",tipo="Deserialización TMSL con Microsoft.AnalysisServices 19.118.3; no ejecuta DAX ni revisa visuales",tablas=db.Model.Tables.Count,relaciones=db.Model.Relationships.Count,medidas=db.Model.Tables.Sum(t=>t.Measures.Count)});
Console.WriteLine(resultado);
if(args.Length>1) File.WriteAllText(args[1],resultado);
