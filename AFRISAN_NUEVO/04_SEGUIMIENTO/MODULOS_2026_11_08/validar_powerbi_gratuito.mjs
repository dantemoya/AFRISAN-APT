// API y Blazor publicados con perfiles aislados. El iframe se simula solo en la prueba.
import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import vm from 'node:vm';
import {inflateRawSync} from 'node:zlib';
import {spawn} from 'node:child_process';
import {randomUUID,createHash} from 'node:crypto';
import {fileURLToPath,pathToFileURL} from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(directory,'../../..');
const profile=path.join(root,'.local','powerbi-gratuito-'+randomUUID());await fs.mkdir(profile,{recursive:true});
const archivo=path.join(profile,'configuracion.json');
const base='http://127.0.0.1:5207/api/v1/';
const env={...process.env,APPDATA:profile,LOCALAPPDATA:profile,ASPNETCORE_ENVIRONMENT:'Development',ASPNETCORE_URLS:'http://127.0.0.1:5207',
 Persistencia__Modo:'Memoria',PowerBiDemo__ArchivoConfiguracion:archivo,PowerBiDemo__UrlPublica:'',Cors__Origins__0:'http://localhost:5297',Logging__EventLog__LogLevel__Default:'None'};
let processApi,logs='',server,browser;
const checks=[];
const report={fecha_utc:new Date().toISOString(),tipo:'Power BI gratuito: API real, configuración aislada y Web publicada; iframe simulado, sin cuenta Microsoft',comprobaciones:checks};
function check(ok,name){checks.push({resultado:ok?'OK':'FALLO',comprobacion:name});if(!ok)throw Error(name);console.log('OK:',name);}
async function stop(){if(processApi&&processApi.exitCode===null){const done=new Promise(r=>processApi.once('exit',r));processApi.kill();await done;}processApi=null;}
async function start(){
 processApi=spawn('dotnet',[path.join(root,'AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api/bin/Debug/net8.0/Afrisan.Nuevo.Api.dll')],{cwd:root,env,windowsHide:true,stdio:['ignore','pipe','pipe']});
 processApi.stdout.on('data',x=>logs+=x);processApi.stderr.on('data',x=>logs+=x);
 for(let i=0;i<150;i++){if(processApi.exitCode!==null)throw Error('La API no inició; revisar el log aislado.');try{if((await fetch('http://127.0.0.1:5207/health/ready')).ok)return;}catch{}await new Promise(r=>setTimeout(r,100));}
 throw Error('Timeout de inicio de la API.');
}
async function api(route,token,body,method=body===undefined?'GET':'PUT',expected=200){
 const response=await fetch(base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});
 const text=await response.text();let result=null;try{result=text?JSON.parse(text):null;}catch{result=text;}
 if(response.status!==expected)throw Error(`${method} ${route}: esperado ${expected}, obtenido ${response.status}: ${JSON.stringify(result)}`);return {data:result,headers:response.headers};
}
const password='DemoAfrisan2026!'; // Solo cuentas públicas del modo Memoria de Development.
const login=async rol=>(await api('sesiones',null,{email:rol+'@afrisan.example',password},'POST')).data.token;
const configRoute='power-bi/demo/configuracion';
const url='https://app.powerbi.com/view?r=eyJkIjoiZGVtbyJ9';
function unzip(bytes){
 const result={};let end=bytes.length-22;
 while(end>=0&&bytes.readUInt32LE(end)!==0x06054b50)end--;
 if(end<0)throw Error('ZIP sin directorio');
 let at=bytes.readUInt32LE(end+16);
 for(let i=0;i<bytes.readUInt16LE(end+10);i++){
  const method=bytes.readUInt16LE(at+10),size=bytes.readUInt32LE(at+20),n=bytes.readUInt16LE(at+28),extra=bytes.readUInt16LE(at+30),comment=bytes.readUInt16LE(at+32),offset=bytes.readUInt32LE(at+42);
  const name=bytes.subarray(at+46,at+46+n).toString('utf8');
  const start=offset+30+bytes.readUInt16LE(offset+26)+bytes.readUInt16LE(offset+28);
  const data=bytes.subarray(start,start+size);result[name]=(method===8?inflateRawSync(data):data).toString('utf8');at+=46+n+extra+comment;
 }return result;
}
try{
 await start();
 let admin=await login('administrador'),jefatura=await login('jefatura');
 await api(configRoute,null,undefined,'GET',401);check(true,'Consulta anónima rechazada');
 await api('power-bi/demo/plantilla',null,undefined,'GET',401);
 for(const rol of ['tecnico','bodega','autorizador']){const token=await login(rol);await api(configRoute,token,undefined,'GET',403);await api('power-bi/demo/plantilla',token,undefined,'GET',403);await api(configRoute,token,{urlPublica:url},'PUT',403);check(true,rol+': consulta, plantilla y configuración rechazadas');}
 let r=await api(configRoute,admin);
 check(!r.data.configurado&&r.data.urlPublica===null&&r.headers.get('cache-control')==='no-store','Sin enlace: estado vacío real, sin informe ficticio ni caché');
 const plantilla=await fetch(base+'power-bi/demo/plantilla',{headers:{Authorization:'Bearer '+jefatura}});
 const files=unzip(Buffer.from(await plantilla.arrayBuffer()));
 check(plantilla.ok&&plantilla.headers.get('content-type')==='application/zip'&&plantilla.headers.get('cache-control')==='no-store','Jefatura descarga la plantilla de Desktop sin conexión a Supabase');
 check(['Inventario','Ordenes','DetalleOT','Operaciones','Despachos','Incidencias','Costos'].every(n=>files[n+'.csv']?.trim().split(/\r?\n/).length===1),'La plantilla contiene únicamente cabeceras: sin datos reales ni registros simulados');
 check(files['Informe/AFRISAN.pbip']&&files['Informe/AFRISAN.SemanticModel/model.bim']&&files['LEEME_DEMO.md']&&JSON.parse(files['Manifiesto.json']).datosReales===false,'Plantilla incluye proyecto, modelo y guía específica de demostración');
 await api(configRoute,jefatura,{urlPublica:url},'PUT',403);check(true,'Jefatura consulta pero no modifica el enlace');
 for(const invalida of ['http://app.powerbi.com/view?r=abc','https://example.com/view?r=abc','https://app.powerbi.com.ejemplo.invalid/view?r=abc','javascript:alert(1)','https://app.powerbi.com/reportEmbed?reportId=abc','https://usuario@app.powerbi.com/view?r=abc','https://app.powerbi.com:444/view?r=abc','https://app.powerbi.com/view?r=a&r=b','https://app.powerbi.com/view?r=','https://app.powerbi.com/view','https://app.powerbi.com/view?r=%3Cscript%3E','<iframe src="'+url+'"></iframe>']){
  await api(configRoute,admin,{urlPublica:invalida},'PUT',400);
 }
 check(true,'Se rechazan enlaces privados, HTML, URLs externas, credenciales y parámetros inválidos');
 r=await api(configRoute,admin,{urlPublica:'  '+url+'&pageName=ReportSection&redirect=https://example.invalid  '});
 check(r.data.configurado&&r.data.urlPublica===url&&r.data.actualizada,'Administrador guarda el enlace público normalizado y su fecha');
 check((await api(configRoute,jefatura)).data.urlPublica===url,'Jefatura recibe la configuración compartida');
 check((await api(configRoute,admin,{urlPublica:url})).data.actualizada===r.data.actualizada,'Repetir el guardado conserva el registro');
 await stop();await start();admin=await login('administrador');jefatura=await login('jefatura');
 check((await api(configRoute,jefatura)).data.urlPublica===url,'El enlace sobrevive al reinicio de la API');
 // Un fallo de disco no cambia la configuración ya confirmada.
 await fs.rename(archivo,archivo+'.respaldo');await fs.mkdir(archivo);
 try{await api(configRoute,admin,{urlPublica:url+'abc'},'PUT',500);check((await api(configRoute,admin)).data.urlPublica===url,'Un fallo al persistir no cambia el enlace activo');}
 finally{await fs.rmdir(archivo);await fs.rename(archivo+'.respaldo',archivo);}
 await api(configRoute,admin,{urlPublica:''});
 await stop();await start();admin=await login('administrador');
 check(!(await api(configRoute,admin)).data.configurado,'Quitar el enlace persiste después de reiniciar');

 const web=path.join(root,'.local/powerbi-demo-web/wwwroot');
 const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.css':'text/css','.wasm':'application/wasm','.png':'image/png','.svg':'image/svg+xml'};
 const config=JSON.stringify({ApiBaseUrl:base});
 server=http.createServer(async(req,res)=>{
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
   res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');res.end(await fs.readFile(file));
  }catch{res.writeHead(500);res.end();}
 });
 await new Promise(r=>server.listen(5297,'localhost',r));
 const {chromium}=await import(pathToFileURL(path.join(root,'.local/validacion_sql_contexto/node_modules/playwright/index.mjs')));
 browser=await chromium.launch({headless:true});
 const context=await browser.newContext({viewport:{width:1360,height:1000}});
 // No se envía el identificador de prueba a Microsoft ni se simula un informe real de AFRISAN.
 await context.route('https://app.powerbi.com/**',route=>route.fulfill({contentType:'text/html',body:'<html lang="es"><body><h1>Iframe de prueba: datos simulados</h1></body></html>'}));
 const page=await context.newPage();let accesosReales=0;
 page.on('request',request=>{if(/\/api\/v1\/power-bi\/(resumen|paquete|configuracion)(\?|$)/.test(request.url()))accesosReales++;});
 async function entrar(rol){await page.goto('http://localhost:5297/acceso');await page.locator('#correo').fill(rol+'@afrisan.example');await page.locator('#clave').fill(password);await page.getByRole('button',{name:'Entrar a AFRISAN →',exact:true}).click();await page.getByRole('button',{name:'Salir',exact:true}).waitFor();}
 async function abrir(){await page.getByRole('link',{name:'Power BI',exact:true}).click();await page.getByRole('heading',{name:'Informe de Power BI',exact:true}).waitFor();await page.getByRole('button',{name:'Actualizar conexión',exact:true}).waitFor();}
 async function salir(){await page.getByRole('button',{name:'Salir',exact:true}).click();await page.locator('#correo').waitFor();}
 await entrar('administrador');await abrir();
 check(new URL(page.url()).pathname==='/power-bi'&&await page.getByText('Aún no hay un informe conectado',{exact:true}).isVisible()&&await page.locator('iframe').count()===0,'Menú principal abre el módulo gratuito sin mostrar un iframe inventado');
 await page.locator('#bi-demo-url').fill(url);await page.getByRole('button',{name:'Guardar enlace',exact:true}).click();
 await page.getByText('Enlace guardado. Revisa el informe mostrado por Microsoft.',{exact:true}).waitFor();
 check(await page.locator('iframe').getAttribute('src')===url,'Formulario del Administrador guarda y muestra el iframe público');
 await page.locator('#bi-demo-url').fill('https://example.invalid/view?r=abc');await page.getByRole('button',{name:'Guardar enlace',exact:true}).click();
 await page.getByRole('alert').waitFor();check((await page.getByRole('alert').innerText()).includes('Usa el enlace público')&&await page.locator('iframe').getAttribute('src')===url,'Enlace inválido en la pantalla explica el formato y conserva el informe anterior');
 await salir();await entrar('jefatura');await abrir();
 check(await page.locator('iframe').getAttribute('src')===url&&await page.locator('#bi-demo-url').count()===0,'Jefatura ve el informe sin controles de configuración');
 const descarga=page.waitForEvent('download');await page.getByRole('button',{name:'Descargar plantilla para Desktop',exact:true}).click();
 const download=await descarga;const destino=path.join(profile,'plantilla.zip');await download.saveAs(destino);
 const descargados=unzip(await fs.readFile(destino));
 check(download.suggestedFilename()==='AFRISAN_PowerBI_Plantilla_Gratuita.zip'&&descargados['LEEME_DEMO.md'],'Botón de Desktop entrega el ZIP vacío mediante descarga autenticada');
 await page.goto('http://localhost:5297/power-bi/demo');await page.locator('iframe').waitFor();
 check(true,'La ruta /power-bi/demo conserva el acceso a la demostración');
 await page.setViewportSize({width:390,height:844});
 check(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),'Pantalla móvil sin desbordamiento horizontal');
 await page.setViewportSize({width:1360,height:1000});
 await page.screenshot({path:path.join(directory,'powerbi_gratuito_jefatura.png'),fullPage:true});
 await salir();await entrar('administrador');await abrir();
 await page.getByRole('button',{name:'Quitar enlace de AFRISAN',exact:true}).click();
 await page.getByText('Enlace retirado de AFRISAN.',{exact:true}).waitFor();
 check(await page.locator('iframe').count()===0,'Administrador puede retirar el enlace desde la aplicación');
 await page.screenshot({path:path.join(directory,'powerbi_gratuito_pendiente.png'),fullPage:true});
 check(accesosReales===0,'La demostración no consulta indicadores ni paquetes de datos reales');
 await salir();await entrar('tecnico');await page.goto('http://localhost:5297/power-bi');
 await page.getByRole('heading',{name:'Acceso restringido',exact:true}).waitFor();
 check(await page.locator('iframe').count()===0,'La pantalla rechaza el rol Técnico también por URL directa');
 await context.close();report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{
 if(browser)await browser.close();if(server){server.closeAllConnections();await new Promise(r=>server.close(r));}await stop();
 await fs.writeFile(path.join(profile,'api.log'),logs);
 report.cantidad_comprobaciones=checks.length;await fs.writeFile(path.join(directory,'resultados_powerbi_gratuito.json'),JSON.stringify(report,null,2));
 console.log('Resultado:',report.estado,'Comprobaciones:',checks.length);
}
