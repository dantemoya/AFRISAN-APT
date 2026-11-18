# Despliegue público de AFRISAN

Guía para publicar la API y la web con HTTPS. **Estado al 09/11/2026:** la
aplicación está preparada para producción y la base de Supabase ya tiene las
migraciones `08` a `10`, pero la API y la web todavía no están desplegadas. El
proveedor de hosting y su cuenta los elige y crea el responsable del proyecto.

## 1. Qué cambia fuera de Development

| Comportamiento | Development | Producción |
| --- | --- | --- |
| Swagger (`/swagger`) | Activo | Desactivado |
| HTTPS | Opcional | `UseHsts` + `UseHttpsRedirection` |
| Límite de inicio de sesión | Desactivado | 10 intentos por minuto e IP (`Seguridad:LoginPorMinuto`) |
| Modo memoria | Permitido | La API se niega a iniciar |
| Conexión PostgreSQL | `VerifyFull`, salvo pruebas locales | Siempre `VerifyFull` |

La cámara del celular (lectura QR y fotos de balanza) solo funciona con HTTPS o
en `localhost`. Por eso las pruebas de terreno (G05) requieren este despliegue.

## 2. Base de datos (Supabase)

**Estado:** las migraciones `08`, `09` y `10` se aplicaron en el proyecto
Supabase de AFRISAN el 09/11/2026, desde el editor SQL del panel
([registro y verificación](../04_SEGUIMIENTO/SUPABASE_2026_11_09.md)). La `11` se
aplicó el 15/11/2026 con el comando de la API
([registro](../04_SEGUIMIENTO/CORRECCIONES_2026_11_14/README.md)). Este paso solo se
repite en una instancia nueva o de pruebas.

Para otra instancia, aplicar las migraciones **en este orden**, con la conexión de
propietario configurada localmente (`ConnectionStrings:Supabase`, ver
[CONEXION_SUPABASE.md](CONEXION_SUPABASE.md)). Cada script es aditivo, se
ejecuta en una transacción y puede repetirse:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-rectificaciones   # 08
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-mantenedores      # 09
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-costos-limites    # 10
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-devolucion-recarga # 11
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-codigos-fotos-pedido # 12
```

La migración `11` (devolución, anulación, tolerancia de peso y recarga, D-39 a D-43)
se aplicó en Supabase el 15/11/2026 con el último comando, después de probarla en
PostgreSQL embebido. `--verificar-supabase` y `--verificar-persistencia` terminaron sin
errores. La API de esta versión la exige: en una instancia sin `11`,
`--verificar-supabase` informa «migración 11».

La migración `12` (códigos cortos, foto obligatoria en todo pesaje y datos del pedido en la
OT, D-47 a D-49) se probó el 16/11/2026 en PostgreSQL embebido
([registro](../04_SEGUIMIENTO/DEFECTOS_2026_11_16/README.md)) y se **aplicó en Supabase el
17/11/2026**, justo antes de publicar la API nueva en Azure. La API de esta versión la exige;
sin `12`, `--verificar-supabase` informa «migración 12».

Si falla, el mensaje incluye el SQLSTATE y no queda ningún cambio parcial.
La conexión de propietario **no** se configura en el servidor público.

## 3. API (ASP.NET Core 8)

Publicar con `dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -c Release`
y definir estas variables de entorno en el hosting (nunca en Git):

| Variable | Contenido |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__AfrisanRuntime` | Conexión del rol restringido `afrisan_api` (Session pooler de Supabase) |
| `Persistencia__JwtKey` | Clave Base64 de al menos 64 bytes para firmar los JWT |
| `Persistencia__FotosPath` | Carpeta privada del servidor, fuera de `wwwroot` |
| `Storage__Proveedor`, `Storage__Url`, `Storage__Bucket`, `Storage__SecretKey` | Supabase Storage privado (ver [STORAGE.md](STORAGE.md)) |
| `Cors__Origins__0` | URL HTTPS exacta de la web publicada |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` si el hosting termina TLS en un proxy (Azure App Service, Render, etc.) |
| `Seguridad__LoginPorMinuto` | Opcional; 10 por defecto |
| `Seguridad__ApiPorMinuto` | Opcional; límite global por usuario/IP, 300/min por defecto (0 lo desactiva) |
| `PowerBi__ReportId`, `PowerBi__TenantId` | Opcional; informe privado de la empresa |

Comprobar después de publicar:

- `GET https://<api>/health/ready` responde 200 con `"supabase": true`.
- `GET https://<api>/swagger` responde 404.
- `http://<api>` redirige a HTTPS.

