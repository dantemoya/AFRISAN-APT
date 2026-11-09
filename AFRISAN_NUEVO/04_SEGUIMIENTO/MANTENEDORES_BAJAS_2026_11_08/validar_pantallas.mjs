import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import vm from 'node:vm';
import {createHash} from 'node:crypto';
import {fileURLToPath,pathToFileURL} from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url));

export async function validarPantallas(c){
 const {api,check,admin,bodega,tecnico,cliente,suc,gas,root}=c;
 const entorno=await api('entorno');
 const vacio=await api('clientes',admin,{rut:'UI-SIN-TRABAJO',nombre:'Cliente UI sin trabajos'});
 const gasUi=await api('refrigerantes',admin,{codigo:'UI-GAS',nombre:'Gas para edición UI'});
 const cilindro=await api('cilindros',admin,{ubicacionInicial:'Bodega',refrigeranteId:gas.id,capacidadGr:10000,taraGr:1000,pesoInicialGr:2500});
 const web=path.join(root,'.local/web-mantenedores/wwwroot'),config=JSON.stringify({ApiBaseUrl:'http://127.0.0.1:5199/api/v1/'});
 const tipos={'.html':'text/html','.js':'text/javascript','.json':'application/json','.css':'text/css','.wasm':'application/wasm','.png':'image/png','.svg':'image/svg+xml'};
 const server=http.createServer(async(req,res)=>{
  try{
   const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);
   if(pathname==='/appsettings.json'){res.setHeader('Content-Type','application/json');res.end(config);return;}
   if(pathname==='/service-worker-assets.js'){
    const state={self:{}};vm.runInNewContext(await fs.readFile(path.join(web,'service-worker-assets.js'),'utf8'),state);
    const manifest=state.self.assetsManifest;manifest.assets=manifest.assets.filter(x=>x.url!=='appsettings.json');
    manifest.assets.push({url:'appsettings.json',hash:'sha256-'+createHash('sha256').update(config).digest('base64')});
    res.setHeader('Content-Type','text/javascript');res.end('self.assetsManifest = '+JSON.stringify(manifest));return;
   }
   let file=path.resolve(web,'.'+pathname);
   if(file!==web&&!file.startsWith(web+path.sep)){res.writeHead(403);res.end();return;}
   try{if(!(await fs.stat(file)).isFile())file=path.join(web,'index.html');}catch{file=path.join(web,'index.html');}
   res.setHeader('Content-Type',tipos[path.extname(file)]||'application/octet-stream');res.end(await fs.readFile(file));
  }catch{res.writeHead(500);res.end();}
 });
 await new Promise(r=>server.listen(5286,'localhost',r));
 const {chromium}=await import(pathToFileURL(path.join(root,'.local/validacion_sql_contexto/node_modules/playwright/index.mjs')));
 const browser=await chromium.launch({headless:true}),context=await browser.newContext({viewport:{width:1360,height:980}}),page=await context.newPage();
 const url='http://localhost:5286';
 try{
  await page.goto(url+'/acceso');await page.locator('#correo').waitFor({timeout:60000});
  async function sesion(token,rol,usuario,ruta){
   await page.evaluate(({token,rol,usuario,instancia})=>{
    localStorage.setItem('afrisan_nuevo.instancia',instancia);
    localStorage.setItem('afrisan_nuevo.sesion',JSON.stringify({Token:token,Vence:new Date(Date.now()+3600000).toISOString(),Nombre:'Prueba '+rol,Rol:rol,Usuario:usuario}));
   },{token,rol,usuario,instancia:entorno.instancia});
   await page.goto(url+ruta);
  }
  const gestionar=nombre=>page.getByRole('row').filter({has:page.getByRole('cell',{name:nombre,exact:true})}).getByRole('button',{name:'Gestionar',exact:true}).click();
  await sesion(admin,'Administrador','admin@example.invalid','/mantenedores');
  await page.locator('#mant-buscar').fill('Cliente corregido');await gestionar('Cliente corregido');
  await page.locator('#mant-nombre').fill('Cliente corregido desde pantalla');await page.getByRole('button',{name:'Guardar cambios',exact:true}).click();
  await page.getByText('Cambios guardados. Se conserva la identidad y el historial del registro.',{exact:true}).waitFor();
  check((await api('mantenedores/clientes',admin)).find(x=>x.id===cliente.id).nombre==='Cliente corregido desde pantalla','Pantalla de mantenedores edita y guarda el cliente');
  await page.getByRole('button',{name:'Desactivar registro',exact:true}).click();
  await page.getByRole('alert').waitFor();
  check((await page.getByRole('alert').innerText()).includes('OT abiertas o cilindros instalados'),'Pantalla explica por qué se bloquea una desactivación con trabajo activo');
  await page.locator('#mant-tipo').selectOption('Sucursal');await page.locator('#mant-buscar').fill('Sucursal corregida');await gestionar('Sucursal corregida');
  await page.locator('#mant-direccion').fill('Dirección actualizada desde pantalla');await page.getByRole('button',{name:'Guardar cambios',exact:true}).click();
  await page.getByText('Cambios guardados. Se conserva la identidad y el historial del registro.',{exact:true}).waitFor();
  check((await api('mantenedores/sucursales',admin)).find(x=>x.id===suc.id).direccion==='Dirección actualizada desde pantalla','Pantalla edita la dirección de la sucursal');
  await page.locator('#mant-tipo').selectOption('Cliente');await page.locator('#mant-buscar').fill(vacio.nombre);await gestionar(vacio.nombre);
  await page.getByRole('button',{name:'Desactivar registro',exact:true}).click();
  await page.getByText('Estado actualizado. Los registros anteriores se conservan.',{exact:true}).waitFor();
  check(await page.locator('table tbody tr').count()===0,'Cliente desactivado desaparece de la selección activa');
  await page.getByLabel('Mostrar también inactivos',{exact:true}).check();
  check(await page.getByRole('cell',{name:'Inactivo',exact:true}).isVisible(),'Mostrar inactivos permite consultar el registro conservado');
  await page.getByRole('button',{name:'Activar registro',exact:true}).click();
  await page.getByRole('button',{name:'Desactivar registro',exact:true}).waitFor();
  check((await api('mantenedores/clientes',admin)).find(x=>x.id===vacio.id).activo,'Pantalla permite reactivar el cliente');
  await page.screenshot({path:path.join(directory,'mantenedores_administrador.png'),fullPage:true});
  await sesion(bodega,'Bodega','bodega@example.invalid','/mantenedores');
  await page.locator('#mant-buscar').fill('UI-GAS');await gestionar(gasUi.nombre);
  check(await page.locator('#mant-tipo option').count()===1,'Bodega solo ve el mantenedor de refrigerantes');
  await page.locator('#mant-nombre').fill('Gas editado por Bodega UI');await page.getByRole('button',{name:'Guardar cambios',exact:true}).click();
  await page.getByText('Cambios guardados. Se conserva la identidad y el historial del registro.',{exact:true}).waitFor();
  check((await api('mantenedores/refrigerantes',bodega)).find(x=>x.id===gasUi.id).nombre==='Gas editado por Bodega UI','Bodega edita el gas desde su pantalla autorizada');
  await page.goto(url+'/bajas-cilindros');await page.getByRole('heading',{name:'Acceso restringido',exact:true}).waitFor();
  check(await page.locator('#baja-codigo').count()===0,'Bodega no accede a bajas mediante URL directa');
  await sesion(admin,'Administrador','admin@example.invalid','/bajas-cilindros?codigo='+cilindro.codigo);
  await page.getByRole('option',{name:new RegExp(cilindro.codigo)}).waitFor({state:'attached'});
  check(await page.locator('#baja-codigo').inputValue()===cilindro.codigo,'La ruta de baja admite el cilindro preseleccionado desde su ficha');
  await page.locator('#baja-motivo').fill('Cilindro deteriorado en revisión de la pantalla de administración');
  await page.getByRole('button',{name:'Registrar baja',exact:true}).click();
  await page.getByRole('status').filter({hasText:'Baja registrada para '+cilindro.codigo}).waitFor();
  const baja=(await api('bajas-cilindros',admin)).find(b=>b.codigo===cilindro.codigo);
  check(baja?.estadoDestino==='Chatarra'&&await page.getByRole('cell',{name:baja.motivo,exact:true}).isVisible(),'Formulario de baja persiste el motivo y muestra el historial');
  check(!(await page.locator('#baja-codigo option').allTextContents()).some(x=>x.includes(cilindro.codigo)),'El cilindro dado de baja sale de la lista de candidatos');
  await page.screenshot({path:path.join(directory,'bajas_administrador.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),'Bajas de cilindros se adapta al ancho móvil');
  await page.goto(url+'/mantenedores');await page.locator('#mant-tipo').waitFor();
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),'Mantenedores se adapta al ancho móvil');
  await sesion(tecnico,'Tecnico','tecnico@example.invalid','/mantenedores');
  await page.getByRole('heading',{name:'Acceso restringido',exact:true}).waitFor();
  check(true,'Técnico no puede abrir los mantenedores por URL');
 }catch(error){await page.screenshot({path:path.join(root,'.local/mantenedores-error.png'),fullPage:true}).catch(()=>{});throw error;}
 finally{await context.close();await browser.close();server.closeAllConnections();await new Promise(r=>server.close(r));}
}
