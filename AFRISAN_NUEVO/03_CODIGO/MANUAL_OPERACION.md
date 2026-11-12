# Manual de operación de AFRISAN

Procedimientos para mantener el sistema funcionando después de la entrega: arranque,
monitoreo, respaldo, recuperación, secretos, cuentas e incidentes. Versión del
**11/11/2026**. Complementa la [guía de despliegue](DESPLIEGUE.md), el
[manual de usuario](MANUAL_USUARIO.md) y la [especificación de la API](API.md).

> Ningún procedimiento de este manual requiere copiar contraseñas o claves en
> documentos, correos o chats. Las credenciales viven en User Secrets (desarrollo)
> o en las variables de entorno del hosting (producción).

## 1. Componentes que se operan

| Componente | Dónde corre | Responsable de operarlo |
| --- | --- | --- |
| API `Afrisan.Nuevo.Api` (.NET 8) | Local en el puerto 5181; en producción, Azure App Service Linux en East US (pendiente S8-02) | Administrador técnico |
| Web `Afrisan.Nuevo.Web` (Blazor WebAssembly) | Local en el puerto 5286; en producción, Azure Static Web Apps con HTTPS | Administrador técnico |
| Base de datos PostgreSQL 16 | Supabase en la región us-east-1, esquema único `afrisan` (32 tablas y 9 vistas) | Propietario del proyecto Supabase |
| Fotografías | Supabase Storage, bucket privado `afrisan-evidencias` | Propietario del proyecto Supabase |
| Integración continua e imágenes | GitHub Actions (`ci.yml`, `imagenes.yml`) | Responsable del repositorio |

La API se conecta con el rol restringido `afrisan_api`, que no es superusuario ni puede
saltarse RLS. Las tareas de mantenimiento de la base (migraciones, respaldos) usan la
conexión del propietario, que no se guarda en el servidor de la API.

## 2. Arranque y detención

**En el equipo de desarrollo:**

```powershell
.\AFRISAN_NUEVO\03_CODIGO\Iniciar-Revision.ps1            # inicia API y web y abre /acceso
.\AFRISAN_NUEVO\03_CODIGO\Iniciar-Revision.ps1 -Detener   # detiene ambos procesos
```

También se puede usar el perfil **AFRISAN NUEVO - AMBOS** de Visual Studio. Si la web
abre antes que la API, muestra "Conectando con la API…" y se conecta sola cuando la API
responde.

**En producción:** reiniciar desde el panel del hosting. Al terminar, comprobar
`GET https://<api>/health/ready` (ver sección 3).

## 3. Monitoreo

| Qué revisar | Cómo | Resultado esperado | Frecuencia |
| --- | --- | --- | --- |
| Salud de la API | `GET /health/ready` | 200 con `"almacenamiento":"PostgreSQL"`, `"supabase":true` y `"fotografias":"SupabaseStorage"` | Diaria o con un monitor externo cada 5 minutos |
| Conexión y esquema | `dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --verificar-supabase` | Código de salida 0; 32 tablas, 9 vistas y RLS activo | Después de cada migración |
| Fotos | `… -- --verificar-storage` | Bucket privado activo, 4 MiB, JPEG/PNG/WebP | Semanal |
| Power BI | `… -- --verificar-powerbi` | `"estado":"CORRECTO"` con el conteo de cilindros, OT, detalles, operaciones, despachos e incidencias | Semanal |
| Conexión de Power BI | `GET /api/v1/power-bi/tablas` sin credenciales | 200 con `"estado":"ok"` y los nombres de las tablas. Si Power BI muestra 401, renovar el token | Si el informe no se actualiza |
| Uso del plan gratuito | Panel de Supabase: tamaño de base, Storage y egreso | Por debajo de los límites del plan | Semanal |
| Registros de la API | Consola o panel de registros del hosting | Sin errores repetidos ni intentos de acceso masivos | Diaria |

Los registros de la API no contienen contraseñas ni tokens. La emisión de tokens de
Power BI queda registrada con la cuenta y la fecha de vencimiento.

## 4. Respaldo

Supabase no incluye recuperación a un punto en el tiempo en el plan gratuito y recomienda
que los proyectos gratuitos exporten sus datos con regularidad y guarden copias fuera
de la plataforma. Además, los respaldos de la base **no incluyen los archivos de
Storage**: solo sus metadatos. Por eso el respaldo de AFRISAN tiene dos partes.

### 4.1 Base de datos

Con la conexión del propietario (Session pooler de Supabase) y el cliente de PostgreSQL 16
o la CLI de Supabase:

```powershell
# Opción A: CLI de Supabase
supabase db dump --db-url "$env:AFRISAN_DB_PROPIETARIO" -f "respaldo_afrisan_$(Get-Date -Format yyyyMMdd).sql" --schema afrisan
supabase db dump --db-url "$env:AFRISAN_DB_PROPIETARIO" -f "datos_afrisan_$(Get-Date -Format yyyyMMdd).sql" --data-only --schema afrisan

# Opción B: pg_dump en formato personalizado
pg_dump "$env:AFRISAN_DB_PROPIETARIO" --schema=afrisan --format=custom --file="afrisan_$(Get-Date -Format yyyyMMdd).dump"
```

`AFRISAN_DB_PROPIETARIO` es una variable de entorno de la sesión, definida solo mientras
dura el respaldo. Las contraseñas de roles personalizados (como `afrisan_api`) no viajan
en el respaldo: después de restaurar se vuelven a asignar con `Preparar-Persistencia.ps1`.