## 4. Web (Blazor WebAssembly)

1. Editar `Afrisan.Nuevo.Web/wwwroot/appsettings.json` y cambiar `ApiBaseUrl` por
   `https://<api>/api/v1/` (termina en `/`). El valor del repositorio apunta a
   `http://localhost:5181/api/v1/` para el desarrollo local.
2. Publicar con `dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release`.
3. Subir el contenido de `bin/Release/net8.0/publish/wwwroot` a un hosting
   estático con HTTPS y redirección de rutas desconocidas a `index.html`
   (por ejemplo Azure Static Web Apps o GitHub Pages con `404.html`).
4. Abrir la web en un celular, iniciar sesión y comprobar que la cámara pide permiso.

## 5. Opción con contenedores

Los archivos de [`Despliegue/`](Despliegue) sirven para hostings que ejecutan
imágenes Docker, como Azure Container Apps, Render, Railway o Fly.io. El contexto
de construcción es la raíz del repositorio.

| Archivo | Qué hace |
| --- | --- |
| `Despliegue/Dockerfile.api` | Publica la API con el SDK 8 y la ejecuta en `aspnet:8.0` como usuario sin privilegios, en el puerto 8080, con `Production` y `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` |
| `Despliegue/Dockerfile.web` | Publica la web con `ApiBaseUrl` fijado **antes** de publicar (el service worker guarda cada archivo con su hash) y la sirve con nginx en el puerto 8080 |
| `Despliegue/nginx.conf` | Redirige las rutas de Blazor a `index.html`, sirve los `.gz`, agrega cabeceras de seguridad y evita la caché de `index.html`, del service worker y de `appsettings.json` |
| `.dockerignore` (raíz) | Deja fuera `.git`, `.local`, las carpetas de fases, las evidencias y los secretos locales |
| `.github/workflows/imagenes.yml` | Ejecución manual: construye las dos imágenes y las publica en `ghcr.io/<usuario>/afrisan-api` y `afrisan-web`, solo con el `GITHUB_TOKEN` automático |

```powershell
docker build -f AFRISAN_NUEVO/03_CODIGO/Despliegue/Dockerfile.api -t afrisan-api .
docker build -f AFRISAN_NUEVO/03_CODIGO/Despliegue/Dockerfile.web --build-arg API_BASE_URL=https://<api>/api/v1/ -t afrisan-web .
```

Las variables de la sección 3 se definen en el hosting del contenedor de la API.
La web solo necesita su URL en `Cors__Origins__0` de la API.

**Verificado el 08/11/2026:** la publicación de ambos proyectos con el SDK 8.0.427
(los pasos `dotnet publish` de los Dockerfile) termina sin errores. En este equipo
no hay Docker, así que no se construyeron ni ejecutaron las imágenes.

## 6. Despliegue vigente en Azure (15/11/2026, D-46)

| Pieza | Recurso | Dirección |
| --- | --- | --- |
| Grupo de recursos | `rg-afrisan` (East US), suscripción Azure for Students | — |
| API | App Service `afrisan-api-dm`, plan `plan-afrisan` **B1 Linux** con *Always On* | https://afrisan-api-dm.azurewebsites.net |
| Web | Static Web App `afrisan-web` (**Free**, East US 2) | https://ambitious-field-0d951cb0f.6.azurestaticapps.net |
| Base y fotos | Supabase us-east-1 (sin cambios) | — |

