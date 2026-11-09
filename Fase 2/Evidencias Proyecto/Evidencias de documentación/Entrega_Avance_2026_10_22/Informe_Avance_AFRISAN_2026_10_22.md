# Avance del proyecto AFRISAN

Sistema Integral de Gestión y Trazabilidad de Refrigerantes



Capstone · Informe de avance · 22 de octubre de 2026



Integrante actual y responsable de continuidad: Dante Moya



## Resumen ejecutivo



El proyecto cuenta con un prototipo en .NET 8 que registra refrigerantes y cilindros, consulta inventario y permite recorrer órdenes de trabajo con entrega, reemplazo físico, fotografías de pesajes, recepción y revisión. La aplicación nueva funciona con datos temporales en memoria y borradores locales en el navegador. La persistencia en Supabase, los límites semanales y mensuales y la integración con Power BI siguen pendientes.



La principal dificultad es la diferencia entre el proceso de recarga de equipos descrito inicialmente y el reemplazo físico de cilindros aclarado durante la revisión del negocio. Este cambio afecta el MER, los criterios de retorno, los cálculos y los informes. La continuidad individual exige priorizar un flujo completo y conservar evidencia verificable de cada incremento.



Este corte actualiza la documentación con la revisión del 20 de octubre. No incorpora nuevas pruebas funcionales ni acredita aceptación del cliente. El porcentaje de avance debe determinarse con criterios acordados con el docente.



## Abstract



AFRISAN has a .NET 8 prototype for refrigerant inventory and physical cylinder replacement. It includes work orders, technical records, weighing photographs and administrative review. Server data remains in memory, while the browser retains local drafts. PostgreSQL integration, branch limits and Power BI are pending. The main challenge is aligning the previous equipment refill schema with the clarified replacement process. Individual continuity requires prioritizing one verified workflow and documenting evidence. This report updates the project status without claiming additional functional tests or production readiness.



## Contenido



2 Contexto y proceso de negocio
3 Avances y evidencias
4 Dificultades y ajustes del proyecto
5 Problemas técnicos identificados
6 Plan de trabajo y próximos incrementos
7 Calidad y módulo Power BI
8 Conclusions and personal reflection
9 Fuentes y control documental



# Contexto y proceso de negocio

## Necesidad del cliente



AFRISAN necesita conocer qué cilindros tiene, qué refrigerante contiene cada uno, dónde se encuentran, quién los manipula y qué pesajes y autorizaciones respaldan sus movimientos. El control de kilos por sucursal busca detectar excesos y permitir su revisión por Jefatura.



## Proceso aclarado durante el desarrollo



La sucursal solicita reemplazar o agregar cilindros por refrigerante y kilos. El Autorizador crea y autoriza la OT y asigna un técnico. Bodega entrega los envases; el técnico registra el retirado y el instalado, sus pesos y fotografías. Bodega recibe los retirados y los entregados sin uso. El Autorizador revisa el trabajo y realiza el cierre administrativo.



Cada cilindro contiene un único tipo de refrigerante. Una OT puede involucrar varios cilindros. El instalado permanece en la sucursal; el retirado regresa a Bodega. Un agregado no requiere inventar un cilindro retirado.



| Rol | Responsabilidad actual |

| --- | --- |

| Bodega | Registrar y consultar inventario; entregar y recibir; definir límites habilitados. |

| Técnico | Ejecutar reemplazos o agregados y registrar pesajes, fotos y observaciones. |

| Autorizador | Crear, autorizar, asignar técnico, revisar incidencias y cerrar OT. |

| Jefatura | Definir límites con prioridad y revisar antecedentes de excesos. |



Jerarquía confirmada: Bodega, Autorizador y Jefatura, de menor a mayor autoridad. Si Jefatura modifica un límite, Bodega queda bloqueada para editarlo hasta que Jefatura la habilite. Los permisos específicos del Autorizador sobre límites aún requieren definición.



## Arquitectura



Se conserva Blazor WebAssembly y ASP.NET Core Web API en .NET 8, con PostgreSQL/Supabase y EF Core como persistencia prevista. La nueva API usa stores en memoria. La Web utiliza IndexedDB para borradores y fotos pendientes. Esta separación permite sustituir la persistencia temporal por una integración durable.



# Avances y evidencias disponibles

La solución vigente para revisión es AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.slnx. La Web usa el puerto 5286 y la API el 5181. Los proyectos anteriores de la raíz permanecen como antecedentes.