### 4.2 Fotografías

Copiar el bucket `afrisan-evidencias` a una carpeta privada (por ejemplo, con la CLI de
Supabase o el cliente S3 compatible de Storage) y comprimirla junto con el respaldo de
la base del mismo día. Cada foto tiene su hash SHA-256 registrado en la base, lo que
permite comprobar después que la copia está íntegra.

### 4.3 Política propuesta

| Elemento | Propuesta |
| --- | --- |
| Frecuencia | Semanal durante la marcha blanca; diaria cuando la operación sea regular |
| Retención | Las últimas 4 copias semanales y la de cada fin de mes durante 12 meses |
| Ubicación | Fuera de Supabase, en un almacenamiento de AFRISAN con acceso restringido y cifrado |
| Responsable | Administrador técnico; la contraparte de AFRISAN verifica que exista la copia mensual |
| Prueba | Restaurar una copia en un proyecto de prueba una vez por mes (caso CP-61 del plan de pruebas) |

## 5. Recuperación

1. **Declarar el incidente** (sección 8) y detener la API para que no entren datos nuevos.
2. **Crear un proyecto o rama de prueba en Supabase** y aplicar las migraciones 05 a 10
   en orden, o restaurar directamente el respaldo:
   `pg_restore --dbname "$env:AFRISAN_DB_DESTINO" --clean --if-exists afrisan_AAAAMMDD.dump`
3. **Restaurar las fotos** en el bucket privado del destino.
4. **Reasignar la contraseña de `afrisan_api`** con `Preparar-Persistencia.ps1` y
   configurar la API para el destino.
5. **Verificar:** `--verificar-supabase`, `--verificar-storage` y una OT de prueba de
   punta a punta. Comparar cantidades de cilindros, OT y pesajes con el último informe.
6. **Registrar** el tiempo total (RTO) y la fecha del último dato recuperado (RPO).

## 6. Secretos y su rotación

| Secreto | Dónde vive | Cuándo rotarlo | Efecto al rotar |
| --- | --- | --- | --- |
| Clave de firma JWT (`Persistencia:JwtKey`) | User Secrets o variable del hosting | Si se filtra o una vez al año | Todas las sesiones y tokens de Power BI dejan de valer; los usuarios vuelven a ingresar |
| Contraseña de `afrisan_api` | Cadena `ConnectionStrings:AfrisanRuntime` | Si se filtra o cambia el responsable | Reiniciar la API con la nueva cadena |
| Clave secreta de Storage (`Storage:SecretKey`) | User Secrets o variable del hosting | Si se filtra | Generar otra en Supabase, actualizar y verificar con `--verificar-storage` |
| Conexión del propietario | Solo en el equipo del administrador, durante mantenciones | Al cambiar el propietario | No afecta a la API |
| Token de Power BI | Parámetro del informe en Power BI | Antes de los 30 días | Descargar de nuevo el proyecto conectado o generar otro token |

Nunca subir secretos a Git. El `.gitignore` excluye `.env`, `.local/` y las carpetas
descargadas del proyecto de Power BI conectado.

## 7. Cuentas de usuario

- Las cuentas se administran en `/administracion` (ver [manual de usuario](MANUAL_USUARIO.md)).
- Al desvincularse una persona: desactivar su cuenta el mismo día. Si es técnico con
  OT abiertas, reasignar primero el trabajo.
- Si nadie puede entrar como Administrador:
  `powershell -File .\AFRISAN_NUEVO\03_CODIGO\Restablecer-Administrador.ps1`
- Revisar cada mes las cuentas activas con la contraparte de AFRISAN.

## 8. Gestión de incidentes

| Severidad | Ejemplos | Respuesta inicial |
| --- | --- | --- |
| Crítica | La API no responde, se pierden datos o hay acceso indebido | Inmediata; avisar a la contraparte y al Administrador; evaluar recuperación |
| Alta | Una función principal falla (no se pueden cerrar OT) | Mismo día hábil |
| Media | Falla parcial con alternativa | Dentro de la semana |
| Baja | Problema visual o de redacción | Próxima versión |

Pasos: registrar qué pasó, cuándo y a quién afecta; contener (por ejemplo, desactivar
una cuenta comprometida o rotar un secreto); corregir; verificar con la regresión del
plan de pruebas; e informar a la contraparte con la causa y la acción preventiva. Si el
incidente involucra datos personales, aplicar además el
[registro de tratamiento de datos](../../Fase%202/Evidencias%20Proyecto/Evidencias%20de%20documentaci%C3%B3n/11_Registro_de_Tratamiento_de_Datos_Afrisan_DM.docx).

## 9. Cambios y versiones

1. Todo cambio entra por GitHub y debe pasar la integración continua (ver [CONTRIBUTING.md](../../CONTRIBUTING.md)).
2. Las migraciones de base son aditivas, se ejecutan en una transacción y se aplican
   **antes** de publicar la API que las necesita.
3. Respaldar la base antes de cada migración en producción.
4. Registrar la versión en [CHANGELOG.md](../../CHANGELOG.md) y la decisión, si la hay,
   en el registro de decisiones.

## 10. Lista de verificación periódica

**Diaria:** salud de la API; errores en los registros.
**Semanal:** respaldo de base y fotos; uso del plan de Supabase; verificación de Storage.
**Mensual:** prueba de restauración; revisión de cuentas activas; actualización de
paquetes con `dotnet list package --vulnerable`; vigencia del token de Power BI.
