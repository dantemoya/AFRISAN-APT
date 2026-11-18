// CDP-64: prueba de humo por rol contra una API con la base de Supabase (local o Azure).
// Comprueba la salud de la API, que cada cuenta de prueba entra y que cada rol ve lo suyo y nada más.
// Con ESCRIBIR=1 además crea un cilindro, una OT de agregado y la lleva hasta el cierre con fotos
// (datos de prueba nuevos, serie P1611-*; se limpian junto con los del 14/11).
// Las claves de las cuentas de prueba están fuera del repositorio (.local/cuentas_prueba_2026_11_14.json).
// El rol Administrador se prueba solo si se entregan AFRISAN_ADMIN_CORREO y AFRISAN_ADMIN_CLAVE.
// Uso (desde la raíz del repositorio):
//   node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/humo_por_rol.mjs                       (API local, puerto 5181)
//   Git Bash:   AFRISAN_API=https://afrisan-api-dm.azurewebsites.net ESCRIBIR=1 node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/humo_por_rol.mjs
//   PowerShell: $env:AFRISAN_API="https://afrisan-api-dm.azurewebsites.net"; $env:ESCRIBIR="1"; node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/humo_por_rol.mjs
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {randomUUID} from 'node:crypto';
import {deflateSync} from 'node:zlib';

