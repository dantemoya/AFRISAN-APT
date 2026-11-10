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
([registro y verificación](../04_SEGUIMIENTO/SUPABASE_2026_11_09.md)). Este paso
solo se repite en una instancia nueva o de pruebas.

Para otra instancia, aplicar las migraciones **en este orden**, con la conexión de
propietario configurada localmente (`ConnectionStrings:Supabase`, ver
[CONEXION_SUPABASE.md](CONEXION_SUPABASE.md)). Cada script es aditivo, se
ejecuta en una transacción y puede repetirse:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-rectificaciones   # 08
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-mantenedores      # 09
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-costos-limites    # 10
```

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

## 6. Después del despliegue

- Registrar la URL pública en el informe final y en el README raíz.
- Ejecutar G05 (sin red y con cámara en Android) y G09 (aceptación con usuarios).
- Crear las cuentas reales desde `/administracion`; no reutilizar las de prueba.
