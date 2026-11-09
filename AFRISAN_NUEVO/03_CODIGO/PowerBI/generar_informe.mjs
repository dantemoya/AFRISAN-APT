// Genera el proyecto editable de Power BI (PBIP/PBIR/TMSL), sin datos de empresa.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const carpeta=path.dirname(fileURLToPath(import.meta.url));
const plantilla=path.resolve(carpeta,'../Afrisan.Nuevo.Api/PowerBi/Plantilla');
const schema='https://developer.microsoft.com/json-schemas/fabric/';
const definition=schema+'item/report/definition/';
const escribir=async(p,datos)=>{await fs.mkdir(path.dirname(p),{recursive:true});await fs.writeFile(p,typeof datos==='string'?datos:JSON.stringify(datos,null,2)+'\n');};
const pq=await fs.readFile(path.join(plantilla,'Afrisan.pq'),'utf8');
const medidas=(await fs.readFile(path.join(plantilla,'Medidas.dax'),'utf8')).split(/\r?\n/).filter(l=>l.trim()&&!l.startsWith('//')).map(l=>{const i=l.indexOf(' = ');return {name:l.slice(0,i),expression:l.slice(i+3),formatString:l.startsWith('Kg')?'#,0.00':'#,0',displayFolder:'Indicadores operativos'};});
const tipos={text:'string',number:'double',date:'dateTime',datetimezone:'dateTime',logical:'boolean'};
const tablas=[];
for(const m of pq.matchAll(/    (\w+) = Leer\("\w+", \{([\s\S]*?)\}\)/g)){
 const nombre=m[1],columnas=[...m[2].matchAll(/\{"([^"]+)",type (\w+)\}/g)].map(x=>({name:x[1],dataType:tipos[x[2]],sourceColumn:x[1],summarizeBy:'none',...(x[2]==='date'?{formatString:'dd/MM/yyyy'}:{})}));
 tablas.push({name:nombre,columns:columnas,partitions:[{name:nombre,mode:'import',source:{type:'m',expression:`Afrisan[${nombre}]`}}]});
 // Cabeceras sin filas: plantilla vacía, nunca métricas de demostración.
 await escribir(path.join(carpeta,'DatosVacios',nombre+'.csv'),columnas.map(c=>c.name).join(',')+'\r\n');
}
tablas.find(t=>t.name==='Inventario').measures=medidas;
for(const c of tablas.find(t=>t.name==='Costos').columns){
 if(['PrecioPorGramo','FactorClp','Total','TotalClp'].includes(c.name))c.formatString='#,0.00000000';
 if(c.name==='Gramos')c.formatString='#,0.######';
}
medidas.find(m=>m.name==='Costo refrigerante CLP').formatString='#,0.00######';
// El filtro de gas no debe crear relaciones bidireccionales entre hechos.
for (const m of medidas) {
 if(m.name.startsWith('OT ')) m.expression=m.expression==='DISTINCTCOUNT(Ordenes[OtId])'
   ? 'CALCULATE(DISTINCTCOUNT(Ordenes[OtId]), TREATAS(VALUES(DetalleOT[OtId]), Ordenes[OtId]))'
   : m.expression.replace('CALCULATE(', 'CALCULATE(').replace(', KEEPFILTERS', ', TREATAS(VALUES(DetalleOT[OtId]), Ordenes[OtId]), KEEPFILTERS');
 if(m.name==='Incidencias abiertas')m.expression=m.expression.replace(', KEEPFILTERS', ', TREATAS(VALUES(DetalleOT[OtId]), Incidencias[OtId]), KEEPFILTERS');
}
const dimensiones=[
 ['Sucursales','SucursalId','Sucursal','Ordenes'],
 ['Tecnicos','TecnicoId','Tecnico','Ordenes'],
 ['Gases','RefrigeranteId','Refrigerante','DetalleOT']
];
for(const [nombre,id,label,origen] of dimensiones){
 const expression=`let Todos = Table.Combine({Table.SelectColumns(Afrisan[${origen}], {"${id}", "${label}"}), Table.SelectColumns(Afrisan[Inventario], {"${id}", "${label}"})}), Valid = Table.SelectRows(Todos, each [${id}] <> null), Unicos = Table.Distinct(Valid, {"${id}"}) in Unicos`;
 tablas.push({name:nombre,columns:[{name:id,dataType:'string',sourceColumn:id,summarizeBy:'none'},{name:label,dataType:'string',sourceColumn:label,summarizeBy:'none'}],partitions:[{name:nombre,mode:'import',source:{type:'m',expression}}]});
}
const relaciones=[];
const relacion=(fromTable,fromColumn,toTable,toColumn)=>relaciones.push({name:`${fromTable}_${toTable}`,fromTable,fromColumn,toTable,toColumn,fromCardinality:'many',toCardinality:'one',crossFilteringBehavior:'oneDirection'});
for(const t of ['DetalleOT','Operaciones','Despachos','Incidencias','Costos'])relacion(t,'OtId','Ordenes','OtId');
for(const t of ['Ordenes','Inventario']){relacion(t,'SucursalId','Sucursales','SucursalId');relacion(t,'TecnicoId','Tecnicos','TecnicoId');}
for(const t of ['Inventario','DetalleOT','Operaciones','Despachos','Costos'])relacion(t,'RefrigeranteId','Gases','RefrigeranteId');
const datos=process.argv[2]?path.resolve(process.argv[2]):path.join(carpeta,'DatosVacios');
await escribir(path.join(carpeta,'AFRISAN.SemanticModel/model.bim'),{name:'AFRISAN',compatibilityLevel:1567,model:{culture:'es-CL',defaultPowerBIDataSourceVersion:'powerBI_V3',sourceQueryCulture:'en-US',dataAccessOptions:{legacyRedirects:true,returnErrorValuesAsNull:true},tables:tablas,relationships:relaciones,expressions:[{name:'CarpetaAfrisan',kind:'m',expression:JSON.stringify(datos)+' meta [IsParameterQuery=true, Type="Text", IsParameterQueryRequired=true]'},{name:'Afrisan',kind:'m',expression:pq.replaceAll('type datetimezone','type datetime')}],annotations:[{name:'PBI_QueryOrder',value:JSON.stringify(['CarpetaAfrisan','Afrisan',...tablas.map(t=>t.name)])}]}});
await escribir(path.join(carpeta,'AFRISAN.SemanticModel/definition.pbism'),{$schema:schema+'item/semanticModel/definitionProperties/1.0.0/schema.json',version:'1.0',settings:{qnaEnabled:false}});
await escribir(path.join(carpeta,'AFRISAN.pbip'),{$schema:schema+'pbip/pbipProperties/1.0.0/schema.json',version:'1.0',artifacts:[{report:{path:'AFRISAN.Report'}}],settings:{enableAutoRecovery:true}});
await escribir(path.join(carpeta,'AFRISAN.Report/definition.pbir'),{$schema:schema+'item/report/definitionProperties/2.0.0/schema.json',version:'4.0',datasetReference:{byPath:{path:'../AFRISAN.SemanticModel'}}});
await escribir(path.join(carpeta,'AFRISAN.Report/definition/version.json'),{$schema:definition+'versionMetadata/1.0.0/schema.json',version:'4.0.0'});
const tema=JSON.parse(await fs.readFile(path.join(plantilla,'Tema_AFRISAN.json'),'utf8'));
await escribir(path.join(carpeta,'AFRISAN.Report/StaticResources/RegisteredResources/Tema_AFRISAN.json'),tema);
await escribir(path.join(carpeta,'AFRISAN.Report/definition/report.json'),{$schema:definition+'report/3.0.0/schema.json',themeCollection:{customTheme:{name:'Tema_AFRISAN',type:'RegisteredResources',reportVersionAtImport:{visual:'2.1.0',page:'2.0.0',report:'3.0.0'}}},resourcePackages:[{name:'RegisteredResources',type:'RegisteredResources',items:[{name:'Tema_AFRISAN',path:'Tema_AFRISAN.json',type:'CustomTheme'}]}]});
const literal=val=>({expr:{Literal:{Value:typeof val==='string'?"'"+val.replaceAll("'","''")+"'":String(val)}}});
const columna=(tabla,nombre)=>({field:{Column:{Expression:{SourceRef:{Entity:tabla}},Property:nombre}},queryRef:`${tabla}.${nombre}`,nativeQueryRef:nombre});
const medida=nombre=>({field:{Measure:{Expression:{SourceRef:{Entity:'Inventario'}},Property:nombre}},queryRef:`Inventario.${nombre}`,nativeQueryRef:nombre});
const paginas=[];
let numero=0;
async function pagina(nombre,titulo){paginas.push(nombre);await escribir(path.join(carpeta,`AFRISAN.Report/definition/pages/${nombre}/page.json`),{$schema:definition+'page/2.0.0/schema.json',name:nombre,displayName:titulo,displayOption:'FitToPage',width:1280,height:720});}
async function visual(pagina,tipo,titulo,x,y,w,h,roles,objects){
 const name='visual_'+(++numero).toString().padStart(3,'0');
 const queryState=Object.fromEntries(Object.entries(roles).map(([rol,campos])=>[rol,{projections:campos}]));
 await escribir(path.join(carpeta,`AFRISAN.Report/definition/pages/${pagina}/visuals/${name}/visual.json`),{$schema:definition+'visualContainer/2.1.0/schema.json',name,position:{x,y,z:numero,width:w,height:h,tabOrder:numero},visual:{visualType:tipo,query:{queryState},visualContainerObjects:{title:[{properties:{show:literal(true),text:literal(titulo),fontSize:literal(13)}}]},...(objects?{objects}:{})}});
}
async function slicer(p,tabla,campo,x,y,w=220){await visual(p,'slicer',campo,x,y,w,100,{Values:[columna(tabla,campo)]});}
await pagina('Resumen','01 · Operación actual');
for(const [i,m] of ['Cilindros registrados','OT abiertas','Kg en cilindros instalados','Incidencias abiertas'].entries())await visual('Resumen','card',m,24+i*314,20,294,130,{Values:[medida(m)]});
await slicer('Resumen','Sucursales','Sucursal',24,165,390);await slicer('Resumen','Tecnicos','Tecnico',440,165,390);await slicer('Resumen','Gases','Refrigerante',856,165,400);
await visual('Resumen','clusteredBarChart','Inventario por estado',24,285,600,405,{Category:[columna('Inventario','Estado')],Y:[medida('Cilindros registrados')]});
await visual('Resumen','clusteredColumnChart','Kg instalados por refrigerante · no equivale a consumo',650,285,606,405,{Category:[columna('Gases','Refrigerante')],Y:[medida('Kg en cilindros instalados')]});
await pagina('Inventario','02 · Inventario y custodia');
await slicer('Inventario','Sucursales','Sucursal',24,20,390);await slicer('Inventario','Gases','Refrigerante',440,20,390);await slicer('Inventario','Tecnicos','Tecnico',856,20,400);
await visual('Inventario','tableEx','Estado actual según el último pesaje vigente',24,145,1232,545,{Values:['Codigo','Refrigerante','Ubicacion','Estado','Sucursal','Tecnico','KgSegunUltimoPesaje','FechaPesaje'].map(c=>columna('Inventario',c))});
await pagina('Ejecucion','03 · Ejecución y pesajes');
await slicer('Ejecucion','Operaciones','DiaConfirmacion',24,20,390);await slicer('Ejecucion','Gases','Refrigerante',440,20,390);await slicer('Ejecucion','Sucursales','Sucursal',856,20,400);
await visual('Ejecucion','lineChart','Kg netos de cilindros instalados por día',24,145,780,245,{Category:[columna('Operaciones','DiaConfirmacion')],Y:[medida('Kg en cilindros instalados')]});
await visual('Ejecucion','card','Retirados pendientes de recepción',830,145,426,245,{Values:[medida('Retirados pendientes recepcion')]});
await visual('Ejecucion','tableEx','Pesos de terreno y recepción (kg)',24,415,1232,275,{Values:['CilindroInstalado','CilindroRetirado','DiaConfirmacion','KgInstalados','KgRetirados','KgRecepcion','PendienteRecepcion'].map(c=>columna('Operaciones',c))});
await pagina('Seguimiento','04 · OT e incidencias');
await slicer('Seguimiento','Ordenes','DiaCreacion',24,20,390);await slicer('Seguimiento','Sucursales','Sucursal',440,20,390);await slicer('Seguimiento','Tecnicos','Tecnico',856,20,400);
await visual('Seguimiento','tableEx','Órdenes de trabajo por fecha de creación',24,145,780,265,{Values:['Codigo','Sucursal','Tecnico','Estado','DiaCreacion','FechaCierre'].map(c=>columna('Ordenes',c))});
await visual('Seguimiento','card','OT cerradas',830,145,426,130,{Values:[medida('OT cerradas')]});
await visual('Seguimiento','card','Incidencias abiertas',830,285,426,125,{Values:[medida('Incidencias abiertas')]});
await visual('Seguimiento','tableEx','Incidencias · sin textos libres ni datos de acceso',24,435,1232,255,{Values:['Tipo','Dia','Abierta','FechaRevision'].map(c=>columna('Incidencias',c))});
await pagina('Costos','05 · Valoración de refrigerante');
await slicer('Costos','Costos','DiaValoracion',24,20,390);await slicer('Costos','Gases','Refrigerante',440,20,390);await slicer('Costos','Sucursales','Sucursal',856,20,400);
for(const [i,m] of ['Costo refrigerante CLP','Gramos valorados','Operaciones valoradas','Valoraciones sin conversion'].entries())await visual('Costos','card',m,24+i*314,145,294,130,{Values:[medida(m)]});
await visual('Costos','tableEx','Valoraciones · precio y cambio conservados; no representa facturación',24,300,1232,390,{Values:['CostoId','OperacionId','Refrigerante','Gramos','PrecioPorGramo','Moneda','Total','FactorClp','TotalClp','FechaCambio','DiaValoracion'].map(c=>columna('Costos',c))});
await escribir(path.join(carpeta,'AFRISAN.Report/definition/pages/pages.json'),{$schema:definition+'pagesMetadata/1.0.0/schema.json',pageOrder:paginas,activePageName:paginas[0]});
console.log(`Proyecto generado: ${paginas.length} páginas, ${numero} visuales, ${tablas.length} tablas y ${medidas.length} medidas.`);
