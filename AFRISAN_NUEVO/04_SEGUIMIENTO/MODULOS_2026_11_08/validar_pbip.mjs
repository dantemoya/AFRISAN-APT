import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(directory,'../../..');
const {default:Ajv}=await import(pathToFileURL(path.join(root,'.local/validacion_sql_contexto/node_modules/ajv/dist/ajv.js')));
const carpeta=path.join(root,'AFRISAN_NUEVO/03_CODIGO/PowerBI');
const cache=path.join(root,'.local/powerbi-schemas');await fs.mkdir(cache,{recursive:true});
const ajv=new Ajv({strict:false,allErrors:true,loadSchema:async uri=>{
 const file=path.join(cache,uri.replaceAll(/[^a-zA-Z0-9.-]/g,'_'));
 try{return JSON.parse(await fs.readFile(file,'utf8'));}catch{}
 const response=await fetch(uri);if(!response.ok)throw Error('No se pudo obtener el esquema '+uri);
 const data=await response.json();await fs.writeFile(file,JSON.stringify(data));return data;
}});
const checks=[];
const check=(ok,nombre)=>{checks.push({resultado:ok?'OK':'FALLO',comprobacion:nombre});if(!ok)throw Error(nombre);};
async function recorrer(dir){for(const e of await fs.readdir(dir,{withFileTypes:true})){
 const file=path.join(dir,e.name);
 if(e.isDirectory()){await recorrer(file);continue;}
 if(!/\.(json|pbip|pbir|pbism)$/.test(e.name))continue;
 const data=JSON.parse(await fs.readFile(file,'utf8'));
 if(data.$schema?.includes('developer.microsoft.com')){
  const validate=await ajv.compileAsync({$ref:data.$schema});
  if(!validate(data))throw Error(path.relative(carpeta,file)+': '+ajv.errorsText(validate.errors));
  check(true,'Esquema Microsoft: '+path.relative(carpeta,file));
 }
}}
const report={fecha_utc:new Date().toISOString(),tipo:'Validación estructural PBIP/PBIR; no sustituye revisión en Desktop',comprobaciones:checks};
try{
 await recorrer(carpeta);
 const model=JSON.parse(await fs.readFile(path.join(carpeta,'AFRISAN.SemanticModel/model.bim'),'utf8')).model;
 const tables=new Map(model.tables.map(t=>[t.name,t]));
 check(model.tables.length===10&&model.tables.flatMap(t=>t.measures||[]).length===18,'Modelo: siete fuentes, tres dimensiones y dieciocho medidas');
 for(const r of model.relationships){check(tables.get(r.fromTable)?.columns.some(c=>c.name===r.fromColumn)&&tables.get(r.toTable)?.columns.some(c=>c.name===r.toColumn),'Relación válida: '+r.name);check(r.crossFilteringBehavior==='oneDirection','Filtro sin ambigüedad: '+r.name);}
 const graphs=new Map();for(const r of model.relationships){if(!graphs.has(r.toTable))graphs.set(r.toTable,[]);graphs.get(r.toTable).push(r.fromTable);}
 for(const origin of tables.keys()){
  const paths=new Map();const visit=(t,trail)=>{for(const dest of graphs.get(t)||[]){if(trail.includes(dest))throw Error('Ciclo de filtros');paths.set(dest,(paths.get(dest)||0)+1);visit(dest,[...trail,dest]);}};visit(origin,[origin]);
  check([...paths.values()].every(n=>n===1),'Un único camino de filtro desde '+origin);
 }
 check(!JSON.stringify(model).includes('Bearer ')&&!JSON.stringify(model).includes('Password='),'El proyecto no incorpora credenciales');
 report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{report.cantidad_comprobaciones=checks.length;await fs.writeFile(path.join(directory,'resultados_pbip.json'),JSON.stringify(report,null,2));console.log(report.estado,checks.length);}
