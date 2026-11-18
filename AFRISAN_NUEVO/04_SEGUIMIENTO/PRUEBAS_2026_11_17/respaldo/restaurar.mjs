// CDP-61: restaura el respaldo lógico de Supabase en una instancia de prueba (PostgreSQL embebido) creada
// con los scripts 05 y 07 a 12, compara cada tabla con el respaldo y mide el tiempo.
// Uso (desde la raíz): node AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_17/respaldo/restaurar.mjs <carpeta del respaldo>
import fs from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
import {fileURLToPath, pathToFileURL} from 'node:url';

const directory = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(directory, '../../../..');
const carpeta = process.argv[2]; if (!carpeta) throw Error('Indica la carpeta del respaldo');
const {PGlite} = await import(pathToFileURL(path.join(root, '.local/validacion_sql_contexto/node_modules/@electric-sql/pglite/dist/index.js')));
const manifiesto = JSON.parse(await fs.readFile(path.join(carpeta, 'manifiesto.json'), 'utf8'));
const checks = []; const tiempos = {};
function check(ok, name) { checks.push({resultado: ok ? 'OK' : 'FALLO', comprobacion: name}); console.log(ok ? 'OK:' : 'FALLO:', name); return ok; }
const sha = b => createHash('sha256').update(b).digest('hex').toUpperCase();
const q = s => '"' + s.replace(/"/g, '""') + '"';

const t0 = performance.now();
const db = await PGlite.create();
for (const s of ['05_Crear_Afrisan_Sin_Emisiones', '07_Habilitar_Persistencia', '08_Rectificacion_Pesajes', '09_Mantenedores_Bajas', '10_Costos_Limites', '11_Devolucion_Anulacion_Recarga', '12_Codigos_Fotos_Pedido'])
  await db.exec(await fs.readFile(path.join(root, `AFRISAN_NUEVO/02_BASE_DATOS/${s}.sql`), 'utf8'));
tiempos.esquema_s = (performance.now() - t0) / 1000;
const destino = new Set((await db.query("SELECT tablename FROM pg_tables WHERE schemaname='afrisan'")).rows.map(r => r.tablename));
check(manifiesto.detalle.every(t => destino.has(t.tabla)) && destino.size === manifiesto.tablas, `La instancia de prueba tiene las mismas ${manifiesto.tablas} tablas que Supabase`);

const t1 = performance.now();
await db.exec("SET session_replication_role = replica");   // sin disparadores ni claves foráneas durante la carga
for (const t of manifiesto.detalle) {
  const blob = new Blob([await fs.readFile(path.join(carpeta, t.tabla + '.csv'))]);
  await db.query(`COPY afrisan.${q(t.tabla)} (${t.columnas.map(q).join(',')}) FROM '/dev/blob' WITH (FORMAT csv, HEADER)`, [], {blob});
}
await db.exec("SET session_replication_role = origin");
// Secuencias: identidades y columnas con nextval quedan en el máximo restaurado; códigos cortos según su número.
const seqs = (await db.query(`SELECT c.table_name, c.column_name, pg_get_serial_sequence('afrisan.'||quote_ident(c.table_name), c.column_name) AS s
  FROM information_schema.columns c WHERE c.table_schema='afrisan' AND (c.is_identity='YES' OR c.column_default LIKE 'nextval(%')`)).rows.filter(r => r.s);
for (const r of seqs) await db.query(`SELECT setval('${r.s}', coalesce((SELECT max(${q(r.column_name)}) FROM afrisan.${q(r.table_name)}), 0) + 1, false)`);
await db.query(`SELECT setval('afrisan."SecuenciaCodigoOT"', coalesce((SELECT max(substr("CodigoOT",4)::int) FROM afrisan."OrdenTrabajo" WHERE "CodigoOT" ~ '^OT-[0-9]{5}$'), 0) + 1, false)`);
await db.query(`SELECT setval('afrisan."SecuenciaCodigoCilindro"', coalesce((SELECT max(substr("CilindroQR",4)::int) FROM afrisan."Cilindros" WHERE "CilindroQR" ~ '^AF-[0-9]{5}$'), 0) + 1, false)`);
tiempos.carga_s = (performance.now() - t1) / 1000;

// Comparación: cada tabla se vuelve a exportar con las mismas columnas y orden, y su SHA-256 debe coincidir.
let iguales = 0, filas = 0;
for (const t of manifiesto.detalle) {
  const r = await db.query(`COPY (SELECT ${t.columnas.map(q).join(',')} FROM afrisan.${q(t.tabla)} ORDER BY 1) TO '/dev/blob' WITH (FORMAT csv, HEADER)`);
  const bytes = Buffer.from(await r.blob.arrayBuffer());
  const n = Number((await db.query(`SELECT count(*)::int AS n FROM afrisan.${q(t.tabla)}`)).rows[0].n);
  filas += n;
  if (sha(bytes) === t.sha256 && n === t.filas) iguales++; else console.log('  diferencia en', t.tabla, n, t.filas);
}
check(iguales === manifiesto.tablas, `Las ${manifiesto.tablas} tablas restauradas son idénticas al respaldo (SHA-256 y ${filas} filas)`);
const fk = (await db.query(`SELECT count(*)::int AS n FROM pg_constraint WHERE contype='f' AND connamespace='afrisan'::regnamespace`)).rows[0].n;
// La carga omitió las claves foráneas: cada una se vuelve a crear (lo que revisa todas las filas) y se deshace.
let fkMalas = 0;
for (const c of (await db.query(`SELECT conrelid::regclass::text AS t, conname, pg_get_constraintdef(oid) AS def FROM pg_constraint WHERE contype='f' AND connamespace='afrisan'::regnamespace`)).rows) {
  try { await db.exec(`BEGIN; ALTER TABLE ${c.t} DROP CONSTRAINT ${q(c.conname)}; ALTER TABLE ${c.t} ADD CONSTRAINT ${q(c.conname)} ${c.def}; ROLLBACK;`); }
  catch (e) { fkMalas++; console.log('  clave foránea con filas inválidas:', c.t, c.conname, e.message); await db.exec('ROLLBACK').catch(() => {}); }
}
const huerfanos = (await db.query(`SELECT
  (SELECT count(*) FROM afrisan."Despacho" d LEFT JOIN afrisan."Cilindros" c USING("Id_Cilindro") WHERE c."Id_Cilindro" IS NULL)
 +(SELECT count(*) FROM afrisan."Pesaje" p LEFT JOIN afrisan."Cilindros" c USING("Id_Cilindro") WHERE c."Id_Cilindro" IS NULL)
 +(SELECT count(*) FROM afrisan."EvidenciaPesaje" e LEFT JOIN afrisan."Pesaje" p USING("Id_Pesaje") WHERE p."Id_Pesaje" IS NULL) AS n`)).rows[0].n;
check(Number(huerfanos) === 0 && fkMalas === 0, `Cilindros, pesajes, despachos y evidencias quedan relacionados (${fk} claves foráneas, 0 filas huérfanas)`);
const cuentas = (await db.query(`SELECT (SELECT count(*) FROM afrisan."Cilindros")::int AS cilindros,(SELECT count(*) FROM afrisan."OrdenTrabajo")::int AS ot,
  (SELECT count(*) FROM afrisan."Pesaje")::int AS pesajes,(SELECT count(*) FROM afrisan."Despacho")::int AS despachos,(SELECT count(*) FROM afrisan."EvidenciaPesaje")::int AS fotos`)).rows[0];
const total = (performance.now() - t0) / 1000;
await db.close();
const report = {fecha_utc: new Date().toISOString(), respaldo_utc: manifiesto.instante_utc, respaldo_s: manifiesto.segundos, ...tiempos, restauracion_total_s: total,
  rto_s: manifiesto.segundos + total, tablas: manifiesto.tablas, filas: manifiesto.filas, cuentas, comprobaciones: checks,
  estado: checks.every(c => c.resultado === 'OK') ? 'CORRECTO' : 'FALLO'};
await fs.writeFile(path.join(directory, 'resultados_respaldo.json'), JSON.stringify(report, null, 2));
console.log(`${report.estado}: respaldo ${manifiesto.segundos.toFixed(1)} s + restauración ${total.toFixed(1)} s`, cuentas);
if (report.estado !== 'CORRECTO') process.exitCode = 1;