| Módulo | Implementación identificada | Límite vigente |

| --- | --- | --- |

| Inventario | Catálogo, cilindros, tara, capacidad, pesaje inicial, ficha y QR. | Datos centrales en memoria. |

| Órdenes | Creación manual, detalle solicitado y autorizado y un técnico asignado. | Sucursal como texto; sin importación ERP. |

| Bodega | Entrega y recepción de envases asociadas a OT. | Custodia entre varias OT necesita corrección. |

| Técnico | Reemplazo o agregado con dos envases diferenciados cuando corresponde. | Reglas provisionales y pruebas integrales pendientes. |

| Fotografías | Colección por cilindro y pesaje; validaciones de tamaño y formato. | Servidor temporal; sin Storage durable. |

| Trabajo local | Borradores, fotos y reintentos mediante IndexedDB. | Offline y concurrencia sin validación integral. |

| Autorización | JWT, roles y restricción del técnico a sus OT. | Cuentas públicas de demostración. |

| Revisión | Incidencias, conclusión del Autorizador y cierre administrativo. | Una conclusión no modifica un pesaje confirmado. |



## Evidencia identificable



TRABAJO_TECNICO.md describe el incremento del 20 de octubre. Las capturas ACCESO_PRIMERA_ENTREGA.png, RESUMEN_API_CONECTADA.png y TECNICO_ORDENES_2026_10_20.png muestran pantallas de revisión; la última corresponde a una lista vacía de OT. No prueban un reemplazo completo.



La entrega anterior documenta compilación sin errores ni advertencias. La revisión del 20 de octubre fue estática y no ejecutó pruebas funcionales. La existencia de código, de un script SQL o de una captura se mantiene diferenciada de su validación y aceptación.



# Dificultades y ajustes del proyecto

## Continuidad individual



Dante Moya continúa como único integrante actual. El plan anterior distribuía tareas entre varios participantes y debe adaptarse a una disponibilidad informada de tres horas diarias. Las contribuciones anteriores conservan su autoría. La pauta contempla evaluación grupal y requiere acordar su aplicación individual con el docente.



## Definición del negocio y rediseño de datos



El diseño anterior asumía recarga de equipos y devolución del mismo cilindro despachado. El proceso aclarado contempla reemplazo físico. Por eso cambian las relaciones entre OT, envases, movimientos y pesajes, y debe revisarse la conciliación de retornos. La arquitectura aprobada sigue siendo válida como estructura, pero el nuevo modelo de datos necesita alineación.



## Creación y localización de tablas



Durante la preparación de Supabase aparecieron errores por esquema existente y dificultad para localizar tablas después de ejecutar scripts. También surgió un error de clave foránea al referenciar columnas sin restricción única. Se prepararon scripts y un diccionario manual de PK, FK y tipos. El SVG exportado evidencia tablas y relaciones visibles; no acredita por sí mismo triggers, políticas o conexión de la aplicación.



## Inicio de la aplicación



Se reportaron páginas que no cargaban, API apagada y diferencias entre la ejecución en Visual Studio y la aplicación abierta en el navegador. El repositorio contiene dos soluciones con puertos distintos. La nueva solución ofrece un perfil de inicio para Web y API, pero ambos procesos deben estar disponibles. El mensaje de un subproceso finalizado no demuestra por sí solo un error de la aplicación.



## Separación y documentación



Se creó AFRISAN_NUEVO para ordenar procesos, datos, código y seguimiento. La revisión intensiva quedó en una carpeta local con hallazgos verificables. Los informes con corte del 16 de octubre describían principalmente la aplicación anterior y necesitan esta actualización para coincidir con la defensa.



Ajuste de método: revisar un proceso, registrar decisiones, implementar un incremento, comprobar resultados y documentar evidencia. No se atribuyen ceremonias Scrum ni acuerdos de aceptación que no estén respaldados.



# Problemas técnicos identificados

Los siguientes escenarios provienen de la lectura del código del 20 de octubre. Su reproducción y comprobación después de corregirlos forman parte del trabajo pendiente.



| Problema | Impacto y acción siguiente |

| --- | --- |

| Borrador sin descarte | Un borrador adicional sincronizado puede impedir terminar toda la OT. Incorporar descarte trazado. |

| Custodia entre OT | El cierre consulta ubicación global del envase. Vincular cada entrega y recepción a su propio movimiento. |

| Conflicto entre pestañas | Borrar una foto puede adelantar la versión local y permitir sobrescribir pesos. Exigir versión coherente en cada modificación. |

