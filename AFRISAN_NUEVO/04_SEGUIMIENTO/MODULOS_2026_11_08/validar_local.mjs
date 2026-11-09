import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import {randomUUID,createHash} from 'node:crypto';
import vm from 'node:vm';
import {fileURLToPath,pathToFileURL} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../..');
export async function validarLocal(c){
 const {api,check,admin,bodega,autorizador,tecnico,photo}=c;
 const {chromium}=await import(pathToFileURL(path.join(root,'.local/validacion_sql_contexto/node_modules/playwright/index.mjs')));
 const web=path.join(root,'.local/web-offline/wwwroot');
 const tipos={'.html':'text/html','.js':'text/javascript','.json':'application/json','.css':'text/css','.wasm':'application/wasm','.woff':'font/woff','.woff2':'font/woff2','.png':'image/png','.svg':'image/svg+xml'};
 const server=http.createServer(async(req,res)=>{
  try{
   const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);
   const config=JSON.stringify({ApiBaseUrl:'http://127.0.0.1:5199/api/v1/'});
   if(pathname==='/appsettings.json'){res.setHeader('Content-Type','application/json');res.end(config);return;}
   if(pathname==='/service-worker-assets.js'){
    const state={self:{}};vm.runInNewContext(await fs.readFile(path.join(web,'service-worker-assets.js'),'utf8'),state);
    const manifest=state.self.assetsManifest;
    manifest.assets=manifest.assets.filter(x=>x.url!=='appsettings.json');
    manifest.assets.push({url:'appsettings.json',hash:'sha256-'+createHash('sha256').update(config).digest('base64')});
    res.setHeader('Content-Type','text/javascript');res.end('self.assetsManifest = '+JSON.stringify(manifest));return;
   }
   let file=path.resolve(web,'.'+pathname);
   if(!file.startsWith(web+path.sep)&&file!==web){res.writeHead(403);res.end();return;}
   try{if(!(await fs.stat(file)).isFile())file=path.join(web,'index.html');}catch{file=path.join(web,'index.html');}
   res.setHeader('Content-Type',tipos[path.extname(file)]||'application/octet-stream');res.end(await fs.readFile(file));
  }catch{res.writeHead(500);res.end();}
 });
 await new Promise(r=>server.listen(5286,'localhost',r));
 const browser=await chromium.launch({headless:true});
 const context=await browser.newContext({viewport:{width:1360,height:900}});
 let page=await context.newPage();
 try{
  const clientes=await api('clientes',admin),sucursales=await api('sucursales',admin),gases=await api('refrigerantes',admin);
  const cilindro=await api('cilindros',admin,{ubicacionInicial:'Bodega',refrigeranteId:gases[0].id,capacidadGr:10000,taraGr:1000,pesoInicialGr:5000});
  let ot=await api('ordenes-trabajo',autorizador,{idOperacion:randomUUID(),sucursalId:sucursales[0].id,sucursal:sucursales[0].nombre,direccion:sucursales[0].direccion,descripcion:'Prueba offline aislada',tecnicoEmail:'tecnico@example.invalid',detalle:[{refrigeranteId:gases[0].id,grSolicitados:4000,grAutorizados:4000}],cilindrosARetirar:[]});
  const route='ordenes-trabajo/'+ot.id;
  ot=await api(route+'/entregas',tecnico,{idOperacion:randomUUID(),codigo:cilindro.codigo,pesoBrutoGr:4800,foto:photo(110)});
  const entorno=await api('entorno');
  await page.goto('http://localhost:5286/acceso');
  await page.waitForSelector('#correo',{timeout:60000});
  // Sembrar una BD v1: reproduce el almacenamiento que había antes de actualizar la app.
  const id=randomUUID(),fotos=[{...photo(111),grupo:'Instalado',version:0},{...photo(112),grupo:'Instalado',version:0}];
  const borrador={usuario:'tecnico@example.invalid',otId:ot.id,datos:{id,instancia:entorno.instancia,version:0,tipo:'Agregado',codigoInstalado:cilindro.codigo,pesoInstaladoGr:4700,observacion:'Trabajo sin señal'},fotos,fotosEliminar:[],pendiente:true,confirmarAlSincronizar:false};
  await page.evaluate(async({ot,entorno,borrador,token})=>{
   await new Promise((r,j)=>{const req=indexedDB.deleteDatabase('afrisan-nuevo-trabajo');req.onsuccess=r;req.onerror=j;req.onblocked=j;});
   const db=await new Promise((r,j)=>{const req=indexedDB.open('afrisan-nuevo-trabajo',1);req.onupgradeneeded=()=>req.result.createObjectStore('registros',{keyPath:'clave'});req.onsuccess=()=>r(req.result);req.onerror=j;});
   await new Promise((r,j)=>{const tx=db.transaction('registros','readwrite'),s=tx.objectStore('registros');s.put({clave:`borrador:${borrador.usuario}:${borrador.datos.id}`,tipo:'borrador',usuario:borrador.usuario,valor:borrador});s.put({clave:`ot:${borrador.usuario}:${entorno.instancia}:${ot.id}`,tipo:'ot',usuario:`${borrador.usuario}:${entorno.instancia}`,valor:ot});s.put({clave:'borrador:ajeno:otra',tipo:'borrador',usuario:'ajeno',valor:{...borrador,usuario:'ajeno',datos:{...borrador.datos,id:'otra'}}});tx.oncomplete=r;tx.onerror=j;});db.close();
   localStorage.setItem('afrisan_nuevo.sesion',JSON.stringify({Token:token,Vence:new Date(Date.now()+3600000).toISOString(),Nombre:'Prueba Tecnico',Rol:'Tecnico',Usuario:borrador.usuario}));
   localStorage.setItem('afrisan_nuevo.instancia',entorno.instancia);
   localStorage.setItem('afrisan_nuevo.entorno',JSON.stringify({Instancia:entorno.instancia,Modo:entorno.modo,PasswordDemo:'',Cuentas:[]}));
  },{ot,entorno,borrador,token:tecnico});
  const migrado=await page.evaluate(async({id})=>({listado:await afrisanLocal.listar('borrador','tecnico@example.invalid'),uno:await afrisanLocal.obtener('borrador','tecnico@example.invalid',id)}),{id});
  check(migrado.listado.length===1&&migrado.listado[0].fotos.length===0&&migrado.uno.fotos.length===2,'Migración IndexedDB v1→v2 conserva fotos y aísla usuarios sin cargar imágenes en el listado');
  await page.goto('http://localhost:5286/ordenes');
  await page.waitForSelector('h1');
  await page.evaluate(async()=>{await navigator.serviceWorker.ready;if(!navigator.serviceWorker.controller)await new Promise(r=>navigator.serviceWorker.addEventListener('controllerchange',r,{once:true}));});
  await context.setOffline(true);
  await page.close();page=await context.newPage();
  await page.goto(`http://localhost:5286/ordenes/${ot.id}/operacion/${id}`);
  await page.waitForSelector('#op-peso-instalado',{timeout:60000});
  check(await page.locator('#op-peso-instalado').inputValue()==='4700'&&await page.locator('.photo-grid img').count()===2,'Cerrar y reabrir pestaña sin red recupera sesión, OT, borrador y fotos');
  await page.locator('#op-peso-instalado').fill('4600');
  await page.getByRole('button',{name:'Guardar borrador local',exact:true}).click();
  await page.getByText('Borrador y fotos conservados en este navegador.').waitFor();
  check((await page.evaluate(id=>afrisanLocal.obtener('borrador','tecnico@example.invalid',id),id)).datos.pesoInstaladoGr===4600,'Edición sin red queda guardada en IndexedDB');
  let uploads=0,perderFoto=true,perderConfirmacion=false;
  await context.route('**/api/v1/ordenes-trabajo/**/operaciones/**/fotos',async route=>{
   if(route.request().method()!=='POST'){await route.continue();return;}
   uploads++;const respuesta=await route.fetch();
   if(perderFoto){perderFoto=false;await route.abort('failed');}else await route.fulfill({response:respuesta});
  });
  await context.route('**/api/v1/ordenes-trabajo/**/confirmar?*',async route=>{
   const respuesta=await route.fetch();if(perderConfirmacion){perderConfirmacion=false;await route.abort('failed');}else await route.fulfill({response:respuesta});
  });
  await context.setOffline(false);await page.evaluate(async()=>{await fetch('http://127.0.0.1:5199/api/v1/entorno');});
  // La página en edición sincroniza únicamente al pulsar su botón.
  await page.getByRole('button',{name:'Guardar y sincronizar borrador',exact:true}).click();
  await page.getByText('Conservado localmente, pendiente de sincronizar.').waitFor();
  check(uploads===1,'Pérdida de respuesta de foto deja el borrador pendiente en el dispositivo');
  await page.getByRole('button',{name:'Guardar y sincronizar borrador',exact:true}).click();
  await page.getByText('Borrador y fotos guardados en la API;').waitFor();
  check(uploads===2,'Reintento envía solo la foto faltante y reconoce la ya recibida sin reenviarla');
  await page.locator('#op-observacion').fill('Solo cambia el texto después de sincronizar');
  await page.getByRole('button',{name:'Guardar y sincronizar borrador',exact:true}).click();
  await page.getByText('Borrador y fotos guardados en la API;').waitFor();
  check(uploads===2,'Editar datos del borrador no reenvía las fotos sincronizadas');
  perderConfirmacion=true;
  await page.getByRole('button',{name:'Confirmar cambio con evidencias',exact:true}).click();
  await page.getByText('Conservado localmente, pendiente de sincronizar.').waitFor();
  await page.getByRole('button',{name:'Confirmar cambio con evidencias',exact:true}).click();
  await page.getByText('Cambio confirmado con sus pesajes y evidencias.').waitFor();
  check(uploads===2&&(await api(route,tecnico)).operaciones.filter(o=>o.estado==='Registrada').length===1,'Respuesta de confirmación perdida se recupera sin duplicar operación ni fotos');
  await page.screenshot({path:path.join(root,'AFRISAN_NUEVO/04_SEGUIMIENTO/MODULOS_2026_11_08/offline_sincronizado.png'),fullPage:true});
  // Un cambio de instalación debe conservar las imágenes del borrador rechazado.
  const otroId=randomUUID();
  await page.evaluate(async({borrador,otroId})=>afrisanLocal.guardar('borrador',borrador.usuario,otroId,{...borrador,datos:{...borrador.datos,id:otroId,instancia:'otra-base'}}),{borrador,otroId});
  await page.goto('http://localhost:5286/ordenes');
  await page.getByRole('button',{name:'Sincronizar pendientes',exact:true}).click();
  await page.getByText('El borrador corresponde a otra instalación.').waitFor();
  check((await page.evaluate(id=>afrisanLocal.obtener('borrador','tecnico@example.invalid',id),otroId)).fotos.length===2,'Conflicto de instalación conserva las fotos locales');
  await page.getByRole('button',{name:/salir|cerrar sesión/i}).click();
  check(await page.evaluate(()=>localStorage.getItem('afrisan_nuevo.sesion'))===null,'Cerrar sesión elimina la sesión persistida');
  check((await page.evaluate(()=>afrisanLocal.listar('borrador','tecnico@example.invalid'))).length===2,'Cerrar sesión conserva el trabajo pendiente por usuario');
  const cambiarSesion=async(token,rol,usuario)=>{
   await page.evaluate(({token,rol,usuario})=>localStorage.setItem('afrisan_nuevo.sesion',JSON.stringify({Token:token,Vence:new Date(Date.now()+3600000).toISOString(),Nombre:'Prueba '+rol,Rol:rol,Usuario:usuario})),{token,rol,usuario});
   await page.goto(`http://localhost:5286/ordenes/${ot.id}/rectificaciones`);
   await page.getByRole('heading',{name:'Pesajes de '+ot.codigo}).waitFor();
  };
  await cambiarSesion(bodega,'Bodega','bodega@example.invalid');
  const p=(await api(route+'/rectificaciones/pesajes',bodega)).find(p=>p.tipo==='Instalado');
  await page.getByLabel('Pesaje confirmado',{exact:true}).selectOption(p.id);
  await page.getByLabel('Peso bruto correcto (g)',{exact:true}).fill('4500');
  await page.getByLabel('Motivo',{exact:true}).fill('Corrección solicitada desde la pantalla de bodega');
  await page.locator('#rect-foto').setInputFiles({name:'rectificacion.png',mimeType:'image/png',buffer:Buffer.from(photo(113).base64,'base64')});
  await page.getByRole('button',{name:'Enviar al Autorizador',exact:true}).click();
  await page.getByText('Solicitud recibida. El peso vigente se conserva hasta su aprobación.').waitFor();
  check((await api(route+'/rectificaciones',bodega)).length===1,'La pantalla de Bodega registra una solicitud con peso, motivo y evidencia');
  await cambiarSesion(autorizador,'Autorizador','autorizador@example.invalid');
  await page.getByRole('button',{name:'Ver evidencia de la corrección',exact:true}).click();
  await page.locator('img[alt="Evidencia del peso propuesto para rectificación"]').waitFor();
  await page.getByLabel('Motivo de aprobación o rechazo',{exact:true}).fill('Evidencia revisada y peso correcto aprobado');
  await page.getByRole('button',{name:'Aprobar peso propuesto',exact:true}).click();
  await page.getByText('Rectificación aprobada; peso vigente actualizado.').waitFor();
  check((await api(route+'/rectificaciones/pesajes',autorizador)).find(x=>x.id===p.id).pesoVigenteGr===4500,'La pantalla del Autorizador permite ver evidencia y aprobar el peso vigente');
  await page.screenshot({path:path.join(root,'AFRISAN_NUEVO/04_SEGUIMIENTO/MODULOS_2026_11_08/rectificacion_aprobada.png'),fullPage:true});
 }catch(error){await page.screenshot({path:path.join(root,'.local/offline-error.png'),fullPage:true}).catch(()=>{});console.error('Navegador:',error.message);throw error;}finally{await context.close();await browser.close();server.closeAllConnections();await new Promise(r=>server.close(r));}
}
