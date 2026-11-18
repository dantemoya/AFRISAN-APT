// CDP-16 y CDP-29: solicitudes simultáneas contra la base de Supabase, con la API local en modo PostgreSQL.
// CDP-16: dos altas con el mismo número de serie a la vez (los códigos los asigna la base desde D-47) y
//         cinco altas simultáneas sin serie, que deben recibir códigos distintos.
// CDP-29: el mismo técnico, con dos sesiones, retira a la vez el mismo cilindro para dos OT distintas.
// Después deja los datos ordenados: anula la OT que no obtuvo el cilindro y cierra la otra como agregado.
// Crea datos de prueba (serie P1711-*), que se limpian junto con los del 14/11.
// Uso (desde la raíz, con la API local encendida): node AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_17/concurrencia_supabase.mjs
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {randomUUID} from 'node:crypto';
import {deflateSync} from 'node:zlib';

const directory = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(directory, '../../..');
const base = (process.env.AFRISAN_API ?? 'http://localhost:5181').replace(/\/+$/, '') + '/api/v1/';
const cuentas = JSON.parse(await fs.readFile(path.join(root, '.local/cuentas_prueba_2026_11_14.json'), 'utf8'));
const checks = []; const datos = {};
const report = {fecha_utc: new Date().toISOString(), tipo: 'CDP-16 y CDP-29 con la API local y la base de Supabase', comprobaciones: checks, datos_creados: datos};
function check(ok, name) { checks.push({resultado: ok ? 'OK' : 'FALLO', comprobacion: name}); if (!ok) throw Error(name); console.log('OK:', name); }
async function http(route, token, body, method = body === undefined ? 'GET' : 'POST') {
  const r = await fetch(base + route, {method, headers: {...(token ? {Authorization: 'Bearer ' + token} : {}), ...(body === undefined ? {} : {'Content-Type': 'application/json'})}, body: body === undefined ? undefined : JSON.stringify(body)});
  const text = await r.text(); let result; try { result = JSON.parse(text); } catch { result = text; }
  return {status: r.status, result};
}
async function api(route, token, body, method) { const {status, result} = await http(route, token, body, method); if (status < 200 || status > 299) throw Error(`${route}: ${status} ${JSON.stringify(result).slice(0, 300)}`); return result; }
function png(color) { const crc = b => { let n = 0xffffffff; for (const x of b) { n ^= x; for (let i = 0; i < 8; i++) n = n & 1 ? (n >>> 1) ^ 0xedb88320 : n >>> 1; } return (n ^ 0xffffffff) >>> 0; }; const chunk = (name, data) => { const b = Buffer.concat([Buffer.from(name), data]); const out = Buffer.alloc(data.length + 12); out.writeUInt32BE(data.length); b.copy(out, 4); out.writeUInt32BE(crc(b), out.length - 4); return out; }; const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(1); ihdr.writeUInt32BE(1, 4); ihdr[8] = 8; ihdr[9] = 2; return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', deflateSync(Buffer.from([0, color, 170, 40]))), chunk('IEND', Buffer.alloc(0))]).toString('base64'); }
const semilla = Date.now() % 150;
const foto = (nombre, n) => ({id: randomUUID(), nombre: `${nombre}.png`, contentType: 'image/png', base64: png((semilla + n) % 256)});
const login = async rol => { const c = cuentas.find(x => x.rol === rol); return (await api('sesiones', null, {email: c.correo, password: c.password})).token; };
const hoyChile = () => new Intl.DateTimeFormat('en-CA', {timeZone: 'America/Santiago'}).format(new Date());

try {
  const bodega = await login('Bodega'), autorizador = await login('Autorizador');
  const tecnicoA = await login('Tecnico'), tecnicoB = await login('Tecnico');
  check(tecnicoA !== tecnicoB, 'El técnico de prueba abre dos sesiones independientes');
  const tecnicoEmail = cuentas.find(x => x.rol === 'Tecnico').correo;
  const sucursal = (await api('sucursales', bodega)).find(s => s.nombre === 'Local Maipú (prueba)');
  const gas = (await api('refrigerantes', bodega)).find(g => g.codigo === 'R404A-PRUEBA');
  check(sucursal && gas, 'Existen la sucursal y el gas de prueba del 14/11');
  const serie = `P1711-${Date.now().toString(36).toUpperCase()}`;
  const alta = s => ({ubicacionInicial: 'Bodega', refrigeranteId: gas.id, capacidadGr: 10000, taraGr: 5000, numeroSerie: s});

  // CDP-16
  const dobles = await Promise.all([http('cilindros', bodega, alta(serie)), http('cilindros', bodega, alta(serie))]);
  const estados = dobles.map(d => d.status).sort();
  check(estados[0] === 201 && estados[1] === 409, `CDP-16: dos altas simultáneas con la misma serie: una se crea (201) y la otra recibe conflicto (409) [${estados}]`);
  const creado = dobles.find(d => d.status === 201).result; datos.cilindroSerie = creado.codigo;
  const varias = await Promise.all(Array.from({length: 5}, () => http('cilindros', bodega, alta(null))));
  const codigos = varias.map(v => v.result.codigo);
  datos.cilindrosSinSerie = codigos;
  check(varias.every(v => v.status === 201), 'CDP-16: cinco altas simultáneas sin serie se crean');
  check(new Set([...codigos, creado.codigo]).size === 6 && codigos.every(c => /^AF-\d{5}$/.test(c)), `CDP-16: los seis cilindros reciben códigos distintos de 8 caracteres (${[creado.codigo, ...codigos].join(', ')})`);
  const conSerie = (await api('cilindros', bodega)).filter(c => (c.numeroSerie ?? '').toUpperCase() === serie);
  check(conSerie.length === 1, 'CDP-16: en la base existe un solo cilindro con esa serie');

  // CDP-29
  await api(`cilindros/${creado.codigo}/pesaje-inicial`, bodega, {pesoBrutoGr: 14000, foto: foto('inicial', 1)});
  const pedido = desc => ({idOperacion: randomUUID(), sucursalId: sucursal.id, sucursal: sucursal.nombre, direccion: sucursal.direccion, descripcion: desc, tecnicoEmail,
    detalle: [{refrigeranteId: gas.id, grSolicitados: 9000, grAutorizados: 9000}], cilindrosARetirar: [], solicitante: 'Jefe de local (prueba)', prioridad: 'Normal', fechaProgramada: hoyChile()});
  const ot1 = await api('ordenes-trabajo', autorizador, pedido(`Prueba CDP-29 A ${serie}`));
  const ot2 = await api('ordenes-trabajo', autorizador, pedido(`Prueba CDP-29 B ${serie}`));
  datos.ots = [ot1.codigo, ot2.codigo];
  const carrera = await Promise.all([
    http(`ordenes-trabajo/${ot1.id}/entregas`, tecnicoA, {idOperacion: randomUUID(), codigo: creado.codigo, pesoBrutoGr: 14000, foto: foto('salida-a', 2)}),
    http(`ordenes-trabajo/${ot2.id}/entregas`, tecnicoB, {idOperacion: randomUUID(), codigo: creado.codigo, pesoBrutoGr: 14000, foto: foto('salida-b', 3)}),
  ]);
  const ok = carrera.filter(c => c.status === 200).length, conflicto = carrera.filter(c => c.status === 409).length;
  report.carrera = carrera.map(c => ({status: c.status, detalle: c.status === 200 ? 'retiro registrado' : (c.result?.detail ?? c.result?.title ?? c.result)}));
  check(ok === 1 && conflicto === 1, `CDP-29: dos retiros simultáneos del mismo cilindro: uno queda activo y el otro recibe conflicto (409) [${carrera.map(c => c.status)}]`);
  const ganadora = carrera[0].status === 200 ? ot1 : ot2, perdedora = ganadora === ot1 ? ot2 : ot1;
  const despachos = (await api('ordenes-trabajo', autorizador)).filter(o => o.id === ot1.id || o.id === ot2.id).flatMap(o => o.entregas.map(e => e.codigo));
  check(despachos.length === 1, 'CDP-29: el cilindro aparece en un solo despacho');

  // Orden de los datos de prueba
  const anulada = await api(`ordenes-trabajo/${perdedora.id}/anular`, autorizador, {motivo: 'Prueba de concurrencia CDP-29: OT sin retiro'});
  const r = `ordenes-trabajo/${ganadora.id}`; const opId = randomUUID(); const inst = await api('entorno');
  let op = await api(r + '/operaciones', tecnicoA, {id: opId, instancia: inst.instancia, version: 0, tipo: 'Agregado', codigoInstalado: creado.codigo, pesoInstaladoGr: 13950}, 'PUT');
  op = await api(r + `/operaciones/${opId}/fotos`, tecnicoA, {...foto('instalado', 4), grupo: 'Instalado', version: op.version});
  await api(r + `/operaciones/${opId}/confirmar?version=${op.version}`, tecnicoA, undefined, 'POST');
  await api(r + '/termino-tecnico', tecnicoA, {observacion: 'Prueba CDP-29'});
  await api(r + '/aprobar-bodega', bodega, {confirmoRevision: true, observacion: 'Prueba CDP-29'});
  await api(r + '/revision-autorizador', autorizador, {confirmoRevision: true, observacion: 'Prueba CDP-29'});
  const cerrada = await api(r + '/cerrar', autorizador, undefined, 'POST');
  check(anulada.estado === 'Anulada' && cerrada.estado === 'Cerrada', `Datos de prueba ordenados: ${perdedora.codigo} anulada y ${ganadora.codigo} cerrada`);
  report.estado = 'CORRECTO';
} catch (error) { report.estado = 'FALLO'; report.error = error.message; console.error(error.message); process.exitCode = 1; }
finally {
  report.cantidad_comprobaciones = checks.length;
  await fs.writeFile(path.join(directory, 'resultados_concurrencia_supabase.json'), JSON.stringify(report, null, 2));
  console.log(`${report.estado}: ${checks.length} comprobaciones`);
}