| Entregas repetidas | Una entrega histórica puede devolver éxito sin cambiar la custodia tras una devolución. Usar identidad de movimiento. |

| Estado de la OT | La API admite entregas después del término técnico aunque la pantalla las restringe. Alinear reglas del servidor. |

| Caché y fecha offline | Pueden reaparecer OT antiguas; falta fecha física separada de sincronización. Conservar instancia y fechas distintas. |



## Brechas para una operación real



Reiniciar la API elimina inventario, OT y fotos centrales. Crear una OT puede ubicar un envase en sucursal como preparación del ejemplo. Una respuesta de rectificación registra revisión, pero no aplica un ajuste de peso. Estos comportamientos deben sustituirse antes de operar con datos reales.



El SQL no admite el nuevo rol Autorizador en su CHECK y no representa completamente el detalle solicitado/autorizado, evidencias múltiples, técnico obligatorio de OT ni límites con historial. Conectar el prototipo al SQL actual sin corregirlo trasladaría al sistema reglas incompatibles.



Los gramos autorizados se almacenan, pero todavía no generan comparación automática con los kilos reales ni con límites semanales o mensuales. Superar un límite requiere registro e investigación según el acuerdo de negocio; el bloqueo automático de ejecución no fue confirmado.



# Plan de trabajo y próximos incrementos

La disponibilidad es de tres horas diarias. La secuencia siguiente expresa prioridades y condiciones de cierre; las fechas finales se reestiman con el alcance validado. La planificación de seis horas del 20 y 21 fue una propuesta previa y no se presenta como trabajo ya ejecutado.



| Orden | Incremento | Resultado necesario |

| --- | --- | --- |

| 1 | Estabilizar el recorrido | Reemplazo completo, recepción y cierre comprobados; descarte de borradores y custodia coherentes. |

| 2 | Alinear negocio y MER | OT con técnico, detalle por gas, pesajes, pareja de envases y colección de evidencias. |

| 3 | Persistir el recorrido | Datos y fotos sobreviven al reinicio con acceso autenticado y transacciones consistentes. |

| 4 | Usuarios reales | Identidades y roles persistidos; cuentas activas y restricciones verificadas en API. |

| 5 | Límites y ajustes | Cálculo acordado, historial del límite, protección de Jefatura y revisión de excesos. |

| 6 | Módulo Power BI | Informe reproducible con fuentes, medidas, actualización y acceso definidos. |

| 7 | Validación restante | Conflictos, reintentos, offline y requisitos restantes del backlog según alcance vigente. |



## Decisiones que desbloquean el desarrollo



Definir qué kilos acumulan los límites, si se distinguen por gas y si los períodos son de calendario. Precisar la fecha del hecho que determina la semana o mes. Acordar correcciones de pesos, cierre parcial, reapertura y descarte. Definir inventario inicial en sucursal y cómo se repone un cilindro vacío.



## Criterio de avance



Una tarea queda terminada cuando cumple sus reglas, conserva datos y evidencia cuando corresponda, y dispone de resultados verificables. Las pantallas programadas no equivalen a historias aceptadas. El plan de ocho columnas actualizado acompaña este informe en Markdown.



Las integraciones con Defontana, certificados, semáforo y componentes de costos siguen en el alcance documental y requieren conciliación con el proceso revisado. El manual de usuario y la matriz formal de trazabilidad permanecen aplazados según la decisión de Dante.



# Calidad y módulo Power BI

## Validaciones prioritarias



Comprobar pesos entre tara y tara más capacidad, evidencia obligatoria de ambos pesajes en reemplazos, acceso por rol, recepción antes del cierre y comportamiento de un borrador descartado. Después ampliar a varias OT, entregas repetidas, conflictos entre pestañas y reintentos offline sin duplicación.



Cada comprobación debe conservar fecha, entorno, pasos, resultado esperado y observado y evidencia. El aislamiento entre dos técnicos necesita dos identidades de prueba; la cuenta demo única no basta para demostrarlo. Las pruebas no ejecutadas se conservan como pendientes.



## Datos necesarios para Power BI



El acta compromete Power BI y al menos cinco KPI. Los indicadores originales incluyen kilos cargados/devueltos, costos, CO₂e, criticidad y devolución. El reemplazo físico obliga a revisar el significado de algunos de ellos. El primer informe puede mostrar inventario y OT, junto con solicitado, autorizado e instalado, manteniendo pendiente la aprobación de cualquier sustitución de KPI.



