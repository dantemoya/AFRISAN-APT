// Repetición del recorrido completo (CDP-67) contra la base de Supabase, después de aplicar la migración 11.
// Usa la API local ya iniciada en modo PostgreSQL (por defecto http://localhost:5181) y las cuentas de prueba
// del 14/11/2026, cuyas claves están fuera del repositorio (.local/cuentas_prueba_2026_11_14.json).
// Crea datos de prueba nuevos en cada ejecución (serie P1511-*); se limpian junto con los del 14/11.
// Uso (desde la raíz del repositorio):
//   FOTOS_DIR=<carpeta con las fotos simuladas> node AFRISAN_NUEVO/04_SEGUIMIENTO/CORRECCIONES_2026_11_14/recorrido_supabase.mjs
// FOTOS_DIR debe contener entrega, retirado, instalado, rectificacion, recarga, recarga_instalado y entrega_vacio (.png).
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {randomUUID} from 'node:crypto';

const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const base=(process.env.AFRISAN_API??'http://localhost:5181')+'/api/v1/';
const fotos=process.env.FOTOS_DIR;if(!fotos)throw Error('Falta FOTOS_DIR');
const cuentas=JSON.parse(await fs.readFile(path.join(root,'.local/cuentas_prueba_2026_11_14.json'),'utf8'));
const checks=[];const datos={};
const report={fecha_utc:new Date().toISOString(),tipo:'API .NET 8 local + base de Supabase con la migración 11 (CDP-67, repetición)',comprobaciones:checks,datos_creados:datos};
function check(ok,name){checks.push({resultado:ok?'OK':'FALLO',comprobacion:name});if(!ok)throw Error(name);console.log('OK:',name);}
async function api(route,token,body,method=body===undefined?'GET':'POST',expected){
 const r=await fetch(base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});
 const text=await r.text();let result;try{result=JSON.parse(text);}catch{result=text;}
 if(expected!==undefined){const ok=[].concat(expected).includes(r.status);check(ok,`${method} ${route.replace(/[0-9a-f-]{36}/g,'{id}')}: HTTP ${r.status}`+(ok?'':` (esperado ${expected}: ${text.slice(0,200)})`));return result;}
 if(!r.ok)throw Error(`${method} ${route}: ${r.status} ${text.slice(0,400)}`);return result;
}
const foto=async nombre=>({id:randomUUID(),nombre:`${nombre}.png`,contentType:'image/png',base64:(await fs.readFile(path.join(fotos,`${nombre}.png`))).toString('base64')});
const login=async rol=>{const c=cuentas.find(x=>x.rol===rol);return (await api('sesiones',null,{email:c.correo,password:c.password})).token;};
const mensaje=r=>typeof r==='string'?r:(r?.detail??r?.title??JSON.stringify(r));
const tecnicoEmail=cuentas.find(x=>x.rol==='Tecnico').correo;

