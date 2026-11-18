using System.Net;
using System.Text;
using System.Text.Json;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Api.Persistencia;

namespace Afrisan.Nuevo.Pruebas;

/// <summary>
/// DEF-15: cuando la base rechaza la operación, la API borra la foto que acababa de guardar; nunca la de un
/// reintento, que puede estar en uso. Cubre el disco privado y Supabase Storage (con un servidor simulado).
/// La prueba con PostgreSQL real está en 04_SEGUIMIENTO/DEFECTOS_2026_11_16/validar_defectos.mjs.
/// </summary>
public sealed class FotosRechazadasTests : IDisposable
{
    private static readonly string Png = Convert.ToBase64String([137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82]);
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "afrisan-def15-" + Guid.NewGuid().ToString("N"));
    private readonly Guid ot = Guid.NewGuid();

    public void Dispose()
    {
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
    }

    // La instancia la calcula Inicializar desde la base; aquí se fija para no necesitar PostgreSQL.
    private PersistenciaOptions Opciones()
    {
        var opciones = new PersistenciaOptions { ConnectionString = "Host=no-se-usa", JwtKey = new byte[32], FotosPath = carpeta };
        typeof(PersistenciaOptions).GetProperty(nameof(PersistenciaOptions.Instancia))!.SetValue(opciones, "instancia-de-prueba");
        return opciones;
    }

    private FotosPrivadas Disco() => new(
        Opciones(),
        new StorageOptions { Nube = false, Url = new Uri("https://sin-configurar.supabase.co"), Bucket = "afrisan-evidencias" },
        new SupabaseFotos(new HttpClient(), new StorageOptions { Url = new Uri("https://sin-configurar.supabase.co"), Bucket = "afrisan-evidencias" }));

    private int ArchivosEnDisco() => Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, "*.bin", SearchOption.AllDirectories).Length : 0;

    private static Task<T> Rechazar<T>(FotoArchivo _) => throw new ReglaInventarioException("Cilindro no disponible", 409);

    [Fact]
    public async Task Si_la_base_rechaza_la_operacion_se_borra_la_foto_nueva_del_disco()
    {
        var fotos = Disco();
        var error = await Assert.ThrowsAsync<ReglaInventarioException>(() =>
            fotos.GuardarYUsar(ot, Guid.NewGuid(), "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", null, Rechazar<bool>));
        Assert.Equal("Cilindro no disponible", error.Message);
        Assert.Equal(0, ArchivosEnDisco());
    }

    [Fact]
    public async Task Si_la_operacion_se_acepta_la_foto_se_conserva()
    {
        var fotos = Disco();
        var archivo = await fotos.GuardarYUsar(ot, Guid.NewGuid(), "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", null,
            f => Task.FromResult(f));
        Assert.Equal(1, ArchivosEnDisco());
        Assert.Equal(16, (await fotos.Leer(archivo.Ruta, "image/png", archivo.Sha256)).Bytes.Length);
    }

    [Fact]
    public async Task Un_reintento_rechazado_no_borra_la_foto_que_ya_estaba_guardada()
    {
        var fotos = Disco();
        var id = Guid.NewGuid();
        await fotos.GuardarYUsar(ot, id, "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", null, f => Task.FromResult(f));
        await Assert.ThrowsAsync<ReglaInventarioException>(() =>
            fotos.GuardarYUsar(ot, id, "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", null, Rechazar<bool>));
        Assert.Equal(1, ArchivosEnDisco());
    }

    [Fact]
    public async Task Con_la_foto_registrada_como_existente_un_rechazo_no_la_borra()
    {
        var fotos = Disco();
        var id = Guid.NewGuid();
        var existente = await fotos.GuardarYUsar(ot, id, "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", null, f => Task.FromResult(f));
        await Assert.ThrowsAsync<ReglaInventarioException>(() =>
            fotos.GuardarYUsar(ot, id, "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", existente, Rechazar<bool>));
        Assert.Equal(1, ArchivosEnDisco());
    }

    [Fact]
    public async Task Una_validacion_de_la_foto_que_falla_no_llega_a_la_base_ni_deja_archivo()
    {
        var fotos = Disco();
        var llamado = false;
        await Assert.ThrowsAsync<ReglaInventarioException>(() =>
            fotos.GuardarYUsar(ot, Guid.NewGuid(), "Despacho", "AF-00001", "balanza.png", "image/jpeg", Png, "tecnico@example.invalid", null,
                _ => { llamado = true; return Task.FromResult(true); }));
        Assert.False(llamado);
        Assert.Equal(0, ArchivosEnDisco());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Conflict, false)]
    public async Task En_Storage_se_borra_solo_la_foto_que_esta_solicitud_subio(HttpStatusCode respuestaSubida, bool debeBorrar)
    {
        var storage = new StorageOptions { Nube = true, Url = new Uri("https://proyecto.supabase.co"), Bucket = "afrisan-evidencias", SecretKey = "sb_secret_solo_para_pruebas_0000" };
        var simulado = new StorageSimulado(respuestaSubida, Convert.FromBase64String(Png));
        var fotos = new FotosPrivadas(Opciones(), storage, new SupabaseFotos(new HttpClient(simulado), storage));

        await Assert.ThrowsAsync<ReglaInventarioException>(() =>
            fotos.GuardarYUsar(ot, Guid.NewGuid(), "Despacho", "AF-00001", "balanza.png", "image/png", Png, "tecnico@example.invalid", null, Rechazar<bool>));

        Assert.NotNull(simulado.Subido);
        Assert.Equal(debeBorrar ? 1 : 0, simulado.Borrados.Count);
        if (debeBorrar) Assert.Contains(simulado.Subido!, simulado.Borrados[0]);
        Assert.Equal(0, ArchivosEnDisco());
    }

    /// <summary>Responde como Supabase Storage: bucket privado, subida, lectura autenticada y borrado por prefijos.</summary>
    private sealed class StorageSimulado(HttpStatusCode respuestaSubida, byte[] contenido) : HttpMessageHandler
    {
        public string? Subido { get; private set; }
        public List<string> Borrados { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ruta = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Get && ruta.EndsWith("/bucket/afrisan-evidencias"))
                return Json("""{"id":"afrisan-evidencias","public":false,"file_size_limit":4194304,"allowed_mime_types":["image/png","image/jpeg","image/webp"]}""");
            if (request.Method == HttpMethod.Post && ruta.Contains("/object/afrisan-evidencias/"))
            {
                Subido = ruta[(ruta.IndexOf("/object/afrisan-evidencias/", StringComparison.Ordinal) + "/object/afrisan-evidencias/".Length)..];
                return new HttpResponseMessage(respuestaSubida);
            }
            if (request.Method == HttpMethod.Get && ruta.Contains("/object/authenticated/"))
            {
                var bytes = new ByteArrayContent(contenido);
                bytes.Headers.ContentType = new("image/png");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = bytes };
            }
            if (request.Method == HttpMethod.Delete && ruta.EndsWith("/object/afrisan-evidencias"))
            {
                using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                Borrados.AddRange(json.RootElement.GetProperty("prefixes").EnumerateArray().Select(p => p.GetString()!));
                return Json("[]");
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string texto) =>
            new(HttpStatusCode.OK) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };
    }
}
