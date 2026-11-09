import {randomUUID} from 'node:crypto';
let route,solicitud,pesaje;
export async function validarRectificaciones(etapa,c){
 const {api,check,admin,bodega,autorizador,tecnico,otro,photo,db,base}=c;
 if(etapa==='confirmado'){
  const ot=(await api('ordenes-trabajo',tecnico))[0];route='ordenes-trabajo/'+ot.id;
  const pesajes=await api(route+'/rectificaciones/pesajes',tecnico);
  pesaje=pesajes.find(p=>p.tipo==='Instalado');
  check(pesaje.pesoOriginalGr===5600&&pesaje.pesoVigenteGr===5600,'Pesaje confirmado conserva el original antes de rectificar');
  await api(route+'/rectificaciones',otro,undefined,'GET',403);
  await api(route+'/rectificaciones',null,undefined,'GET',401);
  const datos={id:randomUUID(),pesajeId:pesaje.id,pesoPropuestoGr:5500,motivo:'Corrección documentada del peso de balanza',foto:photo(101)};
  await api(route+'/rectificaciones',autorizador,datos,'POST',403);
  await api(route+'/rectificaciones',tecnico,{...datos,pesajeId:randomUUID()},'POST',404);
  await api(route+'/rectificaciones',tecnico,{...datos,foto:null},'POST',400);
  await api(route+'/rectificaciones',tecnico,{...datos,pesoPropuestoGr:999},'POST',409);
  await api(route+'/rectificaciones',tecnico,{...datos,pesoPropuestoGr:5600},'POST',409);
  const inicial=await api(route+'/rectificaciones',tecnico,datos);
  check(inicial.estado==='Pendiente'&&inicial.pesoAnteriorGr===5600,'La solicitud queda pendiente sin cambiar el pesaje');
  check((await api(route+'/rectificaciones',tecnico,datos)).id===inicial.id,'Reintento de solicitud no duplica el historial');
  await api(route+'/rectificaciones',tecnico,{...datos,pesoPropuestoGr:5400},'POST',409);
  await api(route+'/rectificaciones',bodega,{...datos,id:randomUUID(),foto:photo(102)},'POST',409);
  await api(route+'/rectificaciones/'+inicial.id+'/resolver',bodega,{aprobar:true,respuesta:'Intento no autorizado'},'POST',403);
  const rechazo=await api(route+'/rectificaciones/'+inicial.id+'/resolver',autorizador,{aprobar:false,respuesta:'Evidencia insuficiente para este cambio'});
  check(rechazo.estado==='Rechazada','Autorizador puede rechazar conservando el peso vigente');
  check((await api(route+'/rectificaciones/pesajes',tecnico)).find(p=>p.id===pesaje.id).pesoVigenteGr===5600,'Rechazo conserva el peso original');
  solicitud=await api(route+'/rectificaciones',bodega,{...datos,id:randomUUID(),foto:photo(103)});
  const foto=await fetch(base+route+'/rectificaciones/'+solicitud.id+'/foto',{headers:{Authorization:'Bearer '+autorizador}});
  check(foto.ok&&Buffer.from(await foto.arrayBuffer()).equals(Buffer.from(photo(103).base64,'base64'))&&foto.headers.get('cache-control')==='no-store','Evidencia privada de rectificación íntegra y sin caché');
  await api(route+'/rectificaciones/'+solicitud.id+'/foto',otro,undefined,'GET',403);
 }
 if(etapa==='bodega'){
  await api(route+'/revision-autorizador',autorizador,{confirmoRevision:true},'POST',409);
  const respuesta={aprobar:true,respuesta:'Peso correcto verificado con la evidencia de balanza'};
  const aprobado=await api(route+'/rectificaciones/'+solicitud.id+'/resolver',autorizador,respuesta);
  check(aprobado.estado==='Aprobada'&&aprobado.revisor,'Aprobación registra responsable, fecha y conclusión');
  check((await api(route+'/rectificaciones/'+solicitud.id+'/resolver',autorizador,respuesta)).estado==='Aprobada','Resolución idempotente después de respuesta incierta');
  await api(route+'/rectificaciones/'+solicitud.id+'/resolver',autorizador,{aprobar:false,respuesta:'Cambiar una resolución firme'},'POST',409);
  const p=(await api(route+'/rectificaciones/pesajes',tecnico)).find(p=>p.id===pesaje.id);
  check(p.pesoOriginalGr===5600&&p.pesoVigenteGr===5500,'Peso aprobado actualizado con el original inmutable');
  check((await api('power-bi/resumen',admin)).kgInstalados===4.5,'Power BI usa el peso rectificado sin duplicar operaciones');
  const original=(await db.query('SELECT "PesoBrutoGr" peso FROM afrisan."Pesaje" WHERE "Id_Pesaje"=$1',[pesaje.id])).rows[0];
  check(original.peso===5600,'La aprobación no modifica la fila del pesaje original');
  const recepcion=(await api(route+'/rectificaciones/pesajes',bodega)).find(p=>p.tipo==='Recepcion'&&p.pesoOriginalGr===1750);
  const vacio=await api(route+'/rectificaciones',bodega,{id:randomUUID(),pesajeId:recepcion.id,pesoPropuestoGr:1000,motivo:'Balanza revisada: cilindro vacío al recibir',foto:photo(105)});
  await api(route+'/rectificaciones/'+vacio.id+'/resolver',autorizador,{aprobar:true,respuesta:'Lectura de tara confirmada en la evidencia'});
  const cilindro=(await api('cilindros',admin)).find(p=>p.codigo===recepcion.cilindro);
  check(cilindro.estado==='Vacio'&&cilindro.ultimoPesaje.pesoBrutoGr===1000,'Rectificar el último peso de recepción actualiza la proyección de inventario a vacío');
  const nuevo=await api(route+'/rectificaciones',bodega,{id:randomUUID(),pesajeId:recepcion.id,pesoPropuestoGr:1600,motivo:'Segunda rectificación con nueva evidencia',foto:photo(106)});
  check(nuevo.pesoAnteriorGr===1000,'Una segunda solicitud parte del peso vigente y conserva la cadena de correcciones');
  await api(route+'/rectificaciones/'+nuevo.id+'/resolver',autorizador,{aprobar:true,respuesta:'Nueva lectura confirmada con la balanza'});
  check((await api('cilindros',admin)).find(p=>p.codigo===recepcion.cilindro).estado==='Disponible','Segunda rectificación actualiza la disponibilidad sin cambiar ubicación');
 }
 if(etapa==='cerrado'){
  await api(route+'/rectificaciones',tecnico,{id:randomUUID(),pesajeId:pesaje.id,pesoPropuestoGr:5400,motivo:'Intento después del cierre de la OT',foto:photo(104)},'POST',409);
  check((await api(route+'/rectificaciones',admin)).length===4,'Historial de solicitudes rechazadas y aprobadas disponible tras el cierre');
 }
}
