import {randomUUID} from 'node:crypto';
import fs from 'node:fs/promises';
import path from 'node:path';
import {csv,unzip} from '../POWER_BI_2026_11_02/validar_powerbi.mjs';

export async function validarControl(c){
 const {api,check,admin,bodega,autorizador,tecnico,jefatura,photo,db,root,start,stop,gas,sucursal,op,entorno,base}=c;
 const hoy=(await db.query("SELECT (now() AT TIME ZONE 'America/Santiago')::date::text dia")).rows[0].dia;
 const lunes=(await db.query("SELECT date_trunc('week',now() AT TIME ZONE 'America/Santiago')::date::text dia")).rows[0].dia;
 const mes=hoy.slice(0,8)+'01';
 const manana=(await db.query("SELECT ((now() AT TIME ZONE 'America/Santiago')::date+1)::text dia")).rows[0].dia;
 for(const token of [null,tecnico,bodega,autorizador])await api('costos',token,undefined,'GET',token?403:401);
 await api('costos/precios',jefatura,{solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:2,moneda:'CLP',vigenteDesde:hoy},'POST',403);
 const precios={},cambios={};
 for(const [moneda,precio,factor] of [['CLP',2,1],['EUR',0.003,1000],['USD',0.002,900],['UF',0.0001,40000]]){
  if(moneda!=='CLP'){
   const datos={solicitudId:randomUUID(),moneda,clpPorUnidad:factor,fechaValor:hoy};
   cambios[moneda]=await api('costos/cambios',admin,datos);
   check((await api('costos/cambios',admin,datos)).id===cambios[moneda].id,'Reintento del cambio '+moneda+' no duplica');
   await api('costos/cambios',admin,{...datos,clpPorUnidad:factor+1},'POST',409);
  }
  const datos={solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:precio,moneda,vigenteDesde:hoy};
  precios[moneda]=await api('costos/precios',admin,datos);
  check((await api('costos/precios',admin,datos)).id===precios[moneda].id,'Precio '+moneda+' conserva identidad al reintentar');
  await api('costos/precios',admin,{...datos,precioPorGramo:precio+1},'POST',409);
  const valor={solicitudId:randomUUID(),operacionId:op.id,precioId:precios[moneda].id,gramos:100,cambioId:cambios[moneda]?.id??null};
  const result=await api('costos',admin,valor);
  check(Math.abs(result.total-100*precio)<1e-8&&Math.abs(result.totalClp-100*precio*factor)<1e-8,'Valoración exacta de gramos explícitos en '+moneda+' y CLP');
  check((await api('costos',admin,valor)).id===result.id,'Respuesta perdida no duplica la valoración '+moneda);
  await api('costos',admin,{...valor,gramos:101},'POST',409);
 }
 await api('costos/precios',admin,{solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:1,moneda:'GBP',vigenteDesde:hoy},'POST',400);
 await api('costos/precios',admin,{solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:-1,moneda:'CLP',vigenteDesde:hoy},'POST',400);
 await api('costos/precios',admin,{solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:0.000000001,moneda:'CLP',vigenteDesde:hoy},'POST',400);
 await api('costos/cambios',admin,{solicitudId:randomUUID(),moneda:'USD',clpPorUnidad:0,fechaValor:hoy},'POST',400);
 await api('costos/cambios',admin,{solicitudId:randomUUID(),moneda:'USD',clpPorUnidad:900,fechaValor:manana},'POST',400);
 await api('costos',admin,{solicitudId:randomUUID(),operacionId:op.id,precioId:precios.USD.id,gramos:100},'POST',400);
 await api('costos',admin,{solicitudId:randomUUID(),operacionId:op.id,precioId:precios.USD.id,gramos:100,cambioId:cambios.EUR.id},'POST',400);
 await api('costos',admin,{solicitudId:randomUUID(),operacionId:op.id,precioId:precios.CLP.id,gramos:0},'POST',400);
 const futuro=await api('costos/precios',admin,{solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:3,moneda:'CLP',vigenteDesde:manana});
 await api('costos',admin,{solicitudId:randomUUID(),operacionId:op.id,precioId:futuro.id,gramos:100},'POST',400);
 const costoUsd=(await api('costos',jefatura)).find(x=>x.moneda==='USD');
 await api('costos/cambios',admin,{solicitudId:randomUUID(),moneda:'USD',clpPorUnidad:1000,fechaValor:hoy});
 check((await api('costos',jefatura)).find(x=>x.id===costoUsd.id).totalClp===180,'Un nuevo cambio manual no revaloriza costos históricos');
 const precioCorregido=await api('costos/precios',admin,{solicitudId:randomUUID(),refrigeranteId:gas.id,precioPorGramo:2.5,moneda:'CLP',vigenteDesde:hoy});
 check(precioCorregido.id!==precios.CLP.id&&(await api('costos/precios',jefatura)).some(p=>p.id===precios.CLP.id&&p.precioPorGramo===2),'Corregir el precio el mismo día conserva la tarifa anterior');
 check((await api('costos/operaciones',admin)).some(x=>x.id===op.id&&x.grInstalados===4600),'La referencia instalada usa peso neto confirmado y no se cobra automáticamente');
 const gas2=await api('refrigerantes',bodega,{codigo:'CONTROL-2',nombre:'Segundo gas de pruebas'});
 const precioAjeno=await api('costos/precios',admin,{solicitudId:randomUUID(),refrigeranteId:gas2.id,precioPorGramo:1,moneda:'CLP',vigenteDesde:hoy});
 await api('costos',admin,{solicitudId:randomUUID(),operacionId:op.id,precioId:precioAjeno.id,gramos:100},'POST',409);
 check((await api('costos',jefatura)).length===4,'Errores de precio o cambio revierten la valoración completa');
 for(const token of [null,tecnico,admin])await api('limites',token,undefined,'GET',token?403:401);
 const definir=(gasId,periodicidad,grLimite)=>({sucursalId:sucursal.id,refrigeranteId:gasId,periodicidad,grLimite,vigenteDesde:periodicidad==='Mes'?mes:lunes,version:0});
 let mensual=await api('limites/definiciones',bodega,definir(gas.id,'Mes',4000));
 const semanal=await api('limites/definiciones',bodega,definir(gas.id,'Semana',5000));
 const otroGas=await api('limites/definiciones',bodega,definir(gas2.id,'Mes',1000));
 const cliente2=await api('clientes',admin,{rut:'CONTROL-OTRO',nombre:'Otro cliente para límites'});
 const suc2=await api('sucursales',admin,{clienteId:cliente2.id,nombre:'Otra sucursal',direccion:'Dirección sintética'});
 const otraSucursal=await api('limites/definiciones',bodega,{...definir(gas.id,'Mes',1000),sucursalId:suc2.id});
 check((await api('limites/definiciones',bodega,definir(gas.id,'Mes',4000))).id===mensual.id,'Reintento de límite no agrega una versión idéntica');
 await api('limites/definiciones',autorizador,definir(gas.id,'Mes',3000),'POST',403);
 await api('limites/definiciones',bodega,{...definir(gas.id,'Mes',3000),vigenteDesde:mes.slice(0,8)+'02',version:mensual.version},'POST',409);
 await api('limites/definiciones',bodega,{...definir(gas.id,'Mes',3000)},'POST',409);
 const avances=await api('limites?fecha='+hoy,autorizador);
 check(avances.find(x=>x.limiteId===mensual.id).grExceso===600&&avances.find(x=>x.limiteId===semanal.id).grExceso===0,'Límites semanal y mensual usan los mismos gramos netos con umbrales separados');
 check(avances.find(x=>x.limiteId===otroGas.id).grInstalados===0,'No se mezclan gramos de gases distintos');
 check(avances.find(x=>x.limiteId===otraSucursal.id).grInstalados===0,'No se mezclan gramos entre sucursales');
 const medicionDto={solicitudId:randomUUID(),limiteId:mensual.id,fecha:hoy,observaciones:'Corte de revisión sintético'};
 const medicion=await api('limites/mediciones',autorizador,medicionDto);
 check(medicion.grInstalados===4600&&medicion.grLimite===4000&&medicion.grExceso===600,'Medición conserva resultado y versión del límite aplicado');
 check((await api('limites/mediciones',autorizador,medicionDto)).id===medicion.id,'Reintento de medición no duplica el corte');
 await api('limites/mediciones',autorizador,{...medicionDto,limiteId:semanal.id},'POST',409);
 await api('limites/mediciones/'+medicion.id+'/revision',bodega,{conclusion:'No autorizado'},'POST',403);
 await api('limites/mediciones/'+medicion.id+'/revision',jefatura,{conclusion:'ok'},'POST',400);
 const revisada=await api('limites/mediciones/'+medicion.id+'/revision',jefatura,{conclusion:'Revisado el aumento de instalaciones de este gas'});
 check(revisada.conclusion?.startsWith('Revisado'),'Jefatura conserva su conclusión sobre el exceso');
 await api('limites/mediciones/'+medicion.id+'/revision',jefatura,{conclusion:'Otra conclusión distinta'},'POST',409);
 const sinExceso=await api('limites/mediciones',bodega,{solicitudId:randomUUID(),limiteId:semanal.id,fecha:hoy});
 await api('limites/mediciones/'+sinExceso.id+'/revision',jefatura,{conclusion:'No hay exceso para revisar'},'POST',409);
 const siguienteLunes=(await db.query("SELECT (date_trunc('week',now() AT TIME ZONE 'America/Santiago')::date+7)::text dia")).rows[0].dia;
 await api('limites/definiciones',bodega,{...definir(gas.id,'Semana',6000),vigenteDesde:siguienteLunes,version:semanal.version});
 check((await api('limites?fecha='+hoy,bodega)).find(x=>x.limiteId===semanal.id).grLimite===5000,'Una definición futura conserva el límite vigente del período actual');
 mensual=await api('limites/definiciones',jefatura,{...definir(gas.id,'Mes',4500),version:mensual.version});
 check(mensual.bloqueado&&mensual.version===2,'Modificar por Jefatura agrega versión y bloquea Bodega');
 await api('limites/definiciones',bodega,{...definir(gas.id,'Mes',4700),version:mensual.version},'POST',409);
 await api('limites/'+mensual.id+'/bloqueo',bodega,{version:mensual.version,bloqueado:false},'PUT',403);
 mensual=await api('limites/'+mensual.id+'/bloqueo',jefatura,{version:mensual.version,bloqueado:false},'PUT');
 mensual=await api('limites/definiciones',bodega,{...definir(gas.id,'Mes',4700),version:mensual.version});
 check(mensual.version===4&&!mensual.bloqueado,'Habilitar por Jefatura permite un nuevo cambio de Bodega');
 check((await api('limites/mediciones',jefatura)).find(x=>x.id===medicion.id).grLimite===4000,'Cambiar el límite no reescribe las mediciones y revisiones anteriores');
 // Nueva operación real de segundo gas: la rectificación modifica el avance, no el corte guardado.
 const cil=await api('cilindros',bodega,{ubicacionInicial:'Bodega',refrigeranteId:gas2.id,capacidadGr:10000,taraGr:1000,pesoInicialGr:4000});
 const orden=await api('ordenes-trabajo',autorizador,{idOperacion:randomUUID(),sucursalId:sucursal.id,sucursal:sucursal.nombre,direccion:sucursal.direccion,descripcion:'Límites con rectificación',tecnicoEmail:'tecnico@example.invalid',detalle:[{refrigeranteId:gas2.id,grSolicitados:4000,grAutorizados:4000}],cilindrosARetirar:[]});
 const ruta='ordenes-trabajo/'+orden.id;
 await api(ruta+'/entregas',tecnico,{idOperacion:randomUUID(),codigo:cil.codigo,pesoBrutoGr:3900,foto:photo(140)});
 let nuevaOp=await api(ruta+'/operaciones',tecnico,{id:randomUUID(),instancia:entorno.instancia,version:0,tipo:'Agregado',codigoInstalado:cil.codigo,pesoInstaladoGr:3800},'PUT');
 nuevaOp=await api(ruta+'/operaciones/'+nuevaOp.id+'/fotos',tecnico,{...photo(141),grupo:'Instalado',version:nuevaOp.version});
 nuevaOp=await api(ruta+'/operaciones/'+nuevaOp.id+'/confirmar?version='+nuevaOp.version,tecnico,undefined,'POST');
 const antes=await api('limites/mediciones',bodega,{solicitudId:randomUUID(),limiteId:otroGas.id,fecha:hoy});
 check(antes.grInstalados===2800,'Segunda operación se cuenta exclusivamente en su gas');
 const pesaje=(await api(ruta+'/rectificaciones/pesajes',tecnico)).find(x=>x.tipo==='Instalado');
 const rect=await api(ruta+'/rectificaciones',tecnico,{id:randomUUID(),pesajeId:pesaje.id,pesoPropuestoGr:3700,motivo:'Corrección de lectura para prueba de límites',foto:photo(142)});
 check((await api('limites?fecha='+hoy,bodega)).find(x=>x.limiteId===otroGas.id).grInstalados===2800,'Una rectificación pendiente no cambia el avance');
 await api(ruta+'/rectificaciones/'+rect.id+'/resolver',autorizador,{aprobar:true,respuesta:'Nueva lectura de balanza comprobada'});
 check((await api('limites?fecha='+hoy,bodega)).find(x=>x.limiteId===otroGas.id).grInstalados===2700,'El avance usa el peso rectificado aprobado');
 check((await api('limites/mediciones',jefatura)).find(x=>x.id===antes.id).grInstalados===2800,'La rectificación posterior conserva el corte histórico');
 const detalles=(await db.query('SELECT "GrInstalados" gr FROM afrisan."MedicionLimiteOperaciones" WHERE "Id_Medicion"=$1',[antes.id])).rows;
 check(detalles.length===1&&Number(detalles[0].gr)===2800,'Cada corte conserva las operaciones y gramos que lo compusieron');
 const calendario=(await db.query(`SELECT (timestamp with time zone '2026-11-02 02:59:59+00' AT TIME ZONE 'America/Santiago')::date::text domingo,
 date_trunc('week',timestamp with time zone '2026-11-02 02:59:59+00' AT TIME ZONE 'America/Santiago')::date::text lunes,
 (timestamp with time zone '2026-11-01 02:59:59+00' AT TIME ZONE 'America/Santiago')::date::text octubre`)).rows[0];
 check(calendario.domingo==='2026-11-01'&&calendario.lunes==='2026-10-26'&&calendario.octubre==='2026-10-31','El calendario separa semanas y meses por fecha chilena, incluso cerca de medianoche UTC');
 // Exportación privada incluye costos; la plantilla gratuita sigue vacía.
 const response=await fetch(base+'power-bi/paquete',{headers:{Authorization:'Bearer '+admin}});
 check(response.ok,'Power BI exporta el paquete privado con las nuevas valoraciones');
 const paquete=Buffer.from(await response.arrayBuffer());await fs.writeFile(path.join(root,'.local/costos-powerbi-prueba.zip'),paquete);
 const filas=csv(unzip(paquete)['Costos.csv']);
 check(filas.length===4&&filas.some(x=>x.Moneda==='USD'&&Number(x.TotalClp)===180&&Number(x.FactorClp)===900),'CSV de Power BI conserva la moneda original y el equivalente histórico en CLP');
 const plantilla=await fetch(base+'power-bi/demo/plantilla',{headers:{Authorization:'Bearer '+admin}});
 const plantillaBytes=Buffer.from(await plantilla.arrayBuffer());await fs.writeFile(path.join(root,'.local/costos-plantilla-gratuita.zip'),plantillaBytes);
 const archivos=unzip(plantillaBytes);
 check(csv(archivos['Costos.csv']).length===0&&JSON.parse(archivos['Manifiesto.json']).datosReales===false,'Plantilla gratuita añade costos con cabeceras y sin cifras reales o simuladas');
 if(process.env.AFRISAN_TEST_CONTROL_UI==='1'){const {validarPantallasControl}=await import('./validar_pantallas.mjs');await validarPantallasControl({...c,gas2,precios,mes,hoy});}
 await stop();await db.exec('RESET ROLE');
 await db.exec(await fs.readFile(path.join(root,'AFRISAN_NUEVO/02_BASE_DATOS/10_Costos_Limites.sql'),'utf8'));
 for(const tabla of ['PreciosRefrigerantes','TiposCambio','Costos','MedicionLimite','LimiteSucursalHistorial']){
  check(!(await db.query(`SELECT has_table_privilege('afrisan_api','afrisan."${tabla}"','DELETE') d,has_table_privilege('afrisan_api','afrisan."${tabla}"','UPDATE') u`)).rows.some(r=>r.d||r.u),'Identidad restringida no puede editar ni borrar '+tabla);
 }
 check((await db.query('SELECT count(*)::int n FROM afrisan."Auditoria" WHERE "Tabla"=\'TiposCambio\'')).rows[0].n>=4,'Tipos de cambio quedan auditados');
 check((await db.query('SELECT "Id_Refrigerante" gas FROM afrisan."ExcesosRegistrados" WHERE "Id_Medicion"=$1',[antes.id])).rows[0].gas==gas2.id,'La vista de excesos identifica el gas de la medición');
 await db.exec('SET ROLE afrisan_api');await start();
 check((await api('costos',admin)).find(x=>x.id===costoUsd.id).totalClp===180,'Reiniciar y repetir migración conserva precio y cambio históricos');
 check((await api('limites/mediciones',jefatura)).find(x=>x.id===antes.id).grInstalados===2800,'Mediciones sobreviven al reinicio y repetición de la migración');
}
