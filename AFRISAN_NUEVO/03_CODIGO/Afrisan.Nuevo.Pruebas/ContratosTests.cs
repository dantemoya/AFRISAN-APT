using System.ComponentModel.DataAnnotations;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// Validaciones compartidas entre la API y la web (contratos DTO).
public sealed class ContratosTests
{
    private static bool Valido(object dto) => Validator.TryValidateObject(dto, new ValidationContext(dto), [], true);

    [Fact]
    public void Contrasena_de_restablecimiento_exige_entre_12_y_128_caracteres()
    {
        Assert.False(Valido(new RestablecerClaveRequest { Password = new string('a', 11) }));
        Assert.True(Valido(new RestablecerClaveRequest { Password = new string('a', 12) }));
        Assert.False(Valido(new RestablecerClaveRequest { Password = new string('a', 129) }));
    }

    [Fact]
    public void Alta_de_usuario_exige_correo_valido_y_rol()
    {
        var valido = new CrearUsuarioRequest { Nombre = "Ana", Apellido = "Pérez", Correo = "ana@afrisan.example", Rol = "Bodega", Password = "ContraseñaSegura1" };

        Assert.True(Valido(valido));
        Assert.False(Valido(valido with { Correo = "no-es-correo" }));
        Assert.False(Valido(valido with { Rol = "" }));
    }

    [Fact]
    public void Roles_definidos_cubren_los_cinco_perfiles()
    {
        Assert.Equal(new[] { "Bodega", "Autorizador", "Jefatura", "Tecnico", "Administrador" }, Roles.Todos);
        Assert.DoesNotContain("Tecnico", Roles.Lectura.Split(','));
    }

    [Theory]
    [InlineData("https://app.powerbi.com/view?r=abc123", "https://app.powerbi.com/view?r=abc123")]
    [InlineData("  https://APP.powerbi.com/view?r=abc123&pageName=x  ", "https://app.powerbi.com/view?r=abc123")]
    [InlineData("https://app.powerbi.com/view?r=a%2Bb%3D", "https://app.powerbi.com/view?r=a%2Bb%3D")]
    public void Enlace_publico_de_Power_BI_se_normaliza(string entrada, string esperado) =>
        Assert.Equal(esperado, EnlacePowerBiPublico.Normalizar(entrada));

    [Theory]
    [InlineData("http://app.powerbi.com/view?r=abc")]
    [InlineData("https://evil.example/view?r=abc")]
    [InlineData("https://app.powerbi.com/reportEmbed?reportId=abc")]
    [InlineData("https://app.powerbi.com/view?r=abc&r=def")]
    [InlineData("https://user:clave@app.powerbi.com/view?r=abc")]
    [InlineData("https://app.powerbi.com/view?r=<script>")]
    public void Enlace_que_no_es_Publicar_en_la_web_se_rechaza(string entrada) =>
        Assert.Throws<ValidationException>(() => EnlacePowerBiPublico.Normalizar(entrada));

    [Fact]
    public void Enlace_vacio_retira_la_configuracion()
    {
        Assert.Null(EnlacePowerBiPublico.Normalizar(null));
        Assert.Null(EnlacePowerBiPublico.Normalizar("   "));
    }
}