const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const origen=(process.env.AFRISAN_API??'http://localhost:5181').replace(/\/+$/,'');
const base=origen+'/api/v1/';
const escribir=process.env.ESCRIBIR==='1';
const cuentas=JSON.parse(await fs.readFile(path.join(root,'.local/cuentas_prueba_2026_11_14.json'),'utf8'));
if(process.env.AFRISAN_ADMIN_CORREO&&process.env.AFRISAN_ADMIN_CLAVE)cuentas.push({rol:'Administrador',correo:process.env.AFRISAN_ADMIN_CORREO,password:process.env.AFRISAN_ADMIN_CLAVE});
const checks=[];const datos={};
const report={fecha_utc:new Date().toISOString(),tipo:`CDP-64 por rol contra ${origen}`+(escribir?' (con OT completa)':' (solo lectura)'),comprobaciones:checks,datos_creados:datos};
function check(ok,name){checks.push({resultado:ok?'OK':'FALLO',comprobacion:name});if(!ok)throw Error(name);console.log('OK:',name);}
async function http(route,token,body,method=body===undefined?'GET':'POST'){
 const r=await fetch(route.startsWith('http')?route:base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});
 const bytes=Buffer.from(await r.arrayBuffer());let result;try{result=JSON.parse(bytes.toString('utf8'));}catch{result=bytes.toString('utf8');}
 return {status:r.status,result,bytes,headers:r.headers};
}
async function api(route,token,body,method){const {status,result}=await http(route,token,body,method);if(status<200||status>299)throw Error(`${method??(body===undefined?'GET':'POST')} ${route}: ${status} ${JSON.stringify(result).slice(0,300)}`);return result;}
function png(color){const crc=b=>{let n=0xffffffff;for(const x of b){n^=x;for(let i=0;i<8;i++)n=n&1?(n>>>1)^0xedb88320:n>>>1;}return(n^0xffffffff)>>>0;};const chunk=(name,data)=>{const b=Buffer.concat([Buffer.from(name),data]);const out=Buffer.alloc(data.length+12);out.writeUInt32BE(data.length);b.copy(out,4);out.writeUInt32BE(crc(b),out.length-4);return out;};const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(1);ihdr.writeUInt32BE(1,4);ihdr[8]=8;ihdr[9]=2;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',deflateSync(Buffer.from([0,color,90,140]))),chunk('IEND',Buffer.alloc(0))]).toString('base64');}
const semilla=Date.now()%200;
const foto=(nombre,n)=>({id:randomUUID(),nombre:`${nombre}.png`,contentType:'image/png',base64:png((semilla+n)%256)});
const guid=/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;
const hoyChile=()=>new Intl.DateTimeFormat('en-CA',{timeZone:'America/Santiago'}).format(new Date());

try{
 // Salud y transporte.
 const salud=await http(origen+'/health/ready');
 check(salud.status===200&&salud.result.modo==='Persistente'&&salud.result.almacenamiento==='PostgreSQL','La API responde con la base PostgreSQL de Supabase');
 report.fotografias=salud.result.fotografias;
 console.log('Fotografías en:',salud.result.fotografias);
 check((await http('cilindros')).status===401,'Sin sesión la API responde 401');

 const tokens={};
 for(const c of cuentas){
  const {status,result}=await http('sesiones',null,{email:c.correo,password:c.password});
  check(status===200&&result.rol===c.rol,`Ingresa la cuenta de prueba de ${c.rol}`);
  tokens[c.rol]=result.token;
 }
 const roles=Object.keys(tokens);
 const lectura=(await api('cilindros',tokens.Bodega)).find(c=>c.pesajeRegistrado||c.ultimoPesaje);
 const matriz=[
  ['GET','cilindros',{Administrador:200,Bodega:200,Autorizador:200,Jefatura:200,Tecnico:403}],
  ['GET','usuarios',{Administrador:200,Bodega:403,Autorizador:403,Jefatura:403,Tecnico:403}],
  ['GET','ordenes-trabajo',{Administrador:200,Bodega:200,Autorizador:200,Jefatura:200,Tecnico:200}],
  ['GET','costos',{Administrador:200,Bodega:403,Autorizador:403,Jefatura:200,Tecnico:403}],
  ['GET','limites',{Administrador:403,Bodega:200,Autorizador:200,Jefatura:200,Tecnico:403}],
  ['POST','ordenes-trabajo',{Administrador:403,Bodega:403,Jefatura:403,Tecnico:403}],
  ['POST','cilindros',{Autorizador:403,Jefatura:403,Tecnico:403}],
 ];
 if(lectura)matriz.push(['GET',`cilindros/${encodeURIComponent(lectura.codigo)}/pesajes`,{Administrador:200,Bodega:200,Autorizador:200,Jefatura:200,Tecnico:403}]);
 for(const [metodo,ruta,esperado] of matriz)for(const rol of roles){
  if(!(rol in esperado))continue;
  const {status}=await http(ruta,tokens[rol],metodo==='POST'?{}:undefined,metodo);
  check(status===esperado[rol],`${rol}: ${metodo} ${ruta.replace(/cilindros\/[^/]+\/pesajes/,'cilindros/{código}/pesajes')} responde ${esperado[rol]}`+(status===esperado[rol]?'':` (recibido ${status})`));
 }
 const tecnicoEmail=cuentas.find(x=>x.rol==='Tecnico').correo;
 const propias=await api('ordenes-trabajo',tokens.Tecnico);
 check(propias.every(o=>o.tecnicoEmail.toLowerCase()===tecnicoEmail.toLowerCase()),'El técnico solo lista sus propias OT');

 if(escribir){
  const {Bodega:bodega,Autorizador:autorizador,Tecnico:tecnico}=tokens;
  const sucursal=(await api('sucursales',bodega)).find(s=>s.nombre==='Local Maipú (prueba)');
  const gas=(await api('refrigerantes',bodega)).find(g=>g.codigo==='R404A-PRUEBA');
  check(sucursal&&gas,'Existen la sucursal y el gas de prueba del 14/11');
  const serie=`P1611-${Date.now().toString(36).toUpperCase()}`;
  const sinFoto=await http('cilindros',bodega,{ubicacionInicial:'Bodega',refrigeranteId:gas.id,capacidadGr:10000,taraGr:5000,pesoInicialGr:14000,numeroSerie:serie+'-X'});
  check(sinFoto.status===400,'El alta con peso y sin foto se rechaza (D-48)');
  const cil=await api('cilindros',bodega,{ubicacionInicial:'Bodega',refrigeranteId:gas.id,capacidadGr:10000,taraGr:5000,pesoInicialGr:14000,numeroSerie:serie,foto:foto('alta',1)});
  datos.cilindro=cil.codigo;
  check(/^AF-\d{5}$/.test(cil.codigo),`El cilindro nuevo recibe un código corto (${cil.codigo})`);
  let ot=await api('ordenes-trabajo',autorizador,{idOperacion:randomUUID(),sucursalId:sucursal.id,sucursal:sucursal.nombre,direccion:sucursal.direccion,descripcion:`Prueba CDP-64 ${serie}`,
   tecnicoEmail,detalle:[{refrigeranteId:gas.id,grSolicitados:9000,grAutorizados:9000}],cilindrosARetirar:[],solicitante:'Jefe de local (prueba)',prioridad:'Urgente',fechaProgramada:hoyChile()});
  datos.ot=ot.codigo;
  check(/^OT-\d{5}$/.test(ot.codigo)&&ot.prioridad==='Urgente'&&ot.solicitante==='Jefe de local (prueba)',`La OT nueva tiene código corto (${ot.codigo}) y los datos del pedido (D-49)`);
  const r=`ordenes-trabajo/${ot.id}`;
  ot=await api(r+'/entregas',tecnico,{idOperacion:randomUUID(),codigo:cil.codigo,pesoBrutoGr:14000,foto:foto('salida',2)});
  const opId=randomUUID();const inst=await api('entorno');
  let op=await api(r+'/operaciones',tecnico,{id:opId,instancia:inst.instancia,version:0,tipo:'Agregado',codigoInstalado:cil.codigo,pesoInstaladoGr:13950},'PUT');
  op=await api(r+`/operaciones/${opId}/fotos`,tecnico,{...foto('instalado',3),grupo:'Instalado',version:op.version});
  op=await api(r+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST');
  check(op.estado==='Registrada','El técnico registra el agregado con foto');
  ot=await api(r+'/termino-tecnico',tecnico,{observacion:'Prueba CDP-64'});
  ot=await api(r+'/aprobar-bodega',bodega,{confirmoRevision:true,observacion:'Prueba CDP-64'});
  ot=await api(r+'/revision-autorizador',autorizador,{confirmoRevision:true,observacion:'Prueba CDP-64'});
  ot=await api(r+'/cerrar',autorizador,undefined,'POST');
  check(ot.estado==='Cerrada','La OT de prueba pasa por los tres roles y se cierra');
  check(ot.historial.every(h=>!guid.test(h.descripcion??'')),'El historial de la OT no muestra GUID (DEF-10)');
  const pesajes=await api(`cilindros/${cil.codigo}/pesajes`,bodega);
  check(pesajes.length===3&&pesajes.every(p=>p.fotos.length>0),'Los pesajes Inicial, Despacho e Instalado tienen su foto');
  const f=await http(`cilindros/${cil.codigo}/pesajes/fotos/${pesajes[0].fotos[0].id}`,bodega);
  check(f.status===200&&f.headers.get('content-type')?.startsWith('image/'),'La foto del último pesaje se puede consultar para verificar el monto');
 }
 report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{
 report.cantidad_comprobaciones=checks.length;
 const destino=origen.includes('localhost')||origen.includes('127.0.0.1')?'resultados_humo_local.json':'resultados_humo_azure.json';
 await fs.writeFile(path.join(directory,destino),JSON.stringify(report,null,2));
 console.log(`${report.estado}: ${checks.length} comprobaciones → ${destino}`);
}
