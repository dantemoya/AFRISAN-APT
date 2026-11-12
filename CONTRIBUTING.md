# Guía de contribución

Reglas para modificar el código y la documentación de AFRISAN sin romper lo que ya
funciona. Se aplican a cualquier persona que continúe el proyecto.

El código es propiedad de su autor, con todos los derechos reservados ([LICENSE](LICENSE)).
Para modificarlo o usarlo se necesita su autorización por escrito.

## 1. Preparar el entorno

- .NET SDK 8 (la integración continua usa también el SDK 10 para abrir `AFRISAN.slnx`).
- Visual Studio 2022 o `dotnet` en la terminal.
- Para trabajar con la base real: los secretos en User Secrets según
  [CONEXION_SUPABASE.md](AFRISAN_NUEVO/03_CODIGO/CONEXION_SUPABASE.md). Sin ellos, la API
  puede arrancar en modo memoria solo en Development.

```powershell
dotnet build AFRISAN.slnx
dotnet test AFRISAN.slnx
.\AFRISAN_NUEVO\03_CODIGO\Iniciar-Revision.ps1
```

## 2. Flujo de trabajo con Git

1. Partir de `main` actualizado y crear una rama por cambio: `feat/…`, `fix/…`, `docs/…`.
2. Hacer commits pequeños, uno por tema, con el formato de
   [Conventional Commits](https://www.conventionalcommits.org/es/v1.0.0/), que es el que
   ya usa el historial:
   - `feat(powerbi): token de 30 días para conectar Power BI a la API`
   - `fix(api): Preparar-Persistencia no reaplica la contraseña de afrisan_api`
   - Tipos usados: `feat`, `fix`, `perf`, `docs`, `test`, `build`, `chore`.
3. Abrir un pull request hacia `main`. La integración continua debe pasar antes de unir.
4. Agregar el cambio en [CHANGELOG.md](CHANGELOG.md) bajo **Sin publicar**.

## 3. Reglas del código

- **Las reglas de negocio se validan en la API y en la base de datos**, no solo en la
  pantalla. Un cambio de flujo actualiza también los disparadores y las pruebas.
- **Los contratos compartidos** (`Afrisan.Nuevo.Contratos`) cambian de forma compatible:
  agregar campos opcionales antes que renombrar o quitar.
- **Masa en gramos enteros**; los kilos solo se muestran.
- **Escrituras reintentables** llevan un UUID del cliente; las ediciones llevan `version`.
- **Mensajes al usuario en español**, sin exponer detalles internos ni datos personales.
- Consultas siempre parametrizadas; nada de concatenar SQL con datos del usuario.

## 4. Base de datos

- Las migraciones se numeran (`11_…sql`), son **aditivas**, corren en una transacción y
  pueden repetirse sin daño.
- Toda tabla nueva tiene RLS y permisos explícitos para `afrisan_api`.
- Después de una migración, regenerar el
  [diccionario de datos](AFRISAN_NUEVO/02_BASE_DATOS/DICCIONARIO_ESQUEMA_AFRISAN.md) y
  respaldar la base antes de aplicarla en producción.

## 5. Pruebas

- Todo cambio de regla agrega o ajusta pruebas xUnit en `Afrisan.Nuevo.Pruebas`.
- Los cambios de flujo completo se verifican también con la batería correspondiente de
  `AFRISAN_NUEVO/04_SEGUIMIENTO` y se registran en la
  [matriz de casos](Fase%202/Evidencias%20Proyecto/Evidencias%20de%20documentaci%C3%B3n/07_Matriz_de_Casos_de_Prueba_Afrisan_DM.xlsx).
- Los datos de prueba son sintéticos; nunca se usan datos reales de AFRISAN en pruebas
  automatizadas.

## 6. Seguridad

- Nunca subir contraseñas, claves, cadenas de conexión ni tokens. Revisar `git diff`
  antes de cada commit.
- Si un secreto llega a GitHub por error, rotarlo de inmediato según el
  [manual de operación](AFRISAN_NUEVO/03_CODIGO/MANUAL_OPERACION.md); borrar el commit
  no basta.
- Revisar dependencias vulnerables con `dotnet list package --vulnerable`.

## 7. Documentación

- Cada cambio visible para el usuario actualiza el [manual de usuario](AFRISAN_NUEVO/03_CODIGO/MANUAL_USUARIO.md).
- Cada endpoint nuevo se agrega en [API.md](AFRISAN_NUEVO/03_CODIGO/API.md).
- Las decisiones de negocio se registran en
  [DECISIONES_Y_PENDIENTES.md](AFRISAN_NUEVO/04_SEGUIMIENTO/DECISIONES_Y_PENDIENTES.md).
- El índice general está en [MAPA_DOCUMENTAL.md](MAPA_DOCUMENTAL.md).
