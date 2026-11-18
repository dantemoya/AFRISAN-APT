// Defectos del recorrido del 14/11/2026 corregidos el 16/11 (D-47 a D-49, DEF-10 y prueba por rol CDP-64)
// sobre la API real y PostgreSQL embebido con los scripts 05, 07, 08, 09, 10, 11 y 12. No usa Supabase.
// Uso (desde la raíz del repositorio, después de compilar): node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/validar_defectos.mjs
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {spawn} from 'node:child_process';
import {randomBytes,randomUUID} from 'node:crypto';
import {deflateSync} from 'node:zlib';

const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const modules=path.join(root,'.local/validacion_sql_contexto/node_modules/@electric-sql');
const {PGlite}=await import(pathToFileURL(path.join(modules,'pglite/dist/index.js')));
const {PGLiteSocketServer}=await import(pathToFileURL(path.join(modules,'pglite-socket/dist/index.js')));
const db=await PGlite.create();
const socket=new PGLiteSocketServer({db,host:'127.0.0.1',port:55432,maxConnections:1});
const dll=process.env.AFRISAN_API_DLL||path.join(root,'AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api/bin/Debug/net8.0/Afrisan.Nuevo.Api.dll');
const base='http://127.0.0.1:5199/api/v1/';
const checks=[];
const report={fecha_utc:new Date().toISOString(),tipo:'API .NET 8 + PostgreSQL embebido (05, 07, 08, 09, 10, 11 y 12)',comprobaciones:checks};
const profile=path.join(root,'.local/perfil-pruebas-'+randomUUID());await fs.mkdir(profile,{recursive:true});
const environment={...process.env,APPDATA:profile,LOCALAPPDATA:profile,Logging__EventLog__LogLevel__Default:'None',ASPNETCORE_ENVIRONMENT:'Development',ASPNETCORE_URLS:'http://127.0.0.1:5199',
 ConnectionStrings__AfrisanRuntime:'Host=127.0.0.1;Port=55432;Database=postgres;Username=afrisan_api;Password=prueba-local;SSL Mode=Disable;Pooling=false',
 Persistencia__Modo:'PostgreSQL',Persistencia__PruebasLocales:'true',Persistencia__JwtKey:randomBytes(64).toString('base64'),
 Persistencia__FotosPath:path.join(root,'.local/evidencias-prueba-'+randomUUID()),PowerBiDemo__ArchivoConfiguracion:path.join(profile,'powerbi-demo.json'),PowerBiDemo__UrlPublica:''};
