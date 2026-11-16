// Pruebas del módulo Power BI (CDP-54 a CDP-57) contra la base de Supabase del 15/11/2026.
// Usa la API local ya iniciada en modo PostgreSQL (por defecto http://localhost:5181) y las cuentas de
// prueba del 14/11/2026, cuyas claves están fuera del repositorio (.local/cuentas_prueba_2026_11_14.json).
// Solo lee datos: no crea OT, cilindros ni movimientos. Sí emite tokens de Power BI de 30 días para la
// cuenta de prueba de Jefatura; ninguno se imprime ni se guarda en el repositorio.
// El proyecto conectado se guarda en .local/powerbi_2026_11_15/ (Git lo ignora) para abrirlo en Desktop.
// Uso (desde la raíz del repositorio):
//   AFRISAN_API=https://afrisan-api-dm.azurewebsites.net node AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_15/validar_powerbi_supabase.mjs   (API desplegada en Azure)
//   node AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_15/validar_powerbi_supabase.mjs
import fs from 'node:fs/promises';
import path from 'node:path';
import zlib from 'node:zlib';
import {fileURLToPath} from 'node:url';

const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const origen=process.env.AFRISAN_API??'http://localhost:5181';
const base=origen+'/api/v1/';
const privado=path.join(root,'.local/powerbi_2026_11_15');
// Contra otra API (p. ej. AFRISAN_API=https://afrisan-api-dm.azurewebsites.net) los resultados y el proyecto van aparte.
const sufijo=process.env.AFRISAN_SUFIJO??(process.env.AFRISAN_API?'_azure':'');
const cuentas=JSON.parse(await fs.readFile(path.join(root,'.local/cuentas_prueba_2026_11_14.json'),'utf8'));
const checks=[];const datos={};
const report={fecha_utc:new Date().toISOString(),tipo:'API .NET 8 local en modo PostgreSQL + base de Supabase (CDP-54 a CDP-57)',
 api:origen,comprobaciones:checks,datos};
let fallos=0;
function check(ok,name,detalle){checks.push({resultado:ok?'OK':'FALLO',comprobacion:name,...(detalle===undefined?{}:{detalle})});
 if(!ok)fallos++;console.log(ok?'OK:':'FALLO:',name,detalle===undefined?'':JSON.stringify(detalle));return ok;}

async function pedir(route,{token,body,method}={}){
 method??=body===undefined?'GET':'POST';
 const r=await fetch(base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},
  body:body===undefined?undefined:JSON.stringify(body)});
 const buf=Buffer.from(await r.arrayBuffer());let json;try{json=JSON.parse(buf.toString('utf8'));}catch{}
 return {status:r.status,headers:r.headers,buf,json};
}
const login=async rol=>{const c=cuentas.find(x=>x.rol===rol);const r=await pedir('sesiones',{body:{email:c.correo,password:c.password}});
 if(r.status!==200)throw Error(`No ingresa la cuenta de prueba ${rol}: HTTP ${r.status}`);return r.json.token;};