try{
 const bodega=await login('Bodega'),autorizador=await login('Autorizador'),tecnico=await login('Tecnico');
 check(true,'Ingresan las cuentas de prueba de Bodega, Autorizador y Técnico');
 const sucursal=(await api('sucursales',bodega)).find(s=>s.nombre==='Local Maipú (prueba)');
 const gas=(await api('refrigerantes',bodega)).find(g=>g.codigo==='R404A-PRUEBA');
 let viejo=(await api('cilindros',bodega)).find(c=>c.numeroSerie==='P1411-A');
 check(sucursal&&gas&&viejo?.estado==='Instalado','Existen la sucursal, el gas y el cilindro instalado del recorrido del 14/11');
 const ultimo=(viejo.ultimoPesaje??viejo.pesajeRegistrado).pesoBrutoGr;

 const pedido=(desc,retirar)=>({idOperacion:randomUUID(),sucursalId:sucursal.id,sucursal:sucursal.nombre,direccion:sucursal.direccion,descripcion:desc,tecnicoEmail,
  detalle:[{refrigeranteId:gas.id,grSolicitados:10000,grAutorizados:9800}],cilindrosARetirar:retirar});
 // Si una ejecución anterior se detuvo con la OT de reemplazo ya retirada de Bodega (no se puede anular), se continúa con ella.
 const previo=await fs.readFile(path.join(directory,'resultados_recorrido_supabase.json'),'utf8').then(JSON.parse).catch(()=>null);
 let ot=(await api('ordenes-trabajo',autorizador)).find(o=>o.descripcion==='Recorrido 15/11: reemplazo con correcciones'&&o.estado==='EnProcesoDespacho');
 let lleno,opId,op;
 if(ot){
  report.intento_anterior={fecha_utc:previo?.fecha_utc,estado:previo?.estado,error:previo?.error,comprobaciones:previo?.comprobaciones};
  Object.assign(datos,previo?.datos_creados??{});
  lleno=(await api('cilindros',bodega)).find(c=>c.numeroSerie==='P1511-A');
  op=ot.operaciones.find(o=>o.estado==='Borrador');opId=op.id;
  check(lleno&&op,'Se retoma la OT de reemplazo de la ejecución anterior, con el retiro de Bodega y el borrador guardados');
 }else{
  // 1. Bodega registra un cilindro nuevo con su pesaje inicial.
  const alta={ubicacionInicial:'Bodega',refrigeranteId:gas.id,capacidadGr:10000,taraGr:5000,observacion:'Recorrido del 15/11/2026'};
  await api('cilindros',bodega,{...alta,numeroSerie:'P1511-X',pesoInicialGr:15100},'POST',[400,409,422]);
  check(true,'Un peso inicial mayor que la tara más la capacidad se rechaza');
  lleno=await api('cilindros',bodega,{...alta,numeroSerie:'P1511-A',pesoInicialGr:14990});
  datos.cilindro_nuevo=lleno.codigo;
  check((await api('cilindros',bodega)).some(c=>c.numeroSerie==='P1511-A'&&c.estado==='Disponible'),'El cilindro nuevo aparece Disponible al buscarlo por serie');
  // 2. D-40: el Autorizador anula una OT que el cliente canceló.
  let anulada=await api('ordenes-trabajo',autorizador,pedido('Recorrido 15/11: visita cancelada',[viejo.codigo]));
  datos.ot_anulada=anulada.codigo;
  await api(`ordenes-trabajo/${anulada.id}/anular`,bodega,{motivo:'Bodega no puede anular'},'POST',403);
  await api(`ordenes-trabajo/${anulada.id}/anular`,autorizador,{motivo:'no'},'POST',400);
  anulada=await api(`ordenes-trabajo/${anulada.id}/anular`,autorizador,{motivo:'El cliente canceló la visita del 15/11'});
  check(anulada.estado==='Anulada'&&anulada.historial.some(h=>h.accion==='OT anulada'),'El Autorizador anula la OT con motivo y queda en el historial');
  // 3. OT de reemplazo con el mismo cilindro previsto (la anulación lo liberó).
  ot=await api('ordenes-trabajo',autorizador,pedido('Recorrido 15/11: reemplazo con correcciones',[viejo.codigo]));
  datos.ot_reemplazo=ot.codigo;
  check(ot.cilindrosARetirar.includes(viejo.codigo),'Anular liberó el cilindro previsto: se reserva en la OT nueva');
  ot=await api(`ordenes-trabajo/${ot.id}/entregas`,tecnico,{idOperacion:randomUUID(),codigo:lleno.codigo,pesoBrutoGr:14990,foto:await foto('entrega')});
  check(ot.estado==='EnProcesoDespacho','El técnico retira el cilindro de Bodega con peso y foto');
  await api(`ordenes-trabajo/${ot.id}/anular`,autorizador,{motivo:'Ya hay un retiro de Bodega'},'POST',409);
 }
 const r=`ordenes-trabajo/${ot.id}`;

 // 4. D-42: tolerancia de peso entre etapas (repite el error de la OT 2 del 14/11).
 const inst=(await api('entorno')).instancia;
 const borrador=(version,retirado,instalado)=>api(r+'/operaciones',tecnico,{id:opId,instancia:inst,version,tipo:'Reemplazo',codigoRetirado:viejo.codigo,codigoInstalado:lleno.codigo,pesoRetiradoGr:retirado,pesoInstaladoGr:instalado,observacion:'Recorrido 15/11'},'PUT');
 if(!op){
  opId=randomUUID();op=await borrador(0,12950,13000);
  op=await api(r+`/operaciones/${opId}/fotos`,tecnico,{...await foto('retirado'),grupo:'Retirado',version:op.version});
  op=await api(r+`/operaciones/${opId}/fotos`,tecnico,{...await foto('instalado'),grupo:'Instalado',version:op.version});
 }else op=await borrador(op.version,12950,13000);
 let rechazo=await api(r+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST',409);
 check(/difiere en más de 200 g/.test(mensaje(rechazo)),'Un instalado de 13.000 g con salida de 14.990 g se rechaza (DEF-03)');
 op=await borrador(op.version,ultimo+210,14960);
 rechazo=await api(r+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST',409);
 check(/más que su último peso registrado/.test(mensaje(rechazo)),'Un retirado 210 g sobre su último peso se rechaza');
 op=await borrador(op.version,12950,14960);
 op=await api(r+`/operaciones/${opId}/confirmar?version=${op.version}`,tecnico,undefined,'POST');
 check(op.estado==='Registrada','Con los pesos de las fotos el reemplazo se confirma');
 await api(r+`/operaciones/${opId}/informar-entrega`,tecnico,undefined,'POST');
 ot=await api(r+'/termino-tecnico',tecnico,{observacion:'Reemplazo terminado'});
 check(ot.estado==='PendienteBodega'&&ot.historial.some(h=>h.accion==='Enviada a revisión de Bodega'),'El técnico envía la OT a Bodega y queda en el historial');

 // 5. Recepción en Bodega con su propio peso.
 ot=await api(r+'/recepciones',bodega,{codigo:viejo.codigo,pesoBrutoGr:12900,observacion:'Recepción del recorrido 15/11'});
 check(true,'Bodega recibe el cilindro retirado con 12.900 g (50 g menos que en terreno)');

 // 6. D-41 y D-39: incidencia abierta, devolución y respuesta del Autorizador.
 const incidencia=randomUUID();
 await api(r+'/incidencias',bodega,{id:incidencia,operacionId:opId,tipo:'Incidencia',motivo:'El peso del retirado no coincide con la recepción'});
 rechazo=await api(r+'/aprobar-bodega',bodega,{confirmoRevision:true,observacion:'Intento con incidencia abierta'},'POST',409);
 check(/Incidencias sin revisar/.test(mensaje(rechazo)),'Bodega no aprueba con su incidencia abierta (DEF-02)');
 await api(r+'/devolver',tecnico,{motivo:'El técnico no se devuelve'},'POST',403);
 await api(r+'/devolver',bodega,{motivo:'no'},'POST',400);
 ot=await api(r+'/devolver',bodega,{motivo:'Repesar el retirado y pedir la corrección con foto'});
 check(ot.estado==='EnTerreno'&&ot.motivoDevolucion&&ot.historial.some(h=>h.accion==='Devuelta al técnico por Bodega'),'Bodega devuelve la OT al técnico con el motivo (DEF-01)');
 ot=await api(r+`/incidencias/${incidencia}/resolver`,autorizador,{respuesta:'Se corrige con una rectificación del retirado'});
 check(ot.incidencias.every(i=>i.respuesta),'El Autorizador responde la incidencia con la OT devuelta');
 const retirado=(await api(r+'/rectificaciones/pesajes',tecnico)).find(p=>p.tipo==='Retirado');
 const rect=randomUUID();
 await api(r+'/rectificaciones',tecnico,{id:rect,pesajeId:retirado.id,pesoPropuestoGr:12900,motivo:'Repesaje en Bodega con foto',foto:await foto('rectificacion')});
 ot=await api(r+'/termino-tecnico',tecnico,{observacion:'Reenvío con la rectificación solicitada'});
 check(ot.estado==='PendienteBodega','El técnico reenvía la OT a Bodega');
 rechazo=await api(r+'/aprobar-bodega',bodega,{confirmoRevision:true},'POST',409);
 check(/rectificaciones de pesajes sin resolver/.test(mensaje(rechazo)),'Bodega no aprueba con una rectificación pendiente');
 await api(r+`/rectificaciones/${rect}/resolver`,autorizador,{aprobar:true,respuesta:'Se aprueba el repesaje con foto'});
 ot=await api(r+'/aprobar-bodega',bodega,{confirmoRevision:true,observacion:'Conforme después de la corrección'});
 check(ot.estado==='PendienteRevision','Sin pendientes, Bodega aprueba');
 await api(r+'/devolver',bodega,{motivo:'Devolución después de aprobar'},'POST',409);
 await api(r+'/revision-autorizador',autorizador,{confirmoRevision:true});
 ot=await api(r+'/cerrar',autorizador,undefined,'POST');
 check(ot.estado==='Cerrada','El Autorizador revisa y cierra la OT corregida');

 // 7. D-43: el retirado queda Vacío y se recarga.
 viejo=await api(`cilindros/${viejo.codigo}`,bodega);
 check(viejo.ubicacion==='Bodega'&&viejo.estado==='Vacio','El cilindro retirado vuelve a Bodega como Vacío (DEF-04)');
 let otra=await api('ordenes-trabajo',autorizador,pedido('Recorrido 15/11: agregado sin cilindros llenos',[]));
 datos.ot_agregado=otra.codigo;
 await api(`ordenes-trabajo/${otra.id}/entregas`,tecnico,{idOperacion:randomUUID(),codigo:viejo.codigo,pesoBrutoGr:12900,foto:await foto('entrega_vacio')},'POST',409);
 check(true,'Un cilindro Vacío no se puede retirar de Bodega para otra OT');
 const recarga={idOperacion:randomUUID(),pesoBrutoGr:14800,foto:await foto('recarga'),observacion:'Recarga del proveedor (recorrido 15/11)'};
 await api(`cilindros/${viejo.codigo}/recarga`,tecnico,recarga,'POST',403);
 await api(`cilindros/${viejo.codigo}/recarga`,bodega,{...recarga,pesoBrutoGr:4000},'POST',409);
 let c=await api(`cilindros/${viejo.codigo}/recarga`,bodega,recarga);
 check(c.estado==='Disponible'&&(c.ultimoPesaje??c.pesajeRegistrado)?.pesoBrutoGr===14800,'Bodega registra la recarga con peso y foto: el cilindro vuelve a Disponible');
 const antes=(await api(`cilindros/${viejo.codigo}/historial`,bodega)).length,pesajeRecarga=(c.ultimoPesaje??c.pesajeRegistrado)?.id;
 c=await api(`cilindros/${viejo.codigo}/recarga`,bodega,recarga);
 check(pesajeRecarga===undefined||(c.ultimoPesaje??c.pesajeRegistrado)?.id===pesajeRecarga,'El reintento devuelve el mismo pesaje de recarga');
 const despues=(await api(`cilindros/${viejo.codigo}/historial`,bodega)).length;
 check(c.estado==='Disponible'&&despues===antes,'Repetir la misma recarga no duplica el registro');
 await api(`cilindros/${lleno.codigo}/recarga`,bodega,{...recarga,idOperacion:randomUUID(),foto:await foto('recarga_instalado')},'POST',409);
 check(true,'No se recarga un cilindro instalado en la sucursal');
 otra=await api(`ordenes-trabajo/${otra.id}/anular`,autorizador,{motivo:'Prueba terminada: no se requiere el agregado'});
 check(otra.estado==='Anulada','La OT de agregado sin retiros se anula al terminar');
 report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{
 report.cantidad_comprobaciones=checks.length;
 await fs.writeFile(path.join(directory,'resultados_recorrido_supabase.json'),JSON.stringify(report,null,2));
}
