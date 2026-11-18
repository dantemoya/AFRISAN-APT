using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api;

// Se almacenan DTOs inmutables y se devuelven copias de colecciones bajo un lock.
// En la siguiente etapa esto deberá sustituirse por persistencia y transacciones.
public sealed class InventarioStore
{
    private readonly object gate = new();
    internal object SyncRoot => gate;
    private readonly List<RefrigeranteResponse> refrigerantes = [];
    private readonly List<CilindroResponse> cilindros = [];
    private readonly Dictionary<long, List<EventoResponse>> eventos = [];
    private long siguienteRefrigerante = 1;
    private long siguienteCilindro = 1;

    public RefrigeranteResponse[] Refrigerantes() { lock (gate) return refrigerantes.OrderBy(r => r.Codigo).ToArray(); }
    public CilindroResponse[] Cilindros() { lock (gate) return cilindros.OrderByDescending(c => c.Id).ToArray(); }
    public CilindroResponse? Cilindro(string codigo) { lock (gate) return cilindros.Find(c => c.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase)); }
    public EventoResponse[] Eventos(long id) { lock (gate) return eventos.TryGetValue(id, out var lista) ? lista.ToArray() : []; }
    // Consulta de la ficha: primero por código y, si no existe, por número de serie (igual que PostgreSQL).
    public CilindroResponse? Buscar(string texto)
    {
        lock (gate) return Cilindro(texto) ?? cilindros.Find(c => string.Equals(c.NumeroSerie?.Trim(), texto.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    internal void Ubicar(string codigo, string ubicacion, string estado, string? sucursal, string? tecnico,
        string actor, string accion, long? peso = null, string? tipoPesaje = null, string? orden = null, FotoMemoria[]? fotos = null)
    {
        lock (gate)
        {
            var indice = cilindros.FindIndex(c => c.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
            if (indice < 0) throw new ReglaInventarioException("Cilindro no registrado.", 404);
            var c = cilindros[indice];
            // D-48: igual que la base, un pesaje no se registra sin foto; se valida antes de cambiar el cilindro.
            if (peso.HasValue && tipoPesaje is not null && (fotos is null || fotos.Length == 0))
                throw new ReglaInventarioException("Todo pesaje requiere al menos una foto de la balanza.", 409);
            cilindros[indice] = c with { Ubicacion = ubicacion, Estado = estado, Sucursal = sucursal, Tecnico = tecnico,
                UltimoPesaje = peso.HasValue ? new(peso.Value, DateTimeOffset.UtcNow, actor, accion) : c.UltimoPesaje };
            eventos[c.Id].Add(new(DateTimeOffset.UtcNow, accion, actor, $"Ubicación registrada: {ubicacion}. {sucursal}"));
            if (peso.HasValue && tipoPesaje is not null) pesajes.Add(new(Guid.NewGuid(), c.Id, tipoPesaje, peso.Value, DateTimeOffset.UtcNow, actor, orden, fotos!));
        }
    }

    // D-48: cada pesaje con sus fotos, para consultarlo y verificar el monto (misma consulta que PostgreSQL).
    internal sealed record FotoMemoria(Guid Id, string Nombre, string ContentType, byte[] Datos);
    private sealed record PesajeMemoria(Guid Id, long CilindroId, string Tipo, long Peso, DateTimeOffset Fecha, string Usuario, string? Orden, FotoMemoria[] Fotos);
    private readonly List<PesajeMemoria> pesajes = [];

    internal static FotoMemoria Foto(FotoPesajeRequest? foto, string falta)
    {
        if (foto is null) throw new ReglaInventarioException(falta);
        if (foto.Id == Guid.Empty || string.IsNullOrWhiteSpace(foto.Nombre) || foto.Nombre.Length > 150)
            throw new ReglaInventarioException("Identificador o nombre de fotografía inválido.");
        var datos = TrabajoStore.LeerImagen(new() { Id = foto.Id, Nombre = foto.Nombre, ContentType = foto.ContentType, Base64 = foto.Base64 });
        return new(foto.Id, Path.GetFileName(foto.Nombre), foto.ContentType, datos);
    }

    public PesajeEvidenciaResponse[] Pesajes(string codigo)
    {
        lock (gate)
        {
            var c = cilindros.Find(x => x.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ReglaInventarioException("No se encontró el cilindro.", 404);
            return pesajes.Where(p => p.CilindroId == c.Id).OrderByDescending(p => p.Fecha)
                .Select(p => new PesajeEvidenciaResponse(p.Id, p.Tipo, p.Peso, p.Fecha, p.Usuario, p.Orden,
                    p.Fotos.Select(f => new FotoPesajeInfo(f.Id, f.Nombre, f.ContentType)).ToArray())).ToArray();
        }
    }

    public (byte[] Bytes, string Mime) FotoPesaje(string codigo, Guid foto)
    {
        lock (gate)
        {
            var c = cilindros.Find(x => x.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ReglaInventarioException("No se encontró el cilindro.", 404);
            var f = pesajes.Where(p => p.CilindroId == c.Id).SelectMany(p => p.Fotos).FirstOrDefault(x => x.Id == foto)
                ?? throw new ReglaInventarioException("Foto no encontrada.", 404);
            return (f.Datos, f.ContentType);
        }
    }

    public RefrigeranteResponse CrearRefrigerante(CrearRefrigeranteRequest dto)
    {
        lock (gate)
        {
            var codigo = dto.Codigo.Trim().ToUpperInvariant();
            if (codigo.Length == 0 || dto.Nombre.Trim().Length == 0) throw new ReglaInventarioException("Código y nombre no pueden estar vacíos.");
            if (refrigerantes.Any(r => r.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)))
                throw new ReglaInventarioException("Ese refrigerante ya está registrado.", 409);
            var registro = new RefrigeranteResponse(siguienteRefrigerante++, codigo, dto.Nombre.Trim());
            refrigerantes.Add(registro);
            return registro;
        }
    }

    public CilindroResponse CrearCilindro(CrearCilindroRequest dto, string usuario)
    {
        lock (gate)
        {
            var refrigerante = refrigerantes.Find(r => r.Id == dto.RefrigeranteId)
                ?? throw new ReglaInventarioException("Selecciona un refrigerante registrado.");
            var serie = string.IsNullOrWhiteSpace(dto.NumeroSerie) ? null : dto.NumeroSerie.Trim();
            if (serie is not null && cilindros.Any(c => string.Equals(c.NumeroSerie, serie, StringComparison.OrdinalIgnoreCase)))
                throw new ReglaInventarioException("El número de serie ya corresponde a otro cilindro.", 409);
            if (dto.CapacidadGr <= 0 || dto.TaraGr <= 0) throw new ReglaInventarioException("Capacidad y tara deben ser mayores que cero.");
            if (dto.PesoInicialGr.HasValue) ValidarPeso(dto.PesoInicialGr.Value, dto.TaraGr, dto.CapacidadGr);
            var foto = dto.PesoInicialGr.HasValue ? Foto(dto.Foto, "Adjunta la foto de la balanza con el peso inicial.") : null;
            // Un alta de existencias en sucursal no representa una entrada a Bodega.
            if (dto.UbicacionInicial is not ("Bodega" or "Sucursal"))
                throw new ReglaInventarioException("Selecciona Bodega o Sucursal como ubicación inicial.");
            var sucursal = Limpiar(dto.SucursalInicial);
            if (dto.UbicacionInicial == "Sucursal" && string.IsNullOrWhiteSpace(sucursal))
                throw new ReglaInventarioException("Indica la sucursal donde ya está instalado el cilindro.");
            if (dto.UbicacionInicial == "Bodega") sucursal = null;
            var fecha = DateTimeOffset.UtcNow;
            var id = siguienteCilindro++;
            var pesaje = dto.PesoInicialGr is { } peso ? new PesajeResponse(peso, fecha, usuario, Limpiar(dto.Observacion)) : null;
            // D-47: código corto de 8 caracteres, igual que la secuencia de PostgreSQL.
            var registro = new CilindroResponse(id, $"AF-{id:D5}", serie, refrigerante.Id, refrigerante.Codigo, refrigerante.Nombre,
                dto.CapacidadGr, dto.TaraGr, dto.UbicacionInicial == "Sucursal" ? "Instalado" : Estado(pesaje, dto.TaraGr), fecha, pesaje,
                dto.UbicacionInicial, sucursal);
            cilindros.Add(registro);
            eventos[id] = [new(fecha, "Alta", usuario, $"Envase registrado en {dto.UbicacionInicial} {sucursal} con un único tipo de refrigerante.")];
            if (pesaje is not null)
            {
                eventos[id].Add(new(fecha, "Pesaje inicial", usuario, $"Lectura bruta de {pesaje.PesoBrutoGr:N0} g; es un ingreso, no un despacho."));
                pesajes.Add(new(Guid.NewGuid(), id, "Inicial", pesaje.PesoBrutoGr, fecha, usuario, null, [foto!]));
            }
            return registro;
        }
    }

    public CilindroResponse Pesar(string codigo, RegistrarPesajeInicialRequest dto, string usuario)
    {
        lock (gate)
        {
            var indice = cilindros.FindIndex(c => c.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
            if (indice < 0) throw new ReglaInventarioException("No se encontró el cilindro.", 404);
            var cilindro = cilindros[indice];
            if (cilindro.PesajeInicial is not null || cilindro.UltimoPesaje is not null || cilindro.Ubicacion != "Bodega") throw new ReglaInventarioException("El cilindro ya tiene pesajes o está fuera de bodega. El registro inicial no reemplaza el historial operativo.", 409);
            ValidarPeso(dto.PesoBrutoGr, cilindro.TaraGr, cilindro.CapacidadGr);
            var foto = Foto(dto.Foto, "Adjunta la foto de la balanza con el peso inicial.");
            var pesaje = new PesajeResponse(dto.PesoBrutoGr, DateTimeOffset.UtcNow, usuario, Limpiar(dto.Observacion));
            var actualizado = cilindro with { PesajeInicial = pesaje, Estado = Estado(pesaje, cilindro.TaraGr) };
            cilindros[indice] = actualizado;
            eventos[cilindro.Id].Add(new(pesaje.Fecha, "Pesaje inicial", usuario, $"Lectura bruta de {pesaje.PesoBrutoGr:N0} g; es un ingreso, no un despacho."));
            pesajes.Add(new(Guid.NewGuid(), cilindro.Id, "Inicial", pesaje.PesoBrutoGr, pesaje.Fecha, usuario, null, [foto]));
            return actualizado;
        }
    }

    private static string Estado(PesajeResponse? pesaje, long tara) => pesaje is null ? "PendientePesaje" : pesaje.PesoBrutoGr == tara ? "Vacio" : "Disponible";
    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static void ValidarPeso(long peso, long tara, long capacidad)
    {
        if (peso < tara || (decimal)peso > (decimal)tara + capacidad)
            throw new ReglaInventarioException("El peso bruto debe estar entre la tara y la tara más la capacidad. Ingresa gramos enteros.");
    }
}

public sealed class ReglaInventarioException(string mensaje, int estado = 400) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}
