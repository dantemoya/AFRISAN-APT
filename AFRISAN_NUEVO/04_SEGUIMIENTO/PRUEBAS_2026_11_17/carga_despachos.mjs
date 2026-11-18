// CDP-62: carga de 50 despachos con lectura del QR. Cada despacho consulta el cilindro por su código (lo que
// hace la pantalla al leer el QR) y registra el retiro con peso y foto. Cinco técnicos simulados trabajan a la
// vez, uno por OT. Criterio: ningún error y cada operación responde en menos de 2 segundos.
// Contra la API en modo Memoria usa las cuentas de demostración; contra PostgreSQL (local o Azure) usa las
// cuentas de prueba de .local/cuentas_prueba_2026_11_14.json y crea datos de prueba (serie P1711C-*): 50
// cilindros y 5 OT. Al terminar, Bodega recibe los 50 cilindros sin uso, que vuelven a quedar disponibles.
// Uso (desde la raíz):
//   node AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_17/carga_despachos.mjs                (API local, puerto 5181)
//   PowerShell: $env:AFRISAN_API="https://afrisan-api-dm.azurewebsites.net"; node AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_17/carga_despachos.mjs
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {randomUUID} from 'node:crypto';
import {deflateSync} from 'node:zlib';

const directory = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(directory, '../../..');
const origen = (process.env.AFRISAN_API ?? 'http://localhost:5181').replace(/\/+$/, '');
const base = origen + '/api/v1/';
const cantidad = Number(process.env.CANTIDAD ?? 50), tecnicos = 5, limiteMs = 2000;
const report = {fecha_utc: new Date().toISOString(), api: origen, cantidad, tecnicos_simulados: tecnicos, limite_ms: limiteMs};
async function http(route, token, body, method = body === undefined ? 'GET' : 'POST') {
  const t = performance.now();
  const r = await fetch(base + route, {method, headers: {...(token ? {Authorization: 'Bearer ' + token} : {}), ...(body === undefined ? {} : {'Content-Type': 'application/json'})}, body: body === undefined ? undefined : JSON.stringify(body)});
  const text = await r.text(); const ms = performance.now() - t; let result; try { result = JSON.parse(text); } catch { result = text; }
  return {status: r.status, result, ms};
}
async function api(route, token, body, method) { const x = await http(route, token, body, method); if (x.status < 200 || x.status > 299) throw Error(`${route}: ${x.status} ${JSON.stringify(x.result).slice(0, 300)}`); return x.result; }
function png(a, b) { const crc = x => { let n = 0xffffffff; for (const y of x) { n ^= y; for (let i = 0; i < 8; i++) n = n & 1 ? (n >>> 1) ^ 0xedb88320 : n >>> 1; } return (n ^ 0xffffffff) >>> 0; }; const chunk = (name, data) => { const c = Buffer.concat([Buffer.from(name), data]); const out = Buffer.alloc(data.length + 12); out.writeUInt32BE(data.length); c.copy(out, 4); out.writeUInt32BE(crc(c), out.length - 4); return out; }; const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(1); ihdr.writeUInt32BE(1, 4); ihdr[8] = 8; ihdr[9] = 2; return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', deflateSync(Buffer.from([0, a % 256, b % 256, 77]))), chunk('IEND', Buffer.alloc(0))]).toString('base64'); }
const semilla = Date.now() % 97;
const foto = (nombre, i, g) => ({id: randomUUID(), nombre: `${nombre}.png`, contentType: 'image/png', base64: png(semilla + i, g)});
const estad = xs => { const s = [...xs].sort((a, b) => a - b); const p = q => s[Math.min(s.length - 1, Math.floor(q * s.length))]; return {n: s.length, p50_ms: Math.round(p(0.5)), p95_ms: Math.round(p(0.95)), max_ms: Math.round(s[s.length - 1] ?? 0)}; };
const hoyChile = () => new Intl.DateTimeFormat('en-CA', {timeZone: 'America/Santiago'}).format(new Date());

try {
  const entorno = await api('entorno');
  const memoria = entorno.modo === 'Memoria';
  let cuentas;
  if (memoria) cuentas = entorno.cuentas.map(c => ({rol: c.rol, correo: c.email, password: entorno.passwordDemo}));
  else cuentas = JSON.parse(await fs.readFile(path.join(root, '.local/cuentas_prueba_2026_11_14.json'), 'utf8'));
  const login = async rol => { const c = cuentas.find(x => x.rol === rol); return (await api('sesiones', null, {email: c.correo, password: c.password})).token; };
  const bodega = await login('Bodega'), autorizador = await login('Autorizador');
  const sesionesTecnico = []; for (let i = 0; i < tecnicos; i++) sesionesTecnico.push(await login('Tecnico'));
  const tecnicoEmail = cuentas.find(x => x.rol === 'Tecnico').correo;
  let gas, sucursal;
  if (memoria) { gas = await api('refrigerantes', bodega, {codigo: 'R-CARGA', nombre: 'Gas de carga'}); sucursal = {id: null, nombre: 'Sucursal de carga', direccion: 'Dirección de carga'}; }
  else { gas = (await api('refrigerantes', bodega)).find(g => g.codigo === 'R404A-PRUEBA'); sucursal = (await api('sucursales', bodega)).find(s => s.nombre === 'Local Maipú (prueba)'); }
  if (!gas || !sucursal) throw Error('Faltan el gas o la sucursal de prueba');
  const serie = `P1711C-${Date.now().toString(36).toUpperCase()}`;

  // Preparación (no se mide como despacho): 50 cilindros con pesaje inicial y foto, y 5 OT.
  const cilindros = [];
  for (let i = 0; i < cantidad; i++) cilindros.push((await api('cilindros', bodega, {ubicacionInicial: 'Bodega', refrigeranteId: gas.id, capacidadGr: 10000, taraGr: 5000, pesoInicialGr: 14000, numeroSerie: `${serie}-${i + 1}`, foto: foto('inicial', i, 11)})).codigo);
  const ots = [];
  for (let k = 0; k < tecnicos; k++) ots.push(await api('ordenes-trabajo', autorizador, {idOperacion: randomUUID(), sucursalId: sucursal.id, sucursal: sucursal.nombre, direccion: sucursal.direccion,
    descripcion: `Prueba de carga CDP-62 ${serie} (${k + 1})`, tecnicoEmail, detalle: [{refrigeranteId: gas.id, grSolicitados: 90000, grAutorizados: 90000}], cilindrosARetirar: [],
    solicitante: 'Prueba de carga', prioridad: 'Baja', fechaProgramada: hoyChile()}));
  report.datos_creados = {serie, cilindros: cilindros.length, ots: ots.map(o => o.codigo)};

  // Carga: cada técnico simulado despacha 10 cilindros en su OT; los cinco trabajan a la vez.
  const lecturas = [], retiros = [], errores = [];
  const inicio = performance.now();
  await Promise.all(ots.map(async (ot, k) => {
    for (let i = k; i < cantidad; i += tecnicos) {
      const codigo = cilindros[i];
      const qr = await http(`cilindros/${encodeURIComponent(codigo.toLowerCase())}`, sesionesTecnico[k]);
      lecturas.push(qr.ms); if (qr.status !== 200) errores.push(`lectura ${codigo}: ${qr.status}`);
      const r = await http(`ordenes-trabajo/${ot.id}/entregas`, sesionesTecnico[k], {idOperacion: randomUUID(), codigo, pesoBrutoGr: 14000, foto: foto('salida', i, 99)});
      retiros.push(r.ms); if (r.status !== 200) errores.push(`retiro ${codigo}: ${r.status} ${JSON.stringify(r.result).slice(0, 120)}`);
    }
  }));
  report.duracion_total_s = Math.round((performance.now() - inicio) / 100) / 10;
  report.lectura_qr = estad(lecturas); report.retiro = estad(retiros); report.errores = errores;

  // Orden: Bodega recibe sin uso los cilindros despachados.
  const recepciones = [];
  for (let k = 0; k < tecnicos; k++) for (let i = k; i < cantidad; i += tecnicos) {
    const r = await http(`ordenes-trabajo/${ots[k].id}/recepciones`, bodega, {codigo: cilindros[i], pesoBrutoGr: 14000, observacion: 'Devuelto sin uso tras la prueba de carga', foto: foto('recepcion', i, 201)});
    recepciones.push(r.ms); if (r.status !== 200) errores.push(`recepción ${cilindros[i]}: ${r.status}`);
  }
  report.recepcion = estad(recepciones);
  const maximo = Math.max(report.lectura_qr.max_ms, report.retiro.max_ms);
  report.estado = errores.length === 0 && maximo < limiteMs ? 'CORRECTO' : 'FALLO';
  console.log(`${report.estado}: ${cantidad} despachos en ${report.duracion_total_s} s; lectura QR p95 ${report.lectura_qr.p95_ms} ms (máx. ${report.lectura_qr.max_ms}); retiro p95 ${report.retiro.p95_ms} ms (máx. ${report.retiro.max_ms}); errores ${errores.length}`);
} catch (error) { report.estado = 'FALLO'; report.error = error.message; console.error(error.message); }
finally {
  const nombre = origen.includes('localhost') || origen.includes('127.0.0.1') ? 'resultados_carga_local.json' : 'resultados_carga_azure.json';
  await fs.writeFile(path.join(directory, nombre), JSON.stringify(report, null, 2));
  if (report.estado !== 'CORRECTO') process.exitCode = 1;
}