- **API autocontenida:** App Service ya no ofrece .NET 8 (su soporte terminó el 10/11/2026), así
  que la API se publica con `-r linux-x64 --self-contained true` y su propio runtime .NET 8, sobre
  la imagen Linux de .NET 10. Comando de inicio: `chmod +x /home/site/wwwroot/Afrisan.Nuevo.Api && /home/site/wwwroot/Afrisan.Nuevo.Api`.
- **Certificado de Supabase:** el paquete incluye `certificates/supabase.crt` (certificado público de
  la CA) y la conexión usa `Root Certificate=/home/site/wwwroot/certificates/supabase.crt`.
- **Ajustes:** `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`,
  `ASPNETCORE_URLS=http://+:8080`, `WEBSITES_PORT=8080`, `Persistencia__FotosPath=/home/afrisan/fotos`,
  `Cors__Origins__0` con la dirección de la web. Los secretos (`ConnectionStrings__AfrisanRuntime` y
  una `Persistencia__JwtKey` propia de producción) los carga el autor con un script local que no los
  muestra. Sin claves de Storage, las fotos quedan en el disco persistente del App Service (DEF-11).
- **Web:** se publica con `dotnet publish` y, solo en la copia que se sube, `appsettings.json` apunta a
  `https://afrisan-api-dm.azurewebsites.net/api/v1/` (con el hash del *service worker* actualizado). El repositorio sigue apuntando a
  localhost para las baterías locales. `wwwroot/staticwebapp.config.json` define las rutas de la SPA.
- **Plan:** F1 (gratis) se descartó el mismo día: un arranque fallido en bucle agotó sus 60 minutos
  diarios de CPU y Azure detuvo la API. B1 cuesta unos US$13 al mes y se descuenta del crédito de
  estudiante (USD 100, vence el 21/04/2027); se puede volver a F1 después de la presentación.
- **Verificado el 15/11:** `/health/ready` 200 con `"supabase": true`; `/swagger` 404; HTTP redirige a
  HTTPS (301); CORS solo para la web; la web lee `entorno` desde la API; el guion de Power BI aprueba
  62 de 62 contra la API de Azure ([resultados](../04_SEGUIMIENTO/POWER_BI_2026_11_15/resultados_powerbi_supabase_azure.json)).

Para volver a publicar (Azure CLI con sesión iniciada):

```powershell
dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -c Release -r linux-x64 --self-contained true -o <carpeta>/api
# copiar el certificado público de Supabase a <carpeta>/api/certificates/supabase.crt y comprimir el contenido de <carpeta>/api en api.zip
az webapp deploy -g rg-afrisan -n afrisan-api-dm --src-path <carpeta>/api.zip --type zip
```

La web se publica con `npx @azure/static-web-apps-cli deploy <carpeta>/web/wwwroot --env production`,
con el token de despliegue en la variable `SWA_CLI_DEPLOYMENT_TOKEN` (`az staticwebapp secrets list`).
El comando se ejecuta desde `<carpeta>/web`: desde la raíz de una unidad la herramienta intenta leer
carpetas del sistema y falla.

**Publicación del 17/11/2026:** API y web con D-47 a D-49 y la búsqueda por código corregida, después
de aplicar la migración `12`. Verificado: `/health/ready` 200 con `"supabase": true`, `/swagger` 404,
HTTP a HTTPS 301, CORS solo para la web, la ruta nueva `/pesajes` existe (401 sin sesión), la web
publicada apunta a la API de Azure, carga sin errores de consola bajo la CSP e incluye las pantallas
nuevas.

## 7. Después del despliegue

- Registrar la URL pública en el informe final y en el README raíz.
- Ejecutar G05 (sin red y con cámara en Android) y G09 (aceptación con usuarios).
- Crear las cuentas reales desde `/administracion`; no reutilizar las de prueba.
