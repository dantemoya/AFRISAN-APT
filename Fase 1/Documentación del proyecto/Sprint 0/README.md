# Sprint 0 · Preparación e Insumos de Diseño

Material de base para el desarrollo iterativo del proyecto. Los mockups y el diagrama de
componentes son los originales de la Fase 1; el diagrama de despliegue y el MER están
actualizados al contexto de **AFRISAN** (`AFRISAN_NUEVO`, corte 02/11/2026). La arquitectura
vigente está en [`../Arquitectura`](../Arquitectura/README.md).

| Elemento requerido | Ubicación en el repositorio | Descripción vigente |
| --- | --- | --- |
| **Primer backlog (historias de usuario priorizadas)** | [`01_Product_Backlog_Afrisan_DM.xlsx`](../01_Product_Backlog_Afrisan_DM.xlsx) | 10 historias de usuario (`HU-01` a `HU-10`) priorizadas y alineadas con el proceso de reemplazo físico de cilindros y pesajes en gramos. |
| **Diagrama de componentes** | [`Diagramas/AFRISAN_05_DIAGRAMA_COMPONENTES.png`](Diagramas/AFRISAN_05_DIAGRAMA_COMPONENTES.png) | Versión preliminar de la Fase 1 (Figura 1 del informe original): aplicación móvil .NET MAUI, aplicación web Blazor WebAssembly, API REST ASP.NET Core, PostgreSQL 16 en Supabase y Power BI. La aplicación móvil se reemplazó después por la web adaptable al celular. |
| **Diagrama de despliegue** | [`Diagramas/AFRISAN_04_DIAGRAMA_DESPLIEGUE.png`](Diagramas/AFRISAN_04_DIAGRAMA_DESPLIEGUE.png) | Nodos de ejecución en navegador web (PC/móvil con IndexedDB), servidor de aplicación `.NET 8`, Supabase Cloud (PostgreSQL + Storage) y Power BI. |
| **Diagrama Entidad-Relación (MER)** | [`Modelo de datos/MER - SISTEMA AFRISAN.png`](<Modelo de datos/MER - SISTEMA AFRISAN.png>) | Modelo relacional del esquema único `afrisan` (30 tablas y 8 vistas operativas sin emisiones). |
| **Mockups / Pantallas iniciales** | [`Mockups/`](Mockups/README.md) | Diseños de Figma de la Fase 1: aplicación móvil del técnico y panel web administrativo. |