Las vistas deben conservar una fila por hecho definido y relacionarse con fecha, sucursal, refrigerante y actor. No sumar etapas distintas como consumo adicional ni multiplicar kilos al unir una operación con varias fotos. Costos requieren precio histórico aplicado y conceptos acordados; emisiones requieren masa estimada, método y GWP con fuente.



## Integración y acceso



El módulo todavía no está conectado. Power BI puede consultar vistas PostgreSQL con una identidad de lectura. La presentación segura dentro de la aplicación necesita resolver autenticación, permisos, licencias y HTTPS. Las tablas de usuarios y las fotografías privadas no son necesarias para los gráficos de gestión.



## Riesgos de continuidad



El corte de revisión encontró código nuevo y EVA2 sin seguimiento Git. Su publicación permanece pendiente por instrucción de Dante. También debe registrarse una decisión de continuidad tecnológica: .NET 8 termina soporte el 10 de noviembre de 2026, según Microsoft. La tecnología del proyecto no se cambió en esta entrega.



# Conclusions and personal reflection

## Individual conclusions



AFRISAN has an implemented prototype for inventory and the physical replacement workflow. It separates warehouse delivery, technical work, photographic evidence, warehouse reception and administrative review. The current server stores operational data in memory, so durable persistence remains a major integration task.



The main engineering issue is the mismatch between the original equipment refill model and the clarified cylinder replacement process. Connecting the current application to the previous SQL schema would preserve incorrect assumptions. The next increment should align the data model and verify one complete workflow before adding further modules.



Power BI remains part of the project scope. Reliable reporting depends on stable branch and user identities, consistent event dates, historical prices and limits, and clearly defined quantities. The evidence currently available does not support a fixed completion percentage or a claim of production readiness.



## Individual reflection



Continuing this project individually requires me to make the workload visible and explain the reasons behind each technical decision. I need to keep the business meaning of each record clear, especially when distinguishing a cylinder replacement from refrigerant consumption or emissions.



My next priority is to verify the central workflow, record observed results and resolve the model inconsistencies. I also need to keep the report, backlog and code aligned so that I can defend the actual state of the work and plan the next increment within my available time.



Esta reflexión se entrega como texto propuesto para revisión personal de Dante antes de su presentación. No contiene una firma ni certifica una experiencia o resultado de prueba adicional.



# Fuentes y control documental

## Fuentes del proyecto



1. Acta de Constitución AFRISAN corregida, carpeta Documentación del Proyecto/ACTUAL. Objetivos, requisitos, reglas y aprobación histórica.
2. Product Backlog AFRISAN corregido, hojas Historias de Usuario y Criterios de Aceptación.
3. Guías 2.4 y 2.6 y pauta 2.5 de Fase 2. Estructura, monitoreo y criterios académicos.
4. AFRISAN_NUEVO/01_NEGOCIO, especialmente definición de OT, responsables, técnico, Autorizador y Power BI.
5. AFRISAN_NUEVO/03_CODIGO/TRABAJO_TECNICO.md y código de API, Web y contratos.
6. Revisión del 20/10: INFORME_GENERAL.md, CODIGO.md, DATOS_Y_NEGOCIO.md y CAPSTONE.md. Hallazgos con rutas, líneas, párrafos y celdas.



## Referencias técnicas



Microsoft. Política de soporte de .NET. https://dotnet.microsoft.com/en-us/platform/support/policy
Microsoft Learn. Secure embed y esquema de estrella para Power BI. https://learn.microsoft.com/en-us/power-bi/collaborate-share/service-embed-secure y https://learn.microsoft.com/en-us/power-bi/guidance/star-schema
Supabase. Conexión PostgreSQL y buckets privados. https://supabase.com/docs/guides/database/connecting-to-postgres y https://supabase.com/docs/guides/storage/buckets/fundamentals
Consulta técnica registrada: 20 de octubre de 2026.



## Control de esta actualización



Corte documental: 22 de octubre de 2026. Base de evidencia: implementación y revisión del 20/10 y documentos anteriores. La edición actualiza el relato, la planificación y el material de defensa; no modifica código, SQL ni datos en nube. Las fuentes históricas se conservan y las versiones anteriores de los documentos actualizados quedan en ANTECEDENTES.



Responsable actual: Dante Moya. Publicación pendiente por instrucción del estudiante. El tratamiento individual de la pauta y la aceptación del proceso revisado requieren constancia del docente y del cliente respectivamente.