let processApi,logs='';
const delay=ms=>new Promise(r=>setTimeout(r,ms));
function check(ok,name){checks.push({resultado:ok?'OK':'FALLO',comprobacion:name});if(!ok)throw Error(name);console.log('OK:',name);}
function child(args=[]){const p=spawn('dotnet',[dll,...args],{cwd:root,env:environment,windowsHide:true,stdio:['pipe','pipe','pipe']});p.stdout.on('data',x=>logs+=x);p.stderr.on('data',x=>logs+=x);return p;}
async function stop(){if(processApi&&processApi.exitCode===null){const exited=new Promise(r=>processApi.once('exit',r));processApi.kill();await exited;}processApi=null;}
async function start(){processApi=child();for(let i=0;i<150;i++){if(processApi.exitCode!==null)throw Error('API no inició: '+logs.slice(-4000));try{if((await fetch('http://127.0.0.1:5199/health/ready')).ok)return;}catch{}await delay(100);}throw Error('API sin respuesta');}
async function http(route,token,body,method=body===undefined?'GET':'POST'){
 const r=await fetch(base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});
 const bytes=Buffer.from(await r.arrayBuffer());const text=bytes.toString('utf8');let result;try{result=JSON.parse(text);}catch{result=text;}
 return {status:r.status,result,bytes,type:r.headers.get('content-type')};
}
async function api(route,token,body,method=body===undefined?'GET':'POST',expected){
 const {status,result}=await http(route,token,body,method);
 if(expected!==undefined){check(status===expected,`${method} ${route.replace(/[0-9a-f-]{36}/g,'{id}')}: HTTP ${expected}`+(status===expected?'':` (recibido ${status}: ${JSON.stringify(result).slice(0,200)})`));return result;}
 if(status<200||status>299)throw Error(`${method} ${route}: ${status} ${JSON.stringify(result)}`);return result;
}
function png(color){const crc=b=>{let n=0xffffffff;for(const x of b){n^=x;for(let i=0;i<8;i++)n=n&1?(n>>>1)^0xedb88320:n>>>1;}return(n^0xffffffff)>>>0;};const chunk=(name,data)=>{const b=Buffer.concat([Buffer.from(name),data]);const out=Buffer.alloc(data.length+12);out.writeUInt32BE(data.length);b.copy(out,4);out.writeUInt32BE(crc(b),out.length-4);return out;};const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(1);ihdr.writeUInt32BE(1,4);ihdr[8]=8;ihdr[9]=2;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',deflateSync(Buffer.from([0,color,20,30]))),chunk('IEND',Buffer.alloc(0))]).toString('base64');}
const foto=color=>({id:randomUUID(),nombre:`pesaje-${color}.png`,contentType:'image/png',base64:png(color)});
const password='Solo-Pruebas-2026-'+randomUUID();
const login=async email=>(await api('sesiones',null,{email,password})).token;
const mensaje=r=>typeof r==='string'?r:(r?.detail??(r?.errors?Object.values(r.errors).flat().join(' '):null)??r?.title??JSON.stringify(r));
const guid=/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;
const hoyChile=()=>new Intl.DateTimeFormat('en-CA',{timeZone:'America/Santiago'}).format(new Date());
const masDias=(fecha,n)=>{const d=new Date(fecha+'T12:00:00Z');d.setUTCDate(d.getUTCDate()+n);return d.toISOString().slice(0,10);};

try{
 for(const script of ['05_Crear_Afrisan_Sin_Emisiones','07_Habilitar_Persistencia','08_Rectificacion_Pesajes','09_Mantenedores_Bajas','10_Costos_Limites','11_Devolucion_Anulacion_Recarga','12_Codigos_Fotos_Pedido'])
  await db.exec(await fs.readFile(path.join(root,`AFRISAN_NUEVO/02_BASE_DATOS/${script}.sql`),'utf8'));
 check(true,'Los scripts 05, 07, 08, 09, 10, 11 y 12 se aplican en orden sobre una base vacía');
 await db.exec(await fs.readFile(path.join(root,'AFRISAN_NUEVO/02_BASE_DATOS/12_Codigos_Fotos_Pedido.sql'),'utf8'));
 check(true,'La migración 12 se puede aplicar dos veces sin error');
 await db.exec("ALTER ROLE afrisan_api LOGIN PASSWORD 'prueba-local'");
 await db.exec('SET ROLE afrisan_api');
 await socket.start();
 const bootstrap=child(['--crear-administrador']);bootstrap.stdin.end('Prueba\nAdministrador\nadmin@example.invalid\n'+password+'\n'+password+'\n');
 if(await new Promise(r=>bootstrap.once('exit',r))!==0)throw Error('Bootstrap falló: '+logs.slice(-4000));
 await start();
 const admin=await login('admin@example.invalid');
 for(const rol of ['Bodega','Autorizador','Tecnico','Jefatura'])await api('usuarios',admin,{nombre:'Prueba',apellido:rol,correo:rol.toLowerCase()+'@example.invalid',rol,password});
 const bodega=await login('bodega@example.invalid'),autorizador=await login('autorizador@example.invalid'),tecnico=await login('tecnico@example.invalid'),jefatura=await login('jefatura@example.invalid');
 const cliente=await api('clientes',admin,{rut:'PRUEBA-12',nombre:'Cliente sintético'});
 const sucursal=await api('sucursales',admin,{clienteId:cliente.id,nombre:'Sucursal sintética',direccion:'Dirección de prueba'});
 const gas=await api('refrigerantes',bodega,{codigo:'GAS-12',nombre:'Gas de prueba'});
 const alta=(ubicacion,peso,f)=>({ubicacionInicial:ubicacion,sucursalId:ubicacion==='Sucursal'?sucursal.id:null,refrigeranteId:gas.id,capacidadGr:10000,taraGr:1000,pesoInicialGr:peso,foto:f});

 // D-48: alta con peso y primer pesaje exigen foto.
 let r=await api('cilindros',bodega,alta('Bodega',6000,null),'POST',400);
 check(/foto/i.test(mensaje(r)),'El alta con peso inicial y sin foto se rechaza con un mensaje sobre la foto');
 const viejo=await api('cilindros',bodega,alta('Sucursal',2500,foto(1)));
 const lleno=await api('cilindros',bodega,alta('Bodega',6000,foto(2)));
 const pendiente=await api('cilindros',bodega,alta('Bodega',null,null));
 check(pendiente.estado==='PendientePesaje','El alta sin peso no exige foto y queda pendiente de pesaje');
 await api(`cilindros/${pendiente.codigo}/pesaje-inicial`,bodega,{pesoBrutoGr:3000},'POST',400);
 const pesado=await api(`cilindros/${pendiente.codigo}/pesaje-inicial`,bodega,{pesoBrutoGr:3000,foto:foto(3)});
 check(pesado.estado==='Disponible','El pesaje inicial con foto deja el cilindro disponible');

 // D-47: códigos cortos generados por la base.
 check(viejo.codigo==='AF-00001'&&lleno.codigo==='AF-00002'&&pendiente.codigo==='AF-00003','Los cilindros reciben códigos AF-00001, AF-00002 y AF-00003');
 const pedido=(desc,extra={})=>({idOperacion:randomUUID(),sucursalId:sucursal.id,sucursal:sucursal.nombre,direccion:sucursal.direccion,descripcion:desc,tecnicoEmail:'tecnico@example.invalid',
  detalle:[{refrigeranteId:gas.id,grSolicitados:5000,grAutorizados:5000}],cilindrosARetirar:[viejo.codigo],
  solicitante:'Jefe de local (prueba)',prioridad:'Alta',fechaProgramada:masDias(hoyChile(),1),...extra});

 // D-49: solicitante, prioridad y fecha programada.
 await api('ordenes-trabajo',autorizador,pedido('Sin solicitante',{solicitante:'  '}),'POST',400);
 await api('ordenes-trabajo',autorizador,pedido('Prioridad inválida',{prioridad:'Maxima'}),'POST',400);
 await api('ordenes-trabajo',autorizador,pedido('Sin fecha',{fechaProgramada:null}),'POST',400);
 r=await api('ordenes-trabajo',autorizador,pedido('Fecha pasada',{fechaProgramada:masDias(hoyChile(),-1)}),'POST',400);
 check(/anterior a hoy/i.test(mensaje(r)),'Una fecha programada anterior a hoy se rechaza');
 let ot=await api('ordenes-trabajo',autorizador,pedido('Reemplazo con fotos'));
 check(ot.codigo==='OT-00001'&&ot.codigo.length<=8,'La primera OT recibe el código OT-00001 (8 caracteres)');
 check(ot.solicitante==='Jefe de local (prueba)'&&ot.prioridad==='Alta'&&ot.fechaProgramada===masDias(hoyChile(),1),'La OT guarda y devuelve solicitante, prioridad y fecha programada');
 const altaOt=ot.historial.find(h=>h.accion==='OT autorizada');
 check(/^Prioridad Alta · programada para el \d\d\/\d\d\/\d{4} · solicitada por Jefe de local \(prueba\)$/.test(altaOt?.descripcion??''),'El historial describe el pedido en texto legible');
 check(altaOt?.usuario==='Prueba Autorizador','El historial muestra el nombre del usuario, no su identificador');
 await api('ordenes-trabajo',jefatura,pedido('Jefatura no crea OT'),'POST',403);
 const rot=`ordenes-trabajo/${ot.id}`;

 // Recorrido completo: cada pesaje con foto.
 ot=await api(rot+'/entregas',tecnico,{idOperacion:randomUUID(),codigo:lleno.codigo,pesoBrutoGr:6000,foto:foto(10)});
 const opId=randomUUID();const inst=await api('entorno');
 let op=await api(rot+'/operaciones',tecnico,{id:opId,instancia:inst.instancia,version:0,tipo:'Reemplazo',codigoRetirado:viejo.codigo,codigoInstalado:lleno.codigo,pesoRetiradoGr:2450,pesoInstaladoGr:5950},'PUT');
 op=await api(rot+`/operaciones/${opId}/fotos`,tecnico,{...foto(20),grupo:'Retirado',version:op.version});
 op=await api(rot+`/operaciones/${opId}/fotos`,tecnico,{...foto(30),grupo:'Instalado',version:op.version});
 op=await api(rot+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST');
 check(op.estado==='Registrada','El cambio con fotos se confirma con la foto obligatoria activa en la base');
 await api(rot+`/operaciones/${opId}/informar-entrega`,tecnico,undefined,'POST');
 r=await api(rot+'/recepciones',bodega,{codigo:viejo.codigo,pesoBrutoGr:2400},'POST',400);
 check(/foto/i.test(mensaje(r)),'La recepción de Bodega sin foto se rechaza');
 let c=(await api('cilindros',bodega)).find(x=>x.codigo===viejo.codigo);
 check(c.ubicacion!=='Bodega','La recepción rechazada no mueve el cilindro');
 const fotoRecepcion=foto(40);
 ot=await api(rot+'/recepciones',bodega,{codigo:viejo.codigo,pesoBrutoGr:2400,foto:fotoRecepcion,observacion:'Recepción con foto'});
 c=(await api('cilindros',bodega)).find(x=>x.codigo===viejo.codigo);
 check(c.ubicacion==='Bodega','Con foto, Bodega recibe el cilindro retirado');

 // DEF-10: historial legible.
 const retiro=ot.historial.find(h=>h.accion==='Retiro de Bodega por Técnico');
 check(retiro?.descripcion===`Cilindro ${lleno.codigo} · 6.000 g`,'El retiro de Bodega se describe con código y peso, sin identificador numérico');
 const recepcion=ot.historial.find(h=>h.accion==='Recepción oficial Bodega');
 check(recepcion?.descripcion===`Cilindro ${viejo.codigo} · 2.400 g`,'La recepción se describe con código y peso');
 const cambio=ot.historial.find(h=>h.accion==='Cambio físico confirmado');
 check(cambio?.descripcion===`Reemplazo: instalado ${lleno.codigo}, retirado ${viejo.codigo}`,'El cambio físico se describe con los códigos instalado y retirado');
 check(ot.historial.every(h=>!guid.test(h.descripcion)&&!guid.test(h.usuario)),'Ningún evento del historial muestra un GUID');

 // D-48: consulta de pesajes con foto para verificar el monto.
 const pesajes=await api(`cilindros/${viejo.codigo}/pesajes`,bodega);
 check(JSON.stringify(pesajes.map(p=>p.tipo))===JSON.stringify(['Recepcion','Retirado','Inicial']),'El cilindro retirado muestra sus pesajes Recepción, Retirado e Inicial');
 check(pesajes.every(p=>p.fotos.length>0),'Cada pesaje del cilindro tiene al menos una foto');
 const pRec=pesajes[0];
 check(pRec.pesoBrutoGr===2400&&pRec.orden==='OT-00001'&&pRec.usuario==='Prueba Bodega','El pesaje de recepción muestra peso, OT y usuario');
 const archivo=await http(`cilindros/${viejo.codigo}/pesajes/fotos/${pRec.fotos[0].id}`,bodega);
 check(archivo.status===200&&archivo.type?.startsWith('image/png')&&archivo.bytes.equals(Buffer.from(fotoRecepcion.base64,'base64')),'La foto de la recepción se descarga idéntica a la enviada');
 for(const [rol,token] of [['Administrador',admin],['Autorizador',autorizador],['Jefatura',jefatura]])
  await api(`cilindros/${viejo.codigo}/pesajes`,token,undefined,'GET',200);
 await api(`cilindros/${viejo.codigo}/pesajes`,tecnico,undefined,'GET',403);
 await api(`cilindros/${viejo.codigo}/pesajes/fotos/${pRec.fotos[0].id}`,tecnico,undefined,'GET',403);
 await api(`cilindros/${lleno.codigo}/pesajes/fotos/${pRec.fotos[0].id}`,bodega,undefined,'GET',404);
 check(true,'La foto de un cilindro no se obtiene a través de otro cilindro');

 // CDP-64 (local): cada rol ve lo suyo y nada más.
 const matriz=[
  ['GET','cilindros',{Administrador:200,Bodega:200,Autorizador:200,Jefatura:200,Tecnico:403}],
  ['GET','usuarios',{Administrador:200,Bodega:403,Autorizador:403,Jefatura:403,Tecnico:403}],
  ['GET','ordenes-trabajo',{Administrador:200,Bodega:200,Autorizador:200,Jefatura:200,Tecnico:200}],
  ['GET',rot,{Bodega:200,Autorizador:200,Tecnico:200}],
 ];
 const tokens={Administrador:admin,Bodega:bodega,Autorizador:autorizador,Jefatura:jefatura,Tecnico:tecnico};
 for(const [metodo,ruta,esperado] of matriz)for(const [rol,estado] of Object.entries(esperado)){
  const {status}=await http(ruta,tokens[rol],undefined,metodo);
  check(status===estado,`CDP-64: ${rol} ${metodo} ${ruta.replace(/[0-9a-f-]{36}/g,'{id}')} responde ${estado}`+(status===estado?'':` (recibido ${status})`));
 }
 const listaTecnico=await api('ordenes-trabajo',tecnico);
 check(listaTecnico.every(o=>o.tecnicoEmail==='tecnico@example.invalid'),'CDP-64: el técnico solo lista sus propias OT');

 // Búsqueda de la ficha (17/11): por código sin distinguir mayúsculas y, si no existe, por número de serie.
 const conSerie=await api('cilindros',bodega,{...alta('Bodega',null,null),numeroSerie:'SER-12'});
 check((await api(`cilindros/${lleno.codigo.toLowerCase()}`,tecnico)).codigo===lleno.codigo,'El técnico abre la ficha con el código en minúsculas');
 check((await api('cilindros/ser-12',tecnico)).codigo===conSerie.codigo,'La ficha también se encuentra por número de serie');
 r=await api('cilindros/NO-EXISTE',tecnico,undefined,'GET',404);
 check(/código o número de serie/.test(mensaje(r)),'Un código inexistente responde 404 con un mensaje claro');
 // D-48 en la base: un pesaje insertado sin evidencia no se confirma.
 await db.exec('RESET ROLE');
 const ids=(await db.query(`SELECT (SELECT "Id_Cilindro" FROM afrisan."Cilindros" WHERE "CilindroQR"='${pendiente.codigo}') AS c,(SELECT "Id_Usuario" FROM afrisan."Usuario" WHERE "Correo_Usuario"='bodega@example.invalid') AS u`)).rows[0];
 let error='';
 try{await db.transaction(async tx=>{await tx.query(`SELECT set_config('afrisan.id_usuario','${ids.u}',true)`);
  await tx.query(`INSERT INTO afrisan."Pesaje"("Id_Cilindro","Id_OrdenTrabajo","Tipo","PesoBrutoGr","Id_Usuario") VALUES(${ids.c},NULL,'Recarga',7000,${ids.u})`);});}
 catch(e){error=e.message;}
 check(/requiere al menos una foto/.test(error),'La base rechaza al confirmar un pesaje sin foto (restricción diferida)'+(/requiere al menos una foto/.test(error)?'':` (${error||'no hubo error'})`));
 error='';
 try{await db.query(`UPDATE afrisan."OrdenTrabajo" SET "Prioridad"='Baja' WHERE "CodigoOT"='OT-00001'`);}catch(e){error=e.message;}
 check(/no se modifican|No reescribir OT autorizada/.test(error),'La base no permite cambiar la prioridad de una OT creada'+(error?` (${error})`:' (no hubo error)'));
 error='';
 try{await db.transaction(async tx=>{await tx.query(`SELECT set_config('afrisan.id_usuario',(SELECT "Id_Usuario"::text FROM afrisan."Usuario" WHERE "Correo_Usuario"='autorizador@example.invalid'),true)`);
  await tx.query(`INSERT INTO afrisan."OrdenTrabajo"("Id_Publico","Id_Sucursal","Id_Tecnico","Id_Autorizador","Descripcion","HuellaCreacion") SELECT gen_random_uuid(),"Id_Sucursal","Id_Tecnico","Id_Autorizador",'Sin pedido','x' FROM afrisan."OrdenTrabajo" LIMIT 1`);});}catch(e){error=e.message;}
 check(/requiere solicitante, prioridad y fecha programada/.test(error),'La base rechaza una OT sin datos del pedido'+(/requiere solicitante/.test(error)?'':` (${error||'no hubo error'})`));
 const largos=(await db.query(`SELECT count(*)::int AS n FROM afrisan."Cilindros" WHERE length("CilindroQR")>8`)).rows[0].n+(await db.query(`SELECT count(*)::int AS n FROM afrisan."OrdenTrabajo" WHERE length("CodigoOT")>8`)).rows[0].n;
 check(largos===0,'Ningún código creado supera 8 caracteres');
 const sinFoto=(await db.query(`SELECT count(*)::int AS n FROM afrisan."Pesaje" p WHERE NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" e WHERE e."Id_Pesaje"=p."Id_Pesaje")`)).rows[0].n;
 check(sinFoto===0,'Todos los pesajes guardados tienen foto');
 const sinRls=(await db.query("SELECT count(*)::int AS n FROM pg_tables WHERE schemaname='afrisan' AND NOT rowsecurity")).rows[0].n;
 check(sinRls===0,'RLS sigue habilitado en todas las tablas después de la migración 12');
 report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);if(process.env.AFRISAN_LOGS)console.error(logs.slice(-6000));process.exitCode=1;}
finally{
 await stop();await socket.stop();await db.close();
 report.cantidad_comprobaciones=checks.length;
 await fs.writeFile(path.join(directory,'resultados_defectos.json'),JSON.stringify(report,null,2));
 await fs.rm(profile,{recursive:true,force:true});await fs.rm(environment.Persistencia__FotosPath,{recursive:true,force:true});
}