// Lector mínimo de ZIP (directorio central + deflate), suficiente para los paquetes de la API.
function leerZip(buf){
 let eocd=buf.lastIndexOf(Buffer.from([0x50,0x4b,0x05,0x06]));if(eocd<0)throw Error('No es un ZIP');
 const n=buf.readUInt16LE(eocd+10);let p=buf.readUInt32LE(eocd+16);const out=new Map();
 for(let i=0;i<n;i++){
  const metodo=buf.readUInt16LE(p+10),comp=buf.readUInt32LE(p+20),ln=buf.readUInt16LE(p+28),le=buf.readUInt16LE(p+30),lc=buf.readUInt16LE(p+32),local=buf.readUInt32LE(p+42);
  const nombre=buf.toString('utf8',p+46,p+46+ln);
  const ini=local+30+buf.readUInt16LE(local+26)+buf.readUInt16LE(local+28);const raw=buf.subarray(ini,ini+comp);
  out.set(nombre,metodo===8?zlib.inflateRawSync(raw):raw);p+=46+ln+le+lc;
 }
 return out;
}
const sinBom=t=>t.replace(/^﻿/,'');
const filasCsv=t=>sinBom(t).split(/\r?\n/).filter(l=>l.length>0).length-1;
const correo=/[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/;
const jwt=/eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\./;
const camposProhibidos=/correo|email|password|clave|contrasena|foto|storage|ruta|direccion/i;

// D-45: kg instalados por fecha de confirmación y kg solicitados de OT no anuladas por fecha de creación,
// por sucursal, semana (lunes a domingo) y mes. Mismo cálculo que PgPowerBi.Consumo.
function consumoDesdeTablas(t,ops,ordenes,refrigeranteId){
 const porOt=new Map(t.ordenes.map(o=>[o.otId,o]));const pedidas=new Map(ordenes.filter(o=>o.estado!=='Anulada').map(o=>[o.otId,o]));
 const filas=[...ops.map(x=>({o:porOt.get(x.otId),dia:x.diaConfirmacion,inst:Number(x.kgInstalados),sol:0})),
  ...t.detalle.filter(x=>pedidas.has(x.otId)&&(refrigeranteId==null||x.refrigeranteId===refrigeranteId)).map(x=>({o:pedidas.get(x.otId),dia:pedidas.get(x.otId).diaCreacion,inst:0,sol:Number(x.kgSolicitados)}))];
 const semana=d=>{const x=new Date(d+'T12:00:00Z');x.setUTCDate(x.getUTCDate()-((x.getUTCDay()+6)%7));return x.toISOString().slice(0,10);};
 const mes=d=>d.slice(0,8)+'01';const r3=v=>Math.round(v*1000)/1000;const grupos=new Map();
 for(const [periodo,inicio] of [['Mes',mes],['Semana',semana]])for(const f of filas){
  const k=periodo+'|'+inicio(f.dia)+'|'+f.o.sucursalId;const g=grupos.get(k)??{periodo,inicio:inicio(f.dia),sucursalId:f.o.sucursalId,sucursal:f.o.sucursal,clienteId:f.o.clienteId,cliente:f.o.cliente,kgInstalados:0,kgSolicitados:0};
  g.kgInstalados+=f.inst;g.kgSolicitados+=f.sol;grupos.set(k,g);}
 return [...grupos.values()].map(g=>({...g,kgInstalados:r3(g.kgInstalados),kgSolicitados:r3(g.kgSolicitados)}));
}
const claveConsumo=x=>[x.periodo,x.inicio,x.sucursalId,x.sucursal,x.clienteId,x.cliente,r3n(x.kgInstalados),r3n(x.kgSolicitados)].join('|');
const r3n=v=>(Math.round(Number(v)*1000)/1000).toFixed(3);
const mismoConsumo=(a,b)=>JSON.stringify((a??[]).map(claveConsumo).sort())===JSON.stringify((b??[]).map(claveConsumo).sort());

// Mismas reglas del resumen (PgPowerBi.Resumen), calculadas aquí desde las tablas que lee Power BI.
function resumenDesdeTablas(t,{desde,hasta,sucursalId,refrigeranteId,tecnicoId}){
 const dia=d=>d>=desde&&d<=hasta;
 const elegibles=new Set(t.ordenes.filter(o=>(sucursalId==null||o.sucursalId===sucursalId)&&(tecnicoId==null||o.tecnicoId===tecnicoId)).map(o=>o.otId));
 const conGas=new Set(t.detalle.filter(x=>refrigeranteId==null||x.refrigeranteId===refrigeranteId).map(x=>x.otId));
 const ordenes=t.ordenes.filter(o=>elegibles.has(o.otId)&&conGas.has(o.otId)&&dia(o.diaCreacion));
 const inv=t.inventario.filter(x=>(sucursalId==null||x.sucursalId===sucursalId)&&(refrigeranteId==null||x.refrigeranteId===refrigeranteId)&&(tecnicoId==null||x.tecnicoId===tecnicoId));
 const ops=t.operaciones.filter(x=>elegibles.has(x.otId)&&dia(x.diaConfirmacion)&&(refrigeranteId==null||x.refrigeranteId===refrigeranteId));
 const inc=t.incidencias.filter(x=>elegibles.has(x.otId)&&conGas.has(x.otId)&&dia(x.dia));
 const r3=v=>Math.round(v*1000)/1000;
 return {cilindros:inv.length,sinPesaje:inv.filter(x=>x.kgSegunUltimoPesaje==null).length,
  ordenesAbiertas:ordenes.filter(o=>!['Cerrada','Anulada'].includes(o.estado)).length,ordenesCerradas:ordenes.filter(o=>o.estado==='Cerrada').length,
  ordenesAnuladas:ordenes.filter(o=>o.estado==='Anulada').length,
  kgInstalados:r3(ops.reduce((s,x)=>s+Number(x.kgInstalados),0)),retiradosPendientes:ops.filter(x=>x.pendienteRecepcion).length,
  incidenciasAbiertas:inc.filter(x=>x.abierta).length,consumo:consumoDesdeTablas(t,ops,ordenes,refrigeranteId)};
}
const indicadores=r=>({cilindros:r.cilindros,sinPesaje:r.sinPesaje,ordenesAbiertas:r.ordenesAbiertas,ordenesCerradas:r.ordenesCerradas,ordenesAnuladas:r.ordenesAnuladas,
 kgInstalados:Math.round(Number(r.kgInstalados)*1000)/1000,retiradosPendientes:r.retiradosPendientes,incidenciasAbiertas:r.incidenciasAbiertas});
const medidaDe=(bim,nombre)=>bim.model.tables.flatMap(x=>x.measures??[]).find(m=>m.name===nombre)?.expression??'';
const igual=(a,b)=>Object.keys(b).every(k=>a[k]===b[k]);

try{
 // 1. Prueba de conexión de Power BI (D-33): la dirección base responde sin credenciales y sin filas.
 for(const ruta of ['power-bi/tablas','power-bi/tablas/']){
  const r=await pedir(ruta);
  check(r.status===200&&r.json?.estado==='ok'&&r.json?.tablas?.length===7&&Object.keys(r.json).every(k=>['estado','tablas','uso'].includes(k)),
   `GET /api/v1/${ruta} sin credenciales responde 200 solo con los nombres de las 7 tablas`,{http:r.status});
 }

 // 2. Jefatura obtiene la configuración y un token de 30 días (CDP-56).
 const jefatura=await login('Jefatura');
 check(true,'Ingresa la cuenta de prueba de Jefatura (primer uso)');
 const conf=await pedir('power-bi/configuracion',{token:jefatura});
 check(conf.status===200&&conf.json?.configurado===false&&conf.headers.get('cache-control')==='no-store',
  'Configuración del informe privado: 200, sin ReportId configurado y con Cache-Control no-store',{http:conf.status,configurado:conf.json?.configurado});
 const emitido=await pedir('power-bi/token',{token:jefatura,method:'POST'});
 const tokenBi=emitido.json?.token;
 // Tolerancia de ±15 minutos: el reloj del servidor puede diferir del de este equipo.
 const dias=(new Date(emitido.json?.vence)-Date.now())/86400000;
 check(emitido.status===200&&typeof tokenBi==='string'&&Math.abs(dias-30)<0.01&&emitido.json.tablas.length===7,
  'POST /power-bi/token: 200, vence en 30 días y declara 7 tablas',{http:emitido.status,dias:Math.round(dias*100)/100});
 const carga=JSON.parse(Buffer.from(tokenBi.split('.')[1],'base64url').toString('utf8'));
 check(carga.alcance==='powerbi'&&!Object.values(carga).some(v=>correo.test(String(v))),
  'El token lleva el alcance powerbi y no incluye el correo de la cuenta',{claims:Object.keys(carga).length});

 // 3. Las siete tablas con el token, sin campos sensibles.
 const tablas={};
 for(const nombre of emitido.json.tablas){
  const r=await pedir('power-bi/tablas/'+nombre,{token:tokenBi});
  tablas[nombre]=r.json;
  const campos=Array.isArray(r.json)&&r.json.length?Object.keys(r.json[0]):[];
  check(r.status===200&&Array.isArray(r.json)&&!campos.some(c=>camposProhibidos.test(c))&&!correo.test(r.buf.toString('utf8')),
   `GET /power-bi/tablas/${nombre} con el token: 200, sin correos ni campos sensibles`,{http:r.status,filas:r.json?.length,campos:campos.length});
 }
 datos.filas_por_tabla=Object.fromEntries(Object.entries(tablas).map(([k,v])=>[k,v?.length]));
 check(tablas.ordenes.every(o=>o.clienteId!=null&&o.cliente)&&tablas.inventario.every(c=>(c.sucursalId==null)===(c.cliente==null)),
  'Ordenes e inventario traen la empresa cliente de su sucursal (D-45)',{clientes:[...new Set(tablas.ordenes.map(o=>o.clienteId))].length});
 const mayus=await pedir('power-bi/tablas/ORDENES',{token:tokenBi});
 check(mayus.status===200&&mayus.json?.length===tablas.ordenes.length,'El nombre de la tabla no distingue mayúsculas',{http:mayus.status});

 // 4. Rechazos (CDP-56).
 const sinToken=await pedir('power-bi/tablas/ordenes');
 check(sinToken.status===401,'Tabla sin token: 401',{http:sinToken.status});
 const partes=tokenBi.split('.');const firma=partes[2];
 const alterado=[partes[0],partes[1],(firma[0]==='A'?'B':'A')+firma.slice(1)].join('.');
 const rAlt=await pedir('power-bi/tablas/ordenes',{token:alterado});
 check(rAlt.status===401,'Token con la firma alterada: 401',{http:rAlt.status});
 const otraCarga=Buffer.from(JSON.stringify({...carga,role:'Administrador'})).toString('base64url');
 const rCarga=await pedir('power-bi/tablas/ordenes',{token:[partes[0],otraCarga,partes[2]].join('.')});
 check(rCarga.status===401,'Token con el rol cambiado sin volver a firmar: 401',{http:rCarga.status});
 for(const [ruta,metodo] of [['ordenes-trabajo','GET'],['cilindros','GET'],['usuarios','GET'],['power-bi/resumen','GET'],['power-bi/paquete','GET'],
  ['power-bi/token','POST'],['power-bi/proyecto-conectado','POST'],['power-bi/tablas/ordenes','POST'],['power-bi/tablas/ordenes','DELETE']]){
  const r=await pedir(ruta,{token:tokenBi,method:metodo});
  check(r.status===403,`Token de Power BI en ${metodo} /api/v1/${ruta}: 403`,{http:r.status});
 }
 const noExiste=await pedir('power-bi/tablas/usuarios',{token:tokenBi});
 check(noExiste.status===404,'Tabla inexistente (usuarios) con el token: 404 y sin datos',{http:noExiste.status});

 // 5. Otros roles no obtienen token ni leen datos de Power BI.
 for(const rol of ['Tecnico','Bodega','Autorizador']){
  const s=await login(rol);
  const t=await pedir('power-bi/token',{token:s,method:'POST'});
  const res=await pedir('power-bi/resumen',{token:s});
  const tab=await pedir('power-bi/tablas/ordenes',{token:s});
  const paq=await pedir('power-bi/paquete',{token:s});
  const pro=await pedir('power-bi/proyecto-conectado',{token:s,method:'POST'});
  const demo=await pedir('power-bi/demo/configuracion',{token:s});
  check([t,res,tab,paq,pro,demo].every(r=>r.status===403),`${rol}: 403 en token, resumen, tablas, paquete, proyecto conectado y demostración`,
   {http:[t,res,tab,paq,pro,demo].map(r=>r.status)});
 }

 // 5b. Módulo gratuito (/power-bi): Jefatura consulta y descarga la plantilla, pero no configura el enlace.
 const demoConf=await pedir('power-bi/demo/configuracion',{token:jefatura});
 check(demoConf.status===200&&demoConf.json?.configurado===false,'Demostración: Jefatura consulta el estado (sin enlace público configurado)',{http:demoConf.status,configurado:demoConf.json?.configurado});
 const demoPut=await pedir('power-bi/demo/configuracion',{token:jefatura,method:'PUT',body:{urlPublica:'https://app.powerbi.com/view?r=PRUEBA'}});
 check(demoPut.status===403,'Demostración: Jefatura no puede guardar el enlace público (solo Administrador)',{http:demoPut.status});
 const plantilla=await pedir('power-bi/demo/plantilla',{token:jefatura});
 const zPlantilla=leerZip(plantilla.buf);
 check(plantilla.status===200&&[...zPlantilla.keys()].some(n=>n.endsWith('.pbip'))&&![...zPlantilla.values()].some(b=>jwt.test(b.toString('utf8'))),
  'Demostración: la plantilla gratuita se descarga con su proyecto y sin tokens',{http:plantilla.status,archivos:zPlantilla.size});

 // 6. Vista previa (resumen) contra los totales recalculados desde las tablas (CDP-54 y base de CDP-57).
 const t=tablas;
 const hoy=new Intl.DateTimeFormat('en-CA',{timeZone:'America/Santiago'}).format(new Date());
 const restar=(d,n)=>{const x=new Date(d+'T12:00:00Z');x.setUTCDate(x.getUTCDate()-n);return x.toISOString().slice(0,10);};
 const otPrueba=t.ordenes.filter(o=>o.sucursalId===3);
 const tecnicoPrueba=otPrueba[0]?.tecnicoId;
 const escenarios=[
  {nombre:'Sin filtros (últimos 30 días)',filtro:{}},
  {nombre:'Todo el historial',filtro:{desde:'2026-01-01',hasta:hoy}},
  {nombre:'Sucursal de prueba (Id 3)',filtro:{desde:'2026-01-01',hasta:hoy,sucursalId:3}},
  {nombre:'Gas de prueba (Id 5)',filtro:{desde:'2026-01-01',hasta:hoy,refrigeranteId:5}},
  {nombre:'Técnico de prueba',filtro:{desde:'2026-01-01',hasta:hoy,tecnicoId:tecnicoPrueba}},
  {nombre:'Sucursal + gas + técnico, solo 15/11',filtro:{desde:'2026-11-15',hasta:'2026-11-15',sucursalId:3,refrigeranteId:5,tecnicoId:tecnicoPrueba}},
  {nombre:'Período sin datos (enero)',filtro:{desde:'2026-01-01',hasta:'2026-01-31'}},
 ];
 datos.resumenes=[];
 for(const e of escenarios){
  const q=new URLSearchParams(Object.entries(e.filtro).filter(([,v])=>v!=null).map(([k,v])=>[k,String(v)])).toString();
  const r=await pedir('power-bi/resumen'+(q?'?'+q:''),{token:jefatura});
  const api=indicadores(r.json);
  const {consumo:consumoTablas,...calc}=resumenDesdeTablas(t,{desde:r.json.desde,hasta:r.json.hasta,...e.filtro});
  datos.resumenes.push({escenario:e.nombre,desde:r.json.desde,hasta:r.json.hasta,api});
  check(r.status===200&&igual(api,calc),`Resumen «${e.nombre}» igual a lo calculado desde las 7 tablas`,{http:r.status,api,...(igual(api,calc)?{}:{tablas:calc})});
  check(mismoConsumo(r.json.consumoPorSucursal,consumoTablas),`Consumo por sucursal «${e.nombre}» (semana y mes) igual al calculado desde las tablas`,
   {filas:r.json.consumoPorSucursal?.length,...(mismoConsumo(r.json.consumoPorSucursal,consumoTablas)?{}:{api:r.json.consumoPorSucursal,tablas:consumoTablas})});
 }
 const sumaEstados=r=>r.ordenesPorEstado?.reduce((s,x)=>s+Number(x.valor),0);
 const todo=await pedir('power-bi/resumen?desde=2026-01-01&hasta='+hoy,{token:jefatura});
 check(sumaEstados(todo.json)===todo.json.ordenesAbiertas+todo.json.ordenesCerradas+todo.json.ordenesAnuladas&&
  todo.json.inventarioPorEstado.reduce((s,x)=>s+Number(x.valor),0)===todo.json.cilindros,
  'Los gráficos por estado suman lo mismo que los indicadores');
 check(todo.json.sucursales.length>0&&todo.json.refrigerantes.length>0&&todo.json.tecnicos.length>0,
  'Las listas de filtros (sucursal, gas, técnico) vienen con opciones',{sucursales:todo.json.sucursales.length,gases:todo.json.refrigerantes.length,tecnicos:todo.json.tecnicos.length});
 const malo=await pedir('power-bi/resumen?desde=2026-11-15&hasta=2026-11-01',{token:jefatura});
 const cero=await pedir('power-bi/resumen?sucursalId=0',{token:jefatura});
 check([400,422].includes(malo.status)&&[400,422].includes(cero.status),'Rango invertido o filtro con Id 0: se rechaza',{http:[malo.status,cero.status]});

 // DEF-13 (corregido el 15/11): las OT anuladas (D-40) no cuentan como abiertas.
 const anuladas=t.ordenes.filter(o=>o.estado==='Anulada');
 datos.ordenes_por_estado=Object.fromEntries([...new Set(t.ordenes.map(o=>o.estado))].map(e=>[e,t.ordenes.filter(o=>o.estado===e).length]));
 check(anuladas.length>0&&todo.json.ordenesAnuladas===anuladas.length&&todo.json.ordenesAbiertas===t.ordenes.filter(o=>!['Cerrada','Anulada'].includes(o.estado)).length,
  'DEF-13: las OT anuladas se informan aparte y no cuentan como abiertas',{abiertas:todo.json.ordenesAbiertas,cerradas:todo.json.ordenesCerradas,anuladas:todo.json.ordenesAnuladas});

 // 7. Paquete ZIP de siete CSV (CDP-55).
 const paq=await pedir('power-bi/paquete',{token:jefatura});
 const zip=leerZip(paq.buf);
 const csv=['Inventario','Ordenes','DetalleOT','Operaciones','Despachos','Incidencias','Costos'];
 const mapa={Inventario:'inventario',Ordenes:'ordenes',DetalleOT:'detalle',Operaciones:'operaciones',Despachos:'despachos',Incidencias:'incidencias',Costos:'costos'};
 check(paq.status===200&&paq.headers.get('content-type')==='application/zip'&&csv.every(n=>zip.has(n+'.csv'))&&zip.has('Manifiesto.json'),
  'GET /power-bi/paquete: ZIP con los 7 CSV y el manifiesto',{http:paq.status,archivos:[...zip.keys()].length});
 const manifiesto=JSON.parse(sinBom(zip.get('Manifiesto.json').toString('utf8')));
 const filas=Object.fromEntries(csv.map(n=>[n,filasCsv(zip.get(n+'.csv').toString('utf8'))]));
 check(csv.every(n=>filas[n]===manifiesto.filas[n]&&filas[n]===tablas[mapa[n]].length),
  'Filas de cada CSV = manifiesto = tabla JSON de Power BI',{filas});
 const textoZip=[...zip.values()].map(b=>b.toString('utf8')).join('\n');
 check(!correo.test(textoZip)&&!jwt.test(textoZip)&&!/password|contrase/i.test(csv.map(n=>zip.get(n+'.csv').toString('utf8').split(/\r?\n/)[0]).join(',')),
  'El paquete no contiene correos, tokens ni columnas de contraseña');
 const paginasPaquete=[...zip.keys()].filter(n=>/^Informe\/.*pages\/[^/]+\/page\.json$/.test(n)).length;
 datos.paquete={archivos:zip.size,proyecto:zip.has('Informe/AFRISAN.pbip'),paginas:paginasPaquete};
 check(zip.has('Informe/AFRISAN.pbip')&&paginasPaquete===6,'El paquete trae el proyecto Informe/AFRISAN.pbip con 6 páginas',datos.paquete);
 const formula=csv.flatMap(n=>sinBom(zip.get(n+'.csv').toString('utf8')).split(/\r?\n/).slice(1)).some(l=>/(^|,)"?[=+@]/.test(l));
 check(!formula,'Ninguna celda de los CSV empieza con = + o @ (neutralización de fórmulas)');

 // 8. Proyecto conectado (CDP-57, preparación).
 const pro=await pedir('power-bi/proyecto-conectado',{token:jefatura,method:'POST'});
 const zp=leerZip(pro.buf);
 const nombres=[...zp.keys()];
 const bimNombre=nombres.find(n=>n.endsWith('model.bim'));
 const bim=JSON.parse(zp.get(bimNombre).toString('utf8'));
 const expr=Object.fromEntries(bim.model.expressions.map(e=>[e.name,e.expression]));
 const url=/^"([^"]+)"/.exec(expr.UrlApi)?.[1];
 const tokenProyecto=/^"([^"]+)"/.exec(expr.TokenPowerBi)?.[1];
 check(pro.status===200&&nombres.some(n=>n.endsWith('AFRISAN.pbip'))&&nombres.some(n=>n.endsWith('LEEME.txt'))&&url===origen+'/api/v1/power-bi/tablas/',
  'POST /power-bi/proyecto-conectado: ZIP con AFRISAN.pbip, LEEME y UrlApi apuntando a la API local',{http:pro.status,archivos:nombres.length,urlApi:url});
 const bimTexto=JSON.stringify(bim);
 check(!bimTexto.includes('CarpetaAfrisan')&&!bimTexto.includes('DatosSimulados'),'El modelo conectado ya no lee la carpeta de CSV simulados');
 const otAbiertas=medidaDe(bim,'OT abiertas');
 check(otAbiertas.includes('NOT (Ordenes[Estado] IN {"Cerrada", "Anulada"})'),'DEF-13: la medida «OT abiertas» del proyecto excluye las anuladas');
 const conProyecto=await pedir('power-bi/tablas/inventario',{token:tokenProyecto});
 check(conProyecto.status===200&&conProyecto.json.length===tablas.inventario.length,'El token incluido en el proyecto lee la tabla inventario',{http:conProyecto.status});
 const paginas=nombres.filter(n=>/pages\/[^/]+\/page\.json$/.test(n));
 const visuales=nombres.filter(n=>/visuals\/[^/]+\/visual\.json$/.test(n));
 const medidas=bim.model.tables.flatMap(x=>x.measures??[]);
 datos.proyecto_conectado={paginas:paginas.length,visuales:visuales.length,medidas:medidas.length,tablas:bim.model.tables.map(x=>x.name)};
 check(paginas.length===6&&visuales.length===38&&medidas.length===20&&nombres.some(n=>n.endsWith('pages/Consumo/visuals/visual_038/visual.json')),
  'El proyecto trae 6 páginas (con «06 · Consumo por sucursal»), 38 visuales y 20 medidas',datos.proyecto_conectado);
 await fs.mkdir(privado,{recursive:true});
 await fs.writeFile(path.join(privado,`AFRISAN_PowerBI_Conectado${sufijo}.zip`),pro.buf);
 // Totales que debería mostrar Power BI Desktop tras Actualizar (sin filtros de página = todo el historial).
 datos.totales_esperados_desktop={filas_por_tabla:datos.filas_por_tabla,...(({consumo,...resto})=>resto)(resumenDesdeTablas(t,{desde:'2000-01-01',hasta:'2100-12-31'})),
  kgSolicitados:Math.round(t.detalle.reduce((s,x)=>s+Number(x.kgSolicitados),0)*1000)/1000,
  kgAutorizados:Math.round(t.detalle.reduce((s,x)=>s+Number(x.kgAutorizados),0)*1000)/1000,
  consumoPorSucursal:Object.values(Object.groupBy(resumenDesdeTablas(t,{desde:'2000-01-01',hasta:'2100-12-31'}).consumo.filter(x=>x.periodo==='Mes'),x=>x.sucursal)).map(g=>({sucursal:g[0].sucursal,cliente:g[0].cliente,kgInstalados:Math.round(g.reduce((s,x)=>s+x.kgInstalados,0)*1000)/1000,kgSolicitados:Math.round(g.reduce((s,x)=>s+x.kgSolicitados,0)*1000)/1000})),
 kgDespachados:Math.round(t.despachos.reduce((s,x)=>s+Number(x.kgDespachados),0)*1000)/1000,
  totalClp:Math.round(t.costos.reduce((s,x)=>s+Number(x.totalClp??0),0))};
 report.estado=fallos?'CON FALLOS':'CORRECTO';
}catch(error){report.estado='DETENIDO';report.error=String(error?.message??error);console.error(error);process.exitCode=1;}
report.resumen={comprobaciones:checks.length,ok:checks.filter(c=>c.resultado==='OK').length,fallos};
await fs.writeFile(path.join(directory,`resultados_powerbi_supabase${sufijo}.json`),JSON.stringify(report,null,1)+'\n');
console.log(report.estado,report.resumen);
if(fallos)process.exitCode=1;
