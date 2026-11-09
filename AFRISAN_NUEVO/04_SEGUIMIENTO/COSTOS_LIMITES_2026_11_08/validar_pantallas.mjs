import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import vm from 'node:vm';
import {createHash} from 'node:crypto';
import {fileURLToPath,pathToFileURL} from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url));

export async function validarPantallasControl(c){
 const {api,check,admin,bodega,tecnico,jefatura,root,gas,gas2,op,sucursal,entorno,precios,mes,hoy}=c;
 const gasUi=await api('refrigerantes',bodega,{codigo:'CONTROL-UI',nombre:'Gas de demostración UI'});
 const web=path.join(root,'.local/web-control/wwwroot'),config=JSON.stringify({ApiBaseUrl:'http://127.0.0.1:5199/api/v1/'});
 const tipos={'.html':'text/html','.js':'text/javascript','.json':'application/json','.css':'text/css','.wasm':'application/wasm','.png':'image/png','.svg':'image/svg+xml'};
 const server=http.createServer(async(req,res)=>{try{
  const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);
  if(pathname==='/appsettings.json'){res.setHeader('Content-Type','application/json');res.end(config);return;}
  if(pathname==='/service-worker-assets.js'){
   const state={self:{}};vm.runInNewContext(await fs.readFile(path.join(web,'service-worker-assets.js'),'utf8'),state);
   const manifest=state.self.assetsManifest;manifest.assets=manifest.assets.filter(x=>x.url!=='appsettings.json');
   manifest.assets.push({url:'appsettings.json',hash:'sha256-'+createHash('sha256').update(config).digest('base64')});
   res.setHeader('Content-Type','text/javascript');res.end('self.assetsManifest = '+JSON.stringify(manifest));return;
  }
  let file=path.resolve(web,'.'+pathname);if(file!==web&&!file.startsWith(web+path.sep)){res.writeHead(403);res.end();return;}
  try{if(!(await fs.stat(file)).isFile())file=path.join(web,'index.html');}catch{file=path.join(web,'index.html');}
  res.setHeader('Content-Type',tipos[path.extname(file)]||'application/octet-stream');res.end(await fs.readFile(file));
 }catch{res.writeHead(500);res.end();}});
 await new Promise(r=>server.listen(5286,'localhost',r));
 const {chromium}=await import(pathToFileURL(path.join(root,'.local/validacion_sql_contexto/node_modules/playwright/index.mjs')));
 const browser=await chromium.launch({headless:true}),context=await browser.newContext({viewport:{width:1360,height:980}}),page=await context.newPage();
 const errores=[];page.on('pageerror',e=>errores.push(e.message));const url='http://localhost:5286';
 try{
  await page.goto(url+'/acceso');await page.locator('#correo').waitFor({timeout:60000});
  async function sesion(token,rol,ruta){
   await page.evaluate(({token,rol,instancia})=>{
    localStorage.setItem('afrisan_nuevo.instancia',instancia);
    localStorage.setItem('afrisan_nuevo.sesion',JSON.stringify({Token:token,Vence:new Date(Date.now()+3600000).toISOString(),Nombre:'Prueba '+rol,Rol:rol,Usuario:rol.toLowerCase()+'@example.invalid'}));
   },{token,rol,instancia:entorno.instancia});await page.goto(url+ruta);
  }
  await sesion(admin,'Administrador','/costos');await page.locator('#cambio-moneda').waitFor();
  await page.locator('#cambio-moneda').selectOption('EUR');await page.locator('#cambio-valor').fill('1100.25');await page.locator('#cambio-fecha').fill(hoy);
  await page.getByRole('button',{name:'Guardar valor de cambio',exact:true}).click();await page.getByText('Valor manual de cambio registrado.',{exact:true}).waitFor();
  check((await api('costos/cambios',admin))[0].clpPorUnidad===1100.25,'Pantalla guarda el valor manual con decimales');
  await page.locator('#precio-gas').selectOption(String(gasUi.id));await page.locator('#precio-moneda').selectOption('UF');await page.locator('#precio-valor').fill('0.00001234');await page.locator('#precio-fecha').fill(hoy);
  await page.getByRole('button',{name:'Registrar precio',exact:true}).click();await page.getByText('Precio registrado; se conservan los precios anteriores.',{exact:true}).waitFor();
  check((await api('costos/precios',admin)).some(p=>p.refrigeranteId===gasUi.id&&p.moneda==='UF'&&p.precioPorGramo===0.00001234),'Pantalla conserva ocho decimales en precios UF por gramo');
  await page.locator('#costo-operacion').selectOption(op.id);await page.locator('#costo-precio').selectOption(String(precios.CLP.id));await page.locator('#costo-gramos').fill('25.5');await page.locator('#costo-gramos').blur();
  check((await page.getByRole('status').filter({hasText:'Total:'}).innerText()).includes('51'),'Calculadora muestra el resultado de gramos por precio');
  await page.getByRole('button',{name:'Guardar valoración',exact:true}).click();await page.getByText('Valoración guardada con su precio y conversión aplicada.',{exact:true}).waitFor();
  check((await api('costos',admin)).some(x=>x.gramos===25.5&&x.totalClp===51),'Guardar desde pantalla conserva la valoración por operación');
  await page.screenshot({path:path.join(directory,'costos_administrador.png'),fullPage:true});
  await sesion(jefatura,'Jefatura','/costos');await page.getByRole('heading',{name:'Valoraciones guardadas',exact:true}).waitFor();
  check(await page.locator('#precio-valor').count()===0&&await page.locator('#costo-gramos').count()===0,'Jefatura consulta costos sin formularios de registro');
  await sesion(bodega,'Bodega','/limites');await page.locator('#limite-sucursal').waitFor();
  await page.locator('#limite-sucursal').selectOption(String(sucursal.id));await page.locator('#limite-gas').selectOption(String(gasUi.id));await page.locator('#limite-periodicidad').selectOption('Mes');await page.locator('#limite-gramos').fill('2000');await page.locator('#limite-vigencia').fill(mes);
  await page.getByRole('button',{name:'Guardar límite',exact:true}).click();await page.getByText('Límite guardado con una versión en el historial.',{exact:true}).waitFor();
  check((await api('limites/definiciones',bodega)).some(x=>x.refrigeranteId===gasUi.id&&x.grLimite===2000),'Bodega define un límite mensual por gas desde la pantalla');
  const fila=page.locator('table').first().getByRole('row').filter({hasText:gas2.codigo});
  await page.locator('#limite-observacion').fill('Medición registrada desde la pantalla');await fila.getByRole('button',{name:'Guardar medición',exact:true}).click();
  await page.getByText('Medición guardada con las operaciones y pesos de este corte.',{exact:true}).waitFor();
  const medida=(await api('limites/mediciones',bodega)).find(x=>x.observaciones==='Medición registrada desde la pantalla');
  check(medida?.grInstalados===2700,'Pantalla guarda una medición con el peso rectificado vigente');
  await page.screenshot({path:path.join(directory,'limites_bodega.png'),fullPage:true});
  await sesion(jefatura,'Jefatura','/limites');await page.locator('#limite-sucursal').waitFor();
  const revision=page.getByRole('row').filter({hasText:'medición #'+medida.id});await revision.getByRole('button',{name:'Revisar exceso',exact:true}).click();
  await page.locator('#exceso-conclusion').fill('Exceso revisado por Jefatura desde pantalla');await page.getByRole('button',{name:'Guardar conclusión',exact:true}).click();
  await page.getByText('Conclusión de Jefatura guardada.',{exact:true}).waitFor();
  check((await api('limites/mediciones',jefatura)).find(x=>x.id===medida.id).conclusion==='Exceso revisado por Jefatura desde pantalla','Jefatura registra la conclusión de un exceso desde su pantalla');
  for(const ruta of ['/costos','/limites']){
   await sesion(tecnico,'Tecnico',ruta);await page.getByRole('heading',{name:'Acceso restringido',exact:true}).waitFor();check(true,'Técnico no accede a '+ruta);
  }
  await page.setViewportSize({width:390,height:844});await sesion(jefatura,'Jefatura','/limites');await page.locator('#limite-fecha').waitFor();
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Límites se adapta al ancho móvil sin desbordar la página');
  await page.screenshot({path:path.join(directory,'limites_movil.png'),fullPage:true});
  check(errores.length===0,'Las pantallas funcionan sin errores JavaScript');
 }catch(error){await page.screenshot({path:path.join(root,'.local/control-ui-fallo.png'),fullPage:true});throw error;}
 finally{await browser.close();await new Promise(r=>server.close(r));}
}
