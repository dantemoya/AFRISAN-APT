// Correcciones del recorrido del 14/11/2026 (D-39 a D-43) sobre la API real y PostgreSQL embebido
// con los scripts 05, 07, 08, 09, 10 y 11. No usa Supabase ni sus credenciales.
// Uso (desde la raíz del repositorio, después de compilar): node AFRISAN_NUEVO/04_SEGUIMIENTO/CORRECCIONES_2026_11_14/validar_correcciones.mjs
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
const report={fecha_utc:new Date().toISOString(),tipo:'API .NET 8 + PostgreSQL embebido (05, 07, 08, 09, 10 y 11)',comprobaciones:checks};
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
async function api(route,token,body,method=body===undefined?'GET':'POST',expected){
 const r=await fetch(base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});
 const text=await r.text();let result;try{result=JSON.parse(text);}catch{result=text;}
 if(expected!==undefined){check(r.status===expected,`${method} ${route}: HTTP ${expected}`+(r.status===expected?'':` (recibido ${r.status}: ${text.slice(0,200)})`));return result;}
 if(!r.ok)throw Error(`${method} ${route}: ${r.status} ${text}`);return result;
}
function png(color){const crc=b=>{let n=0xffffffff;for(const x of b){n^=x;for(let i=0;i<8;i++)n=n&1?(n>>>1)^0xedb88320:n>>>1;}return(n^0xffffffff)>>>0;};const chunk=(name,data)=>{const b=Buffer.concat([Buffer.from(name),data]);const out=Buffer.alloc(data.length+12);out.writeUInt32BE(data.length);b.copy(out,4);out.writeUInt32BE(crc(b),out.length-4);return out;};const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(1);ihdr.writeUInt32BE(1,4);ihdr[8]=8;ihdr[9]=2;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',deflateSync(Buffer.from([0,color,20,30]))),chunk('IEND',Buffer.alloc(0))]).toString('base64');}
const foto=color=>({id:randomUUID(),nombre:`pesaje-${color}.png`,contentType:'image/png',base64:png(color)});
const password='Solo-Pruebas-2026-'+randomUUID();
const login=async email=>(await api('sesiones',null,{email,password})).token;
const mensaje=r=>typeof r==='string'?r:(r?.detail??r?.title??JSON.stringify(r));

try{
 for(const script of ['05_Crear_Afrisan_Sin_Emisiones','07_Habilitar_Persistencia','08_Rectificacion_Pesajes','09_Mantenedores_Bajas','10_Costos_Limites','11_Devolucion_Anulacion_Recarga'])
  await db.exec(await fs.readFile(path.join(root,`AFRISAN_NUEVO/02_BASE_DATOS/${script}.sql`),'utf8'));
 check(true,'Los scripts 05, 07, 08, 09, 10 y 11 se aplican en orden sobre una base vacía');
 await db.exec(await fs.readFile(path.join(root,'AFRISAN_NUEVO/02_BASE_DATOS/11_Devolucion_Anulacion_Recarga.sql'),'utf8'));
 check(true,'La migración 11 se puede aplicar dos veces sin error');
 await db.exec("ALTER ROLE afrisan_api LOGIN PASSWORD 'prueba-local'");
 await db.exec('SET ROLE afrisan_api');
 await socket.start();
 const bootstrap=child(['--crear-administrador']);bootstrap.stdin.end('Prueba\nAdministrador\nadmin@example.invalid\n'+password+'\n'+password+'\n');
 if(await new Promise(r=>bootstrap.once('exit',r))!==0)throw Error('Bootstrap falló: '+logs.slice(-4000));
 await start();
 const admin=await login('admin@example.invalid');
 for(const rol of ['Bodega','Autorizador','Tecnico'])await api('usuarios',admin,{nombre:'Prueba',apellido:rol,correo:rol.toLowerCase()+'@example.invalid',rol,password});
 const bodega=await login('bodega@example.invalid'),autorizador=await login('autorizador@example.invalid'),tecnico=await login('tecnico@example.invalid');
 const cliente=await api('clientes',admin,{rut:'PRUEBA-11',nombre:'Cliente sintético'});
 const sucursal=await api('sucursales',admin,{clienteId:cliente.id,nombre:'Sucursal sintética',direccion:'Dirección de prueba'});
 const gas=await api('refrigerantes',bodega,{codigo:'GAS-11',nombre:'Gas de prueba'});
 const cilindro=(ubicacion,peso)=>api('cilindros',bodega,{ubicacionInicial:ubicacion,sucursalId:ubicacion==='Sucursal'?sucursal.id:null,refrigeranteId:gas.id,capacidadGr:10000,taraGr:1000,pesoInicialGr:peso});
 const viejo=await cilindro('Sucursal',2500),lleno=await cilindro('Bodega',6000);
 const pedido=desc=>({idOperacion:randomUUID(),sucursalId:sucursal.id,sucursal:sucursal.nombre,direccion:sucursal.direccion,descripcion:desc,tecnicoEmail:'tecnico@example.invalid',
  detalle:[{refrigeranteId:gas.id,grSolicitados:5000,grAutorizados:5000}],cilindrosARetirar:[viejo.codigo]});

 // D-40: anulación de una OT sin retiros.
 let ot=await api('ordenes-trabajo',autorizador,pedido('OT que se anula'));
 await api(`ordenes-trabajo/${ot.id}/anular`,bodega,{motivo:'Bodega no puede anular'},'POST',403);
 await api(`ordenes-trabajo/${ot.id}/anular`,autorizador,{motivo:'no'},'POST',400);
 ot=await api(`ordenes-trabajo/${ot.id}/anular`,autorizador,{motivo:'El cliente canceló la visita'});
 check(ot.estado==='Anulada'&&ot.motivoAnulacion==='El cliente canceló la visita','El Autorizador anula una OT asignada con motivo');
 check(ot.historial.some(h=>h.accion==='OT anulada'),'La anulación queda en el historial de la OT');
 await api(`ordenes-trabajo/${ot.id}/entregas`,tecnico,{idOperacion:randomUUID(),codigo:lleno.codigo,pesoBrutoGr:6000,foto:foto(5)},'POST',409);
 let ot2=await api('ordenes-trabajo',autorizador,pedido('Reemplazo con correcciones'));
 check(ot2.cilindrosARetirar.includes(viejo.codigo),'Anular libera la reserva: el mismo cilindro se reserva en otra OT');
 const r2=`ordenes-trabajo/${ot2.id}`;

 // Retiro de Bodega y anulación ya no permitida.
 ot2=await api(r2+'/entregas',tecnico,{idOperacion:randomUUID(),codigo:lleno.codigo,pesoBrutoGr:6000,foto:foto(10)});
 await api(r2+'/anular',autorizador,{motivo:'Ya hay un retiro de Bodega'},'POST',409);

 // D-42: tolerancias de peso al confirmar.
 const opId=randomUUID();const inst=await api('entorno');
 let op=await api(r2+'/operaciones',tecnico,{id:opId,instancia:inst.instancia,version:0,tipo:'Reemplazo',codigoRetirado:viejo.codigo,codigoInstalado:lleno.codigo,pesoRetiradoGr:2800,pesoInstaladoGr:5900,observacion:'Prueba de tolerancia'},'PUT');
 op=await api(r2+`/operaciones/${opId}/fotos`,tecnico,{...foto(20),grupo:'Retirado',version:op.version});
 op=await api(r2+`/operaciones/${opId}/fotos`,tecnico,{...foto(30),grupo:'Instalado',version:op.version});
 let rechazo=await api(r2+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST',409);
 check(/más que su último peso registrado/.test(mensaje(rechazo)),'Un retirado que pesa 300 g más que su último peso se rechaza');
 op=await api(r2+'/operaciones',tecnico,{id:opId,instancia:inst.instancia,version:op.version,tipo:'Reemplazo',codigoRetirado:viejo.codigo,codigoInstalado:lleno.codigo,pesoRetiradoGr:2400,pesoInstaladoGr:5600,observacion:'Prueba de tolerancia'},'PUT');
 rechazo=await api(r2+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST',409);
 check(/difiere en más de 200 g/.test(mensaje(rechazo)),'Un instalado 400 g bajo el peso de salida se rechaza');
 op=await api(r2+'/operaciones',tecnico,{id:opId,instancia:inst.instancia,version:op.version,tipo:'Reemplazo',codigoRetirado:viejo.codigo,codigoInstalado:lleno.codigo,pesoRetiradoGr:2400,pesoInstaladoGr:5900,observacion:'Prueba de tolerancia'},'PUT');
 op=await api(r2+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST');
 check(op.estado==='Registrada','Con 100 g de diferencia (dentro de la tolerancia) el cambio se confirma');
 await api(r2+`/operaciones/${opId}/informar-entrega`,tecnico,undefined,'POST');
 ot2=await api(r2+'/termino-tecnico',tecnico,{observacion:'Primer envío a Bodega'});
 check(ot2.estado==='PendienteBodega','El técnico envía la OT a Bodega');
 check(ot2.historial.some(h=>h.accion==='Enviada a revisión de Bodega'),'El envío a Bodega queda en el historial');

 await api(r2+'/recepciones',bodega,{codigo:viejo.codigo,pesoBrutoGr:2380,observacion:'Recepción de prueba'});

 // D-41: incidencias y rectificaciones pendientes bloquean la aprobación de Bodega.
 const incidencia=randomUUID();
 await api(r2+'/incidencias',bodega,{id:incidencia,operacionId:opId,tipo:'Incidencia',motivo:'La foto del retirado no se lee'});
 rechazo=await api(r2+'/aprobar-bodega',bodega,{confirmoRevision:true,observacion:'Intento con incidencia abierta'},'POST',409);
 check(/Incidencias sin revisar/.test(mensaje(rechazo)),'Bodega no aprueba con una incidencia abierta'+(/Incidencias sin revisar/.test(mensaje(rechazo))?'':` (mensaje: ${mensaje(rechazo)})`));

 // D-39: devolución al técnico.
 await api(r2+'/devolver',tecnico,{motivo:'El técnico no se devuelve'},'POST',403);
 await api(r2+'/devolver',bodega,{motivo:'no'},'POST',400);
 ot2=await api(r2+'/devolver',bodega,{motivo:'Tomar otra foto legible del retirado'});
 check(ot2.estado==='EnTerreno'&&ot2.motivoDevolucion==='Tomar otra foto legible del retirado','Bodega devuelve la OT al técnico con el motivo');
 check(ot2.historial.some(h=>h.accion==='Devuelta al técnico por Bodega'),'La devolución queda en el historial');
 ot2=await api(r2+`/incidencias/${incidencia}/resolver`,autorizador,{respuesta:'Se aceptan los pesos; la foto nueva queda en la rectificación'});
 check(ot2.incidencias.every(i=>i.respuesta),'El Autorizador responde la incidencia antes de la aprobación de Bodega');
 const pesajes=await api(r2+'/rectificaciones/pesajes',tecnico);
 const retirado=pesajes.find(p=>p.tipo==='Retirado');
 const rect=randomUUID();
 await api(r2+'/rectificaciones',tecnico,{id:rect,pesajeId:retirado.id,pesoPropuestoGr:2380,motivo:'Repesaje con foto legible',foto:foto(40)});
 ot2=await api(r2+'/termino-tecnico',tecnico,{observacion:'Reenvío después de la devolución'});
 check(ot2.estado==='PendienteBodega','El técnico reenvía la OT a Bodega');
 rechazo=await api(r2+'/aprobar-bodega',bodega,{confirmoRevision:true},'POST',409);
 check(/rectificaciones de pesajes sin resolver/.test(mensaje(rechazo)),'Bodega no aprueba con una rectificación pendiente');
 await api(r2+`/rectificaciones/${rect}/resolver`,autorizador,{aprobar:true,respuesta:'Foto legible: se aprueba el repesaje'});
 ot2=await api(r2+'/aprobar-bodega',bodega,{confirmoRevision:true,observacion:'Conforme'});
 check(ot2.estado==='PendienteRevision','Sin pendientes, Bodega aprueba');
 await api(r2+'/devolver',bodega,{motivo:'Devolución tardía de prueba'},'POST',409);
 await api(r2+'/revision-autorizador',autorizador,{confirmoRevision:true});
 ot2=await api(r2+'/cerrar',autorizador,undefined,'POST');
 check(ot2.estado==='Cerrada','La OT devuelta y corregida se cierra');

 // D-43: el retirado vuelve vacío y se reingresa con recarga.
 let c=(await api('cilindros',bodega)).find(x=>x.codigo===viejo.codigo);
 check(c.ubicacion==='Bodega'&&c.estado==='Vacio','El cilindro retirado de la sucursal vuelve a Bodega como Vacio');
 const ot3=await api('ordenes-trabajo',autorizador,{...pedido('Sin cilindros disponibles'),cilindrosARetirar:[]});
 await api(`ordenes-trabajo/${ot3.id}/entregas`,tecnico,{idOperacion:randomUUID(),codigo:viejo.codigo,pesoBrutoGr:2380,foto:foto(45)},'POST',409);
 check(true,'Un cilindro vacío no se puede retirar de Bodega para una OT');
 const recarga={idOperacion:randomUUID(),pesoBrutoGr:10500,foto:foto(50),observacion:'Recarga del proveedor'};
 await api(`cilindros/${viejo.codigo}/recarga`,tecnico,recarga,'POST',403);
 await api(`cilindros/${viejo.codigo}/recarga`,bodega,{...recarga,pesoBrutoGr:1000},'POST',409);
 c=await api(`cilindros/${viejo.codigo}/recarga`,bodega,recarga);
 check(c.estado==='Disponible'&&c.ultimoPesaje?.pesoBrutoGr===10500,'Bodega recarga el cilindro vacío con peso y foto, y vuelve a Disponible');
 c=await api(`cilindros/${viejo.codigo}/recarga`,bodega,recarga);
 check(c.estado==='Disponible','Reintentar la misma recarga no duplica el pesaje');
 await api(`cilindros/${lleno.codigo}/recarga`,bodega,{...recarga,idOperacion:randomUUID(),foto:foto(55)},'POST',409);
 check(true,'No se recarga un cilindro instalado en una sucursal');

 await db.exec('RESET ROLE');
 const n=(await db.query("SELECT count(*)::int AS n FROM afrisan.\"Pesaje\" WHERE \"Tipo\"='Recarga'")).rows[0].n;
 check(n===1,'La base guarda un solo pesaje de recarga');
 const sinRls=(await db.query("SELECT count(*)::int AS n FROM pg_tables WHERE schemaname='afrisan' AND NOT rowsecurity")).rows[0].n;
 check(sinRls===0,'RLS sigue habilitado en todas las tablas después de la migración 11');
 report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{
 await stop();await socket.stop();await db.close();
 report.cantidad_comprobaciones=checks.length;
 await fs.writeFile(path.join(directory,'resultados_correcciones.json'),JSON.stringify(report,null,2));
 await fs.rm(profile,{recursive:true,force:true});await fs.rm(environment.Persistencia__FotosPath,{recursive:true,force:true});
}
