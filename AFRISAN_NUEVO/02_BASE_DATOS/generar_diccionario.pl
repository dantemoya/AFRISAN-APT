#!/usr/bin/perl
# Genera DICCIONARIO_ESQUEMA_AFRISAN.md a partir del SQL vigente.
# Uso (desde 02_BASE_DATOS, con el Perl de Git para Windows):
#   perl generar_diccionario.pl > DICCIONARIO_ESQUEMA_AFRISAN.md
# Lee 05_Crear_Afrisan_Sin_Emisiones.sql (mismo esquema que 06) y las migraciones
# aditivas 07 a 10, en el orden en que se aplican.
# Solo lee archivos: no se conecta a ninguna base de datos.
use strict;
use warnings;
use utf8;
use open qw(:std :encoding(UTF-8));
use File::Basename qw(dirname);

my $dir = dirname(__FILE__);
sub leer { my ($f) = @_; open(my $h, '<:encoding(UTF-8)', "$dir/$f") or die "No se pudo leer $f: $!"; local $/; my $t = <$h>; close $h; $t =~ s/\r//g; $t =~ s/^\x{FEFF}//; $t =~ s/^\s*--.*$//mg; return $t; }
my $sql05 = leer('05_Crear_Afrisan_Sin_Emisiones.sql');
my $sql07 = leer('07_Habilitar_Persistencia.sql');
# Migraciones posteriores: número => contenido.
my @migraciones = (['08', leer('08_Rectificacion_Pesajes.sql')], ['09', leer('09_Mantenedores_Bajas.sql')], ['10', leer('10_Costos_Limites.sql')]);

# Descripción funcional de cada tabla (redactada a partir de las reglas del SQL).
my %desc = (
  'Rol' => 'Catálogo de los cinco roles de negocio: Administrador, Jefatura, Autorizador, Bodega y Tecnico.',
  'Usuario' => 'Cuentas reales. La API guarda solo el hash de la contraseña (PBKDF2), bloquea tras intentos fallidos y usa `VersionSeguridad` para invalidar sesiones.',
  'Cliente' => 'Empresas cliente de AFRISAN, identificadas por RUT.',
  'Sucursales' => 'Sucursales de cada cliente, donde quedan instalados los cilindros.',
  'Refrigerantes' => 'Catálogo de tipos de gas. El código es único sin distinguir mayúsculas.',
  'Cilindros' => 'Cada envase físico: código QR, un solo refrigerante, tara y capacidad en gramos. La ubicación y el estado deben ser coherentes (CHECK) y solo cambian mediante los registros operativos (trigger).',
  'OrdenTrabajo' => 'OT creada por el Autorizador y asignada a un técnico. Estados: Asignada → EnProcesoDespacho → EnTerreno → PendienteBodega → PendienteRevision → Cerrada.',
  'DetalleOT' => 'Refrigerantes y gramos solicitados y autorizados por cada OT. Inmutable después de la asignación.',
  'RetiroPrevisto' => 'Cilindros instalados en la sucursal que la OT prevé retirar. Un cilindro solo puede estar reservado en una OT a la vez.',
  'Pesaje' => 'Toda lectura de balanza en gramos: Inicial, Despacho, Instalado, Retirado o Recepcion. Inmutable.',
  'EvidenciaPesaje' => 'Metadatos de cada fotografía de balanza guardada en Storage privado (ruta, tipo, tamaño y SHA-256). Máximo ocho por pesaje. Inmutable.',
  'Despacho' => 'Retiro de un cilindro desde Bodega por el técnico asignado, con su pesaje y foto de salida. Se crea con la función `afrisan.retirar_cilindro`.',
  'OperacionTerreno' => 'Trabajo en la sucursal: Reemplazo (instalado y retirado) o Agregado (solo instalado). Es un borrador versionado hasta que el técnico lo confirma.',
  'CustodiaActual' => 'Proyección de qué cilindros tiene cada técnico y para qué OT. Solo la modifican los triggers de despacho, operación y recepción.',
  'RecepcionBodega' => 'Recepción oficial de Bodega con su propio pesaje, ya sea del cilindro retirado o de un despacho no usado. Inmutable.',
  'RevisionOT' => 'Revisiones obligatorias en orden Bodega → Autorizador; una por etapa. Inmutable.',
  'Incidencia' => 'Incidencias o rectificaciones registradas por el técnico o Bodega, con la respuesta del Autorizador.',
  'HistorialCilindro' => 'Cada cambio de ubicación o estado de un cilindro, registrado por trigger. Inmutable.',
  'EventoOT' => 'Bitácora de eventos de cada OT. Inmutable.',
  'RectificacionPesaje' => 'Solicitud de corrección de un pesaje confirmado (P05): peso anterior y propuesto, motivo, foto privada y resolución del Autorizador. El pesaje original no cambia: la vista `PesajeVigente` aplica la última corrección aprobada. Una OT con solicitudes pendientes no se cierra. No se borra.',
  'TiposCambio' => 'Valores manuales de CLP por una unidad de EUR, USD o UF, con su fecha de valor. No se consulta un proveedor de mercado. Histórico: no se modifica ni se borra.',
  'LimiteSucursal' => 'Límite semanal o mensual de gramos por sucursal. Desde `10` se define por gas, con el criterio de gramos instalados confirmados en el calendario chileno; Jefatura puede bloquear la edición por Bodega. Versionado.',
  'LimiteSucursalHistorial' => 'Versiones de cada límite, generadas por trigger. Inmutable.',
  'MedicionLimite' => 'Cómputo declarado de gramos en un período contra una versión del límite, con metodología explícita.',
  'MedicionLimiteOperaciones' => 'Operaciones de terreno incluidas en una medición de límite.',
  'RevisionExceso' => 'Conclusión de Jefatura sobre un exceso declarado.',
  'PreciosRefrigerantes' => 'Precio por gramo de cada refrigerante, con moneda y fecha de vigencia.',
  'Costos' => 'Valoración por líneas de una operación registrada. `PrecioTotal` se calcula como Cantidad × TarifaUnitaria; desde `10`, `TotalClp` aplica el factor de cambio elegido. Histórico.',
  'CertificadosDisposicion' => 'Lotes entregados para disposición final y el certificado recibido después.',
  'CertificadosCilindros' => 'Cilindros clasificados como Chatarra que forman parte de un lote de disposición.',
  'Auditoria' => 'Registro automático de cada INSERT, UPDATE y DELETE de las demás tablas (sin hash de contraseña). Inmutable.',
  'BajaCilindro' => 'Baja administrativa de un cilindro (Chatarra o Inactivo) con motivo. Inmutable.',
);
my %descVista = (
  'Inventario' => 'Cada cilindro con su último pesaje y el contenido neto estimado (PesoBruto − Tara).',
  'ResumenOT' => 'Cada OT con la cantidad de cilindros despachados y de operaciones confirmadas.',
  'PesajesRecepcion' => 'Peso recibido por Bodega frente al peso de terreno o de salida, y su diferencia en gramos.',
  'LimitesVigentes' => 'Última versión vigente de cada límite por sucursal.',
  'ExcesosRegistrados' => 'Mediciones de límite con el exceso declarado y la conclusión de Jefatura.',
  'CostosOperaciones' => 'Líneas de costo con su OT y sucursal.',
  'DisposicionesCertificadas' => 'Cilindros por lote de disposición y estado del certificado.',
  'PedidoYEjecucion' => 'Gramos solicitados y autorizados por OT y refrigerante, frente al contenido despachado e instalado.',
  'PesajeVigente' => 'Cada pesaje con el peso vigente: el original o la última rectificación aprobada (`security_invoker`). La API la usa para inventario, límites, costos y Power BI.',
);

sub bal {   # devuelve el contenido entre el paréntesis en $pos y su par, y la posición final
  my ($s, $pos) = @_; my ($d, $q) = (0, '');
  for (my $i = $pos; $i < length $s; $i++) {
    my $c = substr($s, $i, 1);
    if ($q) { $q = '' if $c eq $q; next; }
    if ($c eq "'" || $c eq '"') { $q = $c; next; }
    $d++ if $c eq '('; $d-- if $c eq ')';
    return (substr($s, $pos + 1, $i - $pos - 1), $i) if $d == 0;
  }
  die "Paréntesis sin cerrar";
}
sub partir {   # separa por comas de primer nivel
  my ($s) = @_; my @p; my ($d, $q, $cur) = (0, '', '');
  for my $c (split //, $s) {
    if ($q) { $q = '' if $c eq $q; $cur .= $c; next; }
    if ($c eq "'" || $c eq '"') { $q = $c; }
    $d++ if $c eq '('; $d-- if $c eq ')';
    if ($c eq ',' && $d == 0) { push @p, $cur; $cur = ''; next; }
    $cur .= $c;
  }
  push @p, $cur if $cur =~ /\S/;
  return map { my $x = $_; $x =~ s/\s+/ /g; $x =~ s/^ | $//g; $x } @p;
}
sub checks { my ($s) = @_; my @r; while ($s =~ /\bCHECK\s*\(/g) { my $p = pos($s) - 1; my ($c, $e) = bal($s, $p); push @r, $c; pos($s) = $e + 1; } return @r; }
sub cols { my ($s) = @_; $s =~ s/"//g; $s =~ s/\s*,\s*/, /g; return $s; }
sub md { my ($s) = @_; $s =~ s/\|/\\|/g; return $s; }

my (@orden, %tabla, %fkc);
sub fkCompuesta {   # FK de tabla (varias columnas): se lista y se marca en cada columna
  my ($t, $origen, $destino, $refs) = @_;
  push @{$tabla{$t}{fks}}, '(' . cols($origen) . ") → `$destino` (" . cols($refs) . ')';
  $fkc{$t}{$_} //= $destino for split /, /, cols($origen);
}
sub columna {
  my ($t, $def, $origen) = @_;
  my ($nombre, $resto) = $def =~ /^"([^"]+)"\s*(.*)$/ or die "Columna no reconocida: $def";
  my ($tipo) = $resto =~ /^(\w+(?:\s*\([^)]*\))?)/; $tipo =~ s/\s+//g;
  my %c = (nombre => $nombre, tipo => $tipo, notas => []);
  $c{pk} = $resto =~ /PRIMARY KEY/;
  $c{nn} = $c{pk} || $resto =~ /NOT NULL/;
  $c{uq} = $resto =~ /\bUNIQUE\b/;
  push @{$c{notas}}, 'Identity' if $resto =~ /GENERATED ALWAYS AS IDENTITY/;
  if ($resto =~ /GENERATED ALWAYS AS \(/ && $resto =~ /STORED/) { my $p = index($resto, 'AS (') + 3; my ($e) = bal($resto, $p); push @{$c{notas}}, 'Calculada: `' . cols($e) . '`'; }
  push @{$c{notas}}, "Default `$1`" if $resto =~ /DEFAULT\s+('[^']*'|[\w.]+(?:\(\))?)/;
  if ($resto =~ /REFERENCES afrisan\."(\w+)"/) { $c{fk} = $1; push @{$tabla{$t}{fks}}, "`$nombre` → `$1`"; }
  push @{$c{notas}}, 'CHECK `' . cols($_) . '`' for checks($resto);
  push @{$c{notas}}, "Agregada por `$origen`" if $origen;
  push @{$tabla{$t}{cols}}, \%c;
}

# Tablas del script 05 y tablas nuevas de las migraciones (CREATE TABLE IF NOT EXISTS)
sub tablas {
  my ($sql, $origen) = @_;
  while ($sql =~ /CREATE TABLE (?:IF NOT EXISTS )?afrisan\."(\w+)"\s*\(/g) {
    my $t = $1; my $p = pos($sql) - 1; my ($cuerpo, $fin) = bal($sql, $p); pos($sql) = $fin + 1;
    push @orden, $t; $tabla{$t} = { cols => [], fks => [], uq => [], ck => [], idx => [], trg => [], origen => $origen };
    for my $e (partir($cuerpo)) {
      if ($e =~ /^"/) { columna($t, $e); next; }
      if ($e =~ /^PRIMARY KEY\s*\(([^)]*)\)/) { $tabla{$t}{pkc} = cols($1); next; }
      if ($e =~ /^UNIQUE\s*\(([^)]*)\)/) { push @{$tabla{$t}{uq}}, cols($1); next; }
      if ($e =~ /^FOREIGN KEY\s*\(([^)]*)\)\s*REFERENCES afrisan\."(\w+)"\s*\(([^)]*)\)/) { fkCompuesta($t, $1, $2, $3); next; }
      if ($e =~ /^CHECK\s*\(/) { push @{$tabla{$t}{ck}}, cols($_) for checks($e); next; }
      die "Restricción no reconocida en $t: $e";
    }
  }
}
tablas($sql05, '');
tablas($_->[1], $_->[0]) for @migraciones;
# FK agregadas después de crear las tablas
while ($sql05 =~ /ALTER TABLE afrisan\."(\w+)" ADD FOREIGN KEY\s*\(([^)]*)\)\s*REFERENCES afrisan\."(\w+)"\s*\(([^)]*)\)/g) {
  fkCompuesta($1, $2, $3, $4);
}
# Columnas y restricciones modificadas por 07 a 10
for my $m (['07', $sql07], @migraciones) {
  my ($origen, $sql) = @$m;
  while ($sql =~ /ALTER TABLE afrisan\."(\w+)"\s+(.*?);/sg) {
    my ($t, $resto) = ($1, $2);
    for my $e (partir($resto)) {
      if ($e =~ /^ADD COLUMN IF NOT EXISTS (.*)$/) { columna($t, $1, $origen); next; }
      if ($e =~ /^DROP CONSTRAINT IF EXISTS "\Q$t\E_(\w+)_key"$/) {   # único eliminado: "Tabla_col1_col2_key"
        my $c = $1;
        my ($quitado) = grep { (my $x = $_) =~ s/, /_/g; $x eq $c } @{$tabla{$t}{uq}} or die "No se encontró el único $c de $t";
        @{$tabla{$t}{uq}} = grep { $_ ne $quitado } @{$tabla{$t}{uq}};
        push @{$tabla{$t}{cambios}}, "`$origen` elimina el único `($quitado)`";
        next;
      }
      if ($e =~ /^DROP CONSTRAINT IF EXISTS "\Q$t\E_(\w+)_check"$/) {   # CHECK de columna reemplazado
        my ($col) = grep { $_->{nombre} eq $1 } @{$tabla{$t}{cols}} or die "No se encontró la columna $1 de $t";
        @{$col->{notas}} = grep { !/^CHECK / } @{$col->{notas}};
        next;
      }
      if ($e =~ /^ADD CONSTRAINT "\w+" CHECK\s*\(/) { push @{$tabla{$t}{ck}}, cols($_) . " (desde `$origen`)" for checks($e); next; }
    }
  }
}
# Índices únicos y de consulta
for my $m (['', $sql05], @migraciones) {
  my ($origen, $sql) = @$m;
  while ($sql =~ /CREATE (UNIQUE )?INDEX (?:IF NOT EXISTS )?"(\w+)" ON afrisan\."(\w+)"\s*\(/g) {
    my ($u, $n, $t) = ($1, $2, $3); my $p = pos($sql) - 1; my ($e, $fin) = bal($sql, $p);
    my $resto = substr($sql, $fin + 1, 200); my ($w) = $resto =~ /^\s*(WHERE [^;]+);/; pos($sql) = $fin + 1;
    push @{$tabla{$t}{idx}}, ($u ? 'Único' : 'Consulta') . " `$n`: `" . cols($e) . '`' . ($w ? ' ' . '`' . cols($w) . '`' : '') . ($origen ? " (desde `$origen`)" : '');
  }
}
# Triggers declarados y generados en bloques DO
my $plano = $sql05; $plano =~ s/\s+/ /g;
my %trgVisto;
sub trigger { my ($t, $texto) = @_; push @{$tabla{$t}{trg}}, $texto unless $trgVisto{$t}{$texto}++; }
while ($plano =~ /CREATE TRIGGER "(\w+)" (BEFORE|AFTER) ([A-Z ]+?) ON afrisan\."(\w+)"/g) { trigger($4, "`$1` ($2 " . join('/', split / OR /, $3) . ')'); }
for my $m (@migraciones) {
  my ($origen, $sql) = @$m; (my $p = $sql) =~ s/\s+/ /g;
  while ($p =~ /CREATE TRIGGER "(\w+)" (BEFORE|AFTER) ([A-Z ]+?) ON afrisan\."(\w+)"/g) { trigger($4, "`$1` ($2 " . join('/', split / OR /, $3) . ", desde `$origen`)"); }
  while ($p =~ /FOREACH t IN ARRAY ARRAY\[([^\]]+)\] LOOP (?:EXECUTE format\('DROP TRIGGER[^;]*; )?EXECUTE format\('CREATE TRIGGER "(\w+)" (BEFORE|AFTER) ([A-Z ]+?) ON afrisan\.%I/g) {
    my ($lista, $n, $c, $ev) = ($1, $2, $3, $4);
    trigger($_, "`$n` ($c " . join('/', split / OR /, $ev) . ", desde `$origen`)") for $lista =~ /'(\w+)'/g;
  }
}
if ($plano =~ /FOREACH t IN ARRAY ARRAY\[([^\]]+)\] LOOP EXECUTE format\('CREATE TRIGGER %I BEFORE UPDATE OR DELETE ON afrisan\.%I FOR EACH ROW EXECUTE FUNCTION afrisan\.inmutable\(\)'/) {
  for my $t ($1 =~ /'(\w+)'/g) { trigger($t, "`Inmutable_$t` (BEFORE UPDATE/DELETE)"); }
}
if ($plano =~ /FOREACH t IN ARRAY ARRAY\[([^\]]+)\] LOOP EXECUTE format\('CREATE TRIGGER %I BEFORE INSERT OR UPDATE OR DELETE ON afrisan\.%I FOR EACH ROW EXECUTE FUNCTION afrisan\.validar_soporte\(\)'/) {
  for my $t ($1 =~ /'(\w+)'/g) { trigger($t, "`TR_${t}_Validar` (BEFORE INSERT/UPDATE/DELETE)"); }
}
die "No se encontró el bloque de auditoría" unless $plano =~ /'Auditar_'\|\|t\.tablename/;
# El bloque de auditoría de 05 recorre las tablas existentes en ese momento; las de 08-10 declaran su propio trigger.
for my $t (grep { !$tabla{$_}{origen} } @orden) { trigger($t, "`Auditar_$t` (AFTER INSERT/UPDATE/DELETE)") unless $t eq 'Auditoria'; }

# Permisos del rol afrisan_api (07 a 10)
my (%privs, %desde, %politica);
my @ordenPriv = qw(SELECT INSERT UPDATE DELETE);
sub otorgar { my ($t, $priv, $origen) = @_; $privs{$t}{$_} = 1 for split /\s*,\s*/, $priv; $desde{$t} //= $origen; }
for my $m (['07', $sql07], @migraciones) {
  my ($origen, $sql) = @$m; (my $p = $sql) =~ s/\s+/ /g;
  while ($p =~ /GRANT ([A-Z, ]+?) ON ((?:afrisan\."\w+",?\s*)+) TO afrisan_api/g) {
    my ($priv, $lista) = ($1, $2); otorgar($_, $priv, $origen) for $lista =~ /afrisan\."(\w+)"/g;
  }
  while ($p =~ /FOREACH t IN ARRAY ARRAY\[([^\]]+)\] LOOP (.*?)END LOOP/g) {   # bloques DO con GRANT y políticas por tabla
    my ($lista, $cuerpo) = ($1, $2); my @t = $lista =~ /'(\w+)'/g;
    if ($cuerpo =~ /GRANT ([A-Z,]+) ON afrisan\.%I TO afrisan_api/) { my $priv = $1; otorgar($_, $priv, $origen) for @t; }
    if ($cuerpo =~ /CREATE POLICY "BackendConfiable"/) { $politica{$_} = 1 for @t; }
  }
  $politica{$1} = 1 while $p =~ /CREATE POLICY "BackendConfiable" ON afrisan\."(\w+)" TO afrisan_api/g;
}
my %permiso = map { my $t = $_; ($t => join(', ', grep { $privs{$t}{$_} } @ordenPriv)) } keys %privs;
die "No se encontró el bloque de políticas de 07" unless $politica{'Usuario'};

# Vistas: las de 05 y las creadas o reemplazadas por las migraciones
my (@vistas, %baseVista, %vistaOrigen);
sub vista {
  my ($v, $cuerpo, $origen) = @_; my %b; $b{$_} = 1 for ($cuerpo =~ /afrisan\."(\w+)"/g);
  unless (exists $baseVista{$v}) { push @vistas, $v; $vistaOrigen{$v} = $origen; }
  elsif ($origen) { $vistaOrigen{$v} = "reemplazada por `$origen`"; }
  $baseVista{$v} = join(', ', map { "`$_`" } sort keys %b);
}
while ($sql05 =~ /CREATE VIEW afrisan\."(\w+)" AS(.*?);/sg) { vista($1, $2, ''); }
for my $m (@migraciones) { my ($origen, $sql) = @$m; while ($sql =~ /CREATE (?:OR REPLACE )?VIEW afrisan\."(\w+)"(?: WITH \([^)]*\))? AS(.*?);/sg) { vista($1, $2, $origen); } }

my @modulos = (
  ['Operación (componente 01_operacion)', [qw(Rol Usuario Cliente Sucursales Refrigerantes Cilindros OrdenTrabajo DetalleOT RetiroPrevisto Pesaje EvidenciaPesaje Despacho OperacionTerreno CustodiaActual RecepcionBodega RevisionOT Incidencia HistorialCilindro EventoOT RectificacionPesaje)]],
  ['Soporte: límites, costos y disposición (componente 02_soporte)', [qw(LimiteSucursal LimiteSucursalHistorial MedicionLimite MedicionLimiteOperaciones RevisionExceso PreciosRefrigerantes TiposCambio Costos CertificadosDisposicion CertificadosCilindros)]],
  ['Auditoría y bajas (componente 03_auditoria_y_acceso)', [qw(Auditoria BajaCilindro)]],
);
my %enModulo = map { $_ => 1 } map { @{$_->[1]} } @modulos;
for my $t (@orden) { die "Tabla $t sin módulo asignado" unless $enModulo{$t}; die "Tabla $t sin descripción" unless $desc{$t}; }
die "Se esperaban 32 tablas y 9 vistas" unless @orden == 32 && @vistas == 9;
my $base = grep { !$tabla{$_}{origen} } @orden;
my $sinAcceso = grep { !$permiso{$_} } @orden;
sub acceso { my ($t) = @_; return '**Sin acceso** (diferido)' unless $permiso{$t};
  return $permiso{$t} . ($politica{$t} ? '' : ' (sin política RLS)') . ($desde{$t} ne '07' ? " · desde `$desde{$t}`" : ''); }

# Salida
print "# Diccionario del esquema `afrisan` (vigente)\n\n";
print "- **Fuente:** `05_Crear_Afrisan_Sin_Emisiones.sql` (mismo esquema que `06`) más las migraciones aditivas `07_Habilitar_Persistencia.sql`, `08_Rectificacion_Pesajes.sql`, `09_Mantenedores_Bajas.sql` y `10_Costos_Limites.sql`.\n";
print "- **Contenido:** " . scalar(@orden) . ' tablas y ' . scalar(@vistas) . " vistas en PostgreSQL 16 (Supabase): $base tablas y 8 vistas del modelo base más `RectificacionPesaje` (`08`), `TiposCambio` (`10`) y la vista `PesajeVigente` (`08`).\n";
print "- **Estado de instalación:** el modelo base y `07` están aplicados en Supabase (verificación remota del 02/11/2026, `04_SEGUIMIENTO/PERSISTENCIA_2026_11_02`). `08`, `09` y `10` están probadas en PostgreSQL local y faltan por aplicar (ver `03_CODIGO/DESPLIEGUE.md`).\n";
print "- **Generado con:** `perl generar_diccionario.pl > DICCIONARIO_ESQUEMA_AFRISAN.md`. Si cambia el SQL, vuelve a generarlo en vez de editarlo a mano.\n";
print "- **Convenciones:** PK = clave primaria, FK = clave foránea, UQ = único. Las masas se guardan en gramos enteros y las fechas en `timestamptz`.\n";
print "- **Seguridad:** RLS está activo en las " . scalar(@orden) . " tablas y no hay acceso para `PUBLIC`, `anon` ni `authenticated`. La columna *Acceso API* indica los permisos del rol `afrisan_api` después de `10`; $sinAcceso tablas siguen sin acceso porque su módulo quedó diferido.\n\n";
print "## Resumen\n\n| # | Tabla | Módulo | Columnas | Acceso API (`afrisan_api`) |\n| --- | --- | --- | --- | --- |\n";
my $n = 0;
for my $m (@modulos) { for my $t (@{$m->[1]}) { $n++; my ($corto) = $m->[0] =~ /^([^(:]+)/; $corto =~ s/\s+$//;
  my $nombre = "[`$t`](#$t)" . ($tabla{$t}{origen} ? " (nueva en `$tabla{$t}{origen}`)" : '');
  print "| $n | $nombre | $corto | " . scalar(@{$tabla{$t}{cols}}) . ' | ' . acceso($t) . " |\n"; } }
print "\n";
for my $m (@modulos) {
  print "## $m->[0]\n\n";
  for my $t (@{$m->[1]}) {
    my $x = $tabla{$t};
    print "<a id=\"$t\"></a>\n### `$t`\n\n$desc{$t}\n\n| Columna | Tipo | Nulo | Clave | Notas |\n| --- | --- | --- | --- | --- |\n";
    my %enPk = map { $_ => 1 } split /, /, ($x->{pkc} // '');
    for my $c (@{$x->{cols}}) {
      my @k; push @k, 'PK' if $c->{pk} || $enPk{$c->{nombre}}; push @k, "FK → `$c->{fk}`" if $c->{fk}; push @k, 'UQ' if $c->{uq};
      push @k, "FK compuesta → `$fkc{$t}{$c->{nombre}}`" if !$c->{fk} && $fkc{$t}{$c->{nombre}};
      print '| `' . $c->{nombre} . '` | `' . $c->{tipo} . '` | ' . ($c->{nn} || $enPk{$c->{nombre}} ? 'No' : 'Sí') . ' | ' . join(', ', @k) . ' | ' . md(join('; ', @{$c->{notas}})) . " |\n";
    }
    print "\n";
    print '- **PK compuesta:** `(' . $x->{pkc} . ")`\n" if $x->{pkc};
    print '- **Claves foráneas:** ' . join('; ', @{$x->{fks}}) . "\n" if @{$x->{fks}};
    print '- **Únicos compuestos:** ' . join('; ', map { "`($_)`" } @{$x->{uq}}) . "\n" if @{$x->{uq}};
    print '- **Índices:** ' . md(join('; ', @{$x->{idx}})) . "\n" if @{$x->{idx}};
    print '- **Reglas CHECK de la tabla:** ' . md(join('; ', map { "`$_`" } @{$x->{ck}})) . "\n" if @{$x->{ck}};
    print '- **Triggers:** ' . join('; ', @{$x->{trg}}) . "\n" if @{$x->{trg}};
    print '- **Cambios posteriores:** ' . join('; ', @{$x->{cambios}}) . "\n" if $x->{cambios};
    print '- **Acceso API:** ' . ($permiso{$t} ? $permiso{$t} . ($politica{$t} ? ' · política `BackendConfiable`' : ' · sin política RLS') . ($desde{$t} ne '07' ? " · habilitado por `$desde{$t}`" : '')
      : 'sin `GRANT` ni política para `afrisan_api`; módulo diferido, la API no puede leer ni escribir esta tabla') . "\n\n";
  }
}
print "## Vistas\n\n| Vista | Qué muestra | Tablas base | Origen | Acceso API |\n| --- | --- | --- | --- | --- |\n";
for my $v (@vistas) { die "Vista $v sin descripción" unless $descVista{$v};
  print "| `$v` | $descVista{$v} | $baseVista{$v} | " . ($vistaOrigen{$v} ? ($vistaOrigen{$v} =~ /^\d/ ? "nueva en `$vistaOrigen{$v}`" : $vistaOrigen{$v}) : '`05`') . ' | ' . ($permiso{$v} // 'Sin acceso') . " |\n"; }
