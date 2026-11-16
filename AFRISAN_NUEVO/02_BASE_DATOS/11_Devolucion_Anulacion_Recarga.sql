-- Actualización aditiva. Ejecutar después de 10. Corrige las incongruencias del recorrido del 14/11/2026:
--   D-39  Bodega devuelve la OT al técnico con un motivo, en vez de aprobarla con errores.
--   D-40  El Autorizador anula una OT que todavía no tiene retiros ni trabajos registrados.
--   D-41  Bodega no aprueba con incidencias o rectificaciones pendientes; las incidencias se
--         responden en cualquier etapa abierta.
--   D-42  El peso instalado no puede diferir en más de 200 g del peso de salida de Bodega, y un
--         cilindro retirado no puede pesar más que su último peso registrado.
--   D-43  El cilindro retirado de una sucursal vuelve a Bodega como «Vacio» (pendiente de
--         recarga); Bodega lo reingresa con un pesaje de recarga y foto.
-- No modifica registros existentes.
BEGIN;

-- 1. Estados y columnas nuevas de la OT
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "MotivoDevolucion" text;
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "MotivoAnulacion" text;
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "FechaAnulacion" timestamptz;
DO $$ DECLARE c text; BEGIN
  FOR c IN SELECT conname FROM pg_constraint WHERE conrelid='afrisan."OrdenTrabajo"'::regclass AND contype='c'
      AND pg_get_constraintdef(oid) LIKE '%''PendienteRevision''%' AND pg_get_constraintdef(oid) NOT LIKE '%FechaTerminoTecnico%' LOOP
    EXECUTE format('ALTER TABLE afrisan."OrdenTrabajo" DROP CONSTRAINT %I',c);
  END LOOP;
  -- Tipos de pesaje y la regla «solo el inicial no pertenece a una OT», que ahora incluye la recarga.
  FOR c IN SELECT conname FROM pg_constraint WHERE conrelid='afrisan."Pesaje"'::regclass AND contype='c'
      AND (pg_get_constraintdef(oid) LIKE '%''Recepcion''%' OR pg_get_constraintdef(oid) LIKE '%Id_OrdenTrabajo%') LOOP
    EXECUTE format('ALTER TABLE afrisan."Pesaje" DROP CONSTRAINT %I',c);
  END LOOP;
END $$;
ALTER TABLE afrisan."Pesaje" DROP CONSTRAINT IF EXISTS "Pesaje_Origen_check";
ALTER TABLE afrisan."Pesaje" ADD CONSTRAINT "Pesaje_Origen_check"
  CHECK (("Tipo" IN ('Inicial','Recarga'))=("Id_OrdenTrabajo" IS NULL));
ALTER TABLE afrisan."OrdenTrabajo" DROP CONSTRAINT IF EXISTS "OrdenTrabajo_Estado_check";
ALTER TABLE afrisan."OrdenTrabajo" ADD CONSTRAINT "OrdenTrabajo_Estado_check"
  CHECK ("Estado" IN ('Asignada','EnProcesoDespacho','EnTerreno','PendienteBodega','PendienteRevision','Cerrada','Anulada'));
ALTER TABLE afrisan."OrdenTrabajo" DROP CONSTRAINT IF EXISTS "OrdenTrabajo_Anulacion_check";
ALTER TABLE afrisan."OrdenTrabajo" ADD CONSTRAINT "OrdenTrabajo_Anulacion_check"
  CHECK ((("Estado"='Anulada')=("FechaAnulacion" IS NOT NULL)) AND ("Estado"<>'Anulada' OR length(btrim("MotivoAnulacion"))>=5));
ALTER TABLE afrisan."Pesaje" DROP CONSTRAINT IF EXISTS "Pesaje_Tipo_check";
ALTER TABLE afrisan."Pesaje" ADD CONSTRAINT "Pesaje_Tipo_check"
  CHECK ("Tipo" IN ('Inicial','Despacho','Retirado','Instalado','Recepcion','Recarga'));

-- 2. Transiciones de la OT: devolución (Bodega) y anulación (Autorizador)
CREATE OR REPLACE FUNCTION afrisan.ot_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE a integer; devolucion boolean; anulacion boolean;
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar OT'; END IF;
  IF TG_OP='INSERT' THEN
    a:=afrisan.actor(ARRAY['Autorizador']); NEW."Id_Autorizador":=a; NEW."FechaCreacion":=clock_timestamp();
    IF NEW."Estado"<>'Asignada' OR NEW."FechaTerminoTecnico" IS NOT NULL THEN RAISE EXCEPTION 'Crear OT asignada, no cerrada'; END IF;
    IF NEW."MotivoDevolucion" IS NOT NULL OR NEW."MotivoAnulacion" IS NOT NULL OR NEW."FechaAnulacion" IS NOT NULL THEN RAISE EXCEPTION 'Crear OT sin devolución ni anulación'; END IF;
    IF NOT EXISTS(SELECT 1 FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING("Id_Rol") WHERE u."Id_Usuario"=NEW."Id_Tecnico" AND u."Activo" AND r."Nombre_Rol"='Tecnico') THEN RAISE EXCEPTION 'Técnico asignado inválido'; END IF;
    RETURN NEW;
  END IF;
  IF (to_jsonb(NEW)-ARRAY['Estado','FechaTerminoTecnico','ObservacionTermino','FechaCierre','MotivoDevolucion','MotivoAnulacion','FechaAnulacion'])
     <>(to_jsonb(OLD)-ARRAY['Estado','FechaTerminoTecnico','ObservacionTermino','FechaCierre','MotivoDevolucion','MotivoAnulacion','FechaAnulacion']) THEN RAISE EXCEPTION 'No reescribir OT autorizada'; END IF;
  IF NEW."Estado"=OLD."Estado" THEN RAISE EXCEPTION 'Sin transición válida'; END IF;
  devolucion:=OLD."Estado"='PendienteBodega' AND NEW."Estado"='EnTerreno';
  anulacion:=OLD."Estado"='Asignada' AND NEW."Estado"='Anulada';
  IF NOT devolucion AND NEW."MotivoDevolucion" IS DISTINCT FROM OLD."MotivoDevolucion" THEN RAISE EXCEPTION 'El motivo de devolución solo se registra al devolver la OT'; END IF;
  IF NOT anulacion AND (NEW."MotivoAnulacion",NEW."FechaAnulacion") IS DISTINCT FROM (OLD."MotivoAnulacion",OLD."FechaAnulacion") THEN RAISE EXCEPTION 'La anulación solo se registra al anular la OT'; END IF;
  IF NEW."Estado"='EnProcesoDespacho' AND OLD."Estado"='Asignada' THEN
    a:=afrisan.actor(ARRAY['Tecnico']);
    IF NOT EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'Falta despacho'; END IF;
  ELSIF NEW."Estado"='EnTerreno' AND OLD."Estado" IN ('EnProcesoDespacho','Asignada') THEN
    a:=afrisan.actor(ARRAY['Tecnico']);
    IF NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Registrada') THEN RAISE EXCEPTION 'Falta cambio confirmado'; END IF;
  ELSIF devolucion THEN
    a:=afrisan.actor(ARRAY['Bodega']);
    IF EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'La OT ya fue aprobada por Bodega'; END IF;
    IF length(btrim(coalesce(NEW."MotivoDevolucion",'')))<5 THEN RAISE EXCEPTION 'Explica por qué se devuelve la OT al técnico'; END IF;
  ELSIF anulacion THEN
    a:=afrisan.actor(ARRAY['Autorizador']);
    IF EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo") OR EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo") THEN
      RAISE EXCEPTION 'Solo se anula una OT sin retiros de Bodega ni trabajos registrados'; END IF;
    IF length(btrim(coalesce(NEW."MotivoAnulacion",'')))<5 THEN RAISE EXCEPTION 'Indica el motivo de la anulación'; END IF;
    NEW."FechaAnulacion":=clock_timestamp();
  ELSIF NEW."Estado"='PendienteBodega' AND OLD."Estado"='EnTerreno' THEN
    a:=afrisan.actor(ARRAY['Tecnico']);
    IF EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Borrador') THEN RAISE EXCEPTION 'Resolver borradores antes de terminar'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan."RetiroPrevisto" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Reservado') AND coalesce(btrim(NEW."ObservacionTermino"),'')='' THEN RAISE EXCEPTION 'Explicar ejecución parcial'; END IF;
    NEW."FechaTerminoTecnico":=clock_timestamp();
  ELSIF NEW."Estado"='PendienteRevision' AND OLD."Estado"='PendienteBodega' THEN
    a:=afrisan.actor(ARRAY['Bodega']);
    IF NOT EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Etapa"='Bodega') THEN RAISE EXCEPTION 'Falta aprobación Bodega'; END IF;
  ELSIF NEW."Estado"='Cerrada' AND OLD."Estado"='PendienteRevision' THEN
    a:=afrisan.actor(ARRAY['Autorizador']); PERFORM afrisan.sin_pendientes(OLD."Id_OrdenTrabajo");
    IF (SELECT count(*) FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo")<>2 OR EXISTS(SELECT 1 FROM afrisan."Incidencia" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Respuesta" IS NULL) THEN RAISE EXCEPTION 'Faltan revisiones/gestión de incidencias'; END IF;
    NEW."FechaCierre":=clock_timestamp();
  ELSE RAISE EXCEPTION 'Transición de OT no permitida'; END IF;
  IF NEW."Estado" IN ('EnProcesoDespacho','EnTerreno','PendienteBodega') AND NOT devolucion AND a<>OLD."Id_Tecnico" THEN RAISE EXCEPTION 'OT de otro técnico'; END IF;
  IF NEW."Estado"<>'PendienteBodega' AND (NEW."FechaTerminoTecnico",NEW."ObservacionTermino") IS DISTINCT FROM (OLD."FechaTerminoTecnico",OLD."ObservacionTermino") THEN RAISE EXCEPTION 'No sobrescribir informe técnico'; END IF;
  RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION afrisan.ot_cierre_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NEW."Estado" IN ('Cerrada','Anulada') AND OLD."Estado" NOT IN ('Cerrada','Anulada') THEN
    UPDATE afrisan."RetiroPrevisto" SET "Estado"='NoEjecutado' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Reservado';
  END IF;
  IF NEW."Estado"='Cerrada' AND OLD."Estado"<>'Cerrada' THEN
    INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario") VALUES(NEW."Id_OrdenTrabajo",'Cierre administrativo',afrisan.actor(ARRAY['Autorizador']));
  ELSIF NEW."Estado"='Anulada' AND OLD."Estado"<>'Anulada' THEN
    INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'OT anulada',afrisan.actor(ARRAY['Autorizador']),NEW."MotivoAnulacion");
  ELSIF OLD."Estado"='PendienteBodega' AND NEW."Estado"='EnTerreno' THEN
    INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Devuelta al técnico por Bodega',afrisan.actor(ARRAY['Bodega']),NEW."MotivoDevolucion");
  ELSIF OLD."Estado"='EnTerreno' AND NEW."Estado"='PendienteBodega' THEN
    INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Enviada a revisión de Bodega',NEW."Id_Tecnico",NEW."ObservacionTermino");
  END IF;
  RETURN NULL;
END $$;

-- La reserva de un cilindro también se libera al anular la OT.
CREATE OR REPLACE FUNCTION afrisan.pedido_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; c afrisan."Cilindros";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar el pedido; requiere procedimiento de anulación'; END IF;
  IF TG_TABLE_NAME='RetiroPrevisto' AND TG_OP='UPDATE' AND pg_trigger_depth()>1 THEN
    IF OLD."Estado"<>'Reservado' OR NEW."Estado" NOT IN ('Ejecutado','NoEjecutado') OR
       (NEW."Id_OrdenTrabajo",NEW."Id_Cilindro") IS DISTINCT FROM (OLD."Id_OrdenTrabajo",OLD."Id_Cilindro") THEN RAISE EXCEPTION 'Transición interna de reserva inválida'; END IF;
    IF NEW."Estado"='NoEjecutado' AND NOT EXISTS(SELECT 1 FROM afrisan."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado" IN ('Cerrada','Anulada')) THEN RAISE EXCEPTION 'Solo liberar un retiro no ejecutado al cerrar o anular'; END IF;
    RETURN NEW;
  END IF;
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Autorizador']);
  IF o."Estado"<>'Asignada' OR TG_OP='UPDATE' THEN RAISE EXCEPTION 'Pedido autorizado inmutable; agregar detalle solo antes del despacho'; END IF;
  IF TG_TABLE_NAME='RetiroPrevisto' THEN
    SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
    IF c."Ubicacion"<>'Sucursal' OR c."Id_Sucursal"<>o."Id_Sucursal" OR NOT EXISTS(SELECT 1 FROM afrisan."DetalleOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Refrigerante"=c."Id_Refrigerante") THEN RAISE EXCEPTION 'Retiro no corresponde a sucursal/gas encargado'; END IF;
  END IF;
  RETURN NEW;
END $$;

-- 3. Bodega tampoco aprueba con incidencias o rectificaciones pendientes (D-41)
CREATE OR REPLACE FUNCTION afrisan.revision_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo";
BEGIN
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY[NEW."Etapa"]);
  PERFORM afrisan.sin_pendientes(o."Id_OrdenTrabajo");
  IF NEW."Etapa"='Bodega' AND o."Estado"<>'PendienteBodega' THEN RAISE EXCEPTION 'Primero término técnico'; END IF;
  IF NEW."Etapa"='Autorizador' AND (o."Estado"<>'PendienteRevision' OR NOT EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Etapa"='Bodega')) THEN RAISE EXCEPTION 'Primero aprobación de Bodega'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan."Incidencia" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Respuesta" IS NULL) THEN RAISE EXCEPTION 'Incidencias sin revisar'; END IF;
  NEW."Id_Usuario":=afrisan.actor(ARRAY[NEW."Etapa"]); NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS "Revision_Rectificaciones" ON afrisan."RevisionOT";
CREATE TRIGGER "Revision_Rectificaciones" BEFORE INSERT ON afrisan."RevisionOT" FOR EACH ROW EXECUTE FUNCTION afrisan.rectificacion_pendiente();

-- Las incidencias se responden en cualquier etapa abierta, para no trabar la aprobación de Bodega.
CREATE OR REPLACE FUNCTION afrisan.incidencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar incidencias'; END IF;
  IF TG_OP='INSERT' THEN
    o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico','Bodega']);
    IF o."Estado" IN ('Cerrada','Anulada') OR EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") OR NEW."Respuesta" IS NOT NULL OR NEW."Id_Revisor" IS NOT NULL OR NEW."FechaRevision" IS NOT NULL THEN RAISE EXCEPTION 'Incidencia fuera de etapa'; END IF;
    IF NEW."Id_Operacion" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_Operacion"=NEW."Id_Operacion" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'Operación de otra OT'; END IF;
    NEW."Id_Usuario":=afrisan.actor(ARRAY['Tecnico','Bodega']); NEW."Fecha":=clock_timestamp();
  ELSE
    o:=afrisan.ot_actor(OLD."Id_OrdenTrabajo",ARRAY['Autorizador']);
    IF o."Estado" IN ('Cerrada','Anulada') OR OLD."Respuesta" IS NOT NULL OR NEW."Respuesta" IS NULL OR
      (to_jsonb(NEW)-ARRAY['Respuesta','Id_Revisor','FechaRevision'])<>(to_jsonb(OLD)-ARRAY['Respuesta','Id_Revisor','FechaRevision']) THEN RAISE EXCEPTION 'Solo revisar una incidencia pendiente de una OT abierta'; END IF;
    NEW."Id_Revisor":=afrisan.actor(ARRAY['Autorizador']); NEW."FechaRevision":=clock_timestamp();
  END IF; RETURN NEW;
END $$;

-- 4. Tolerancia de peso entre la salida de Bodega y la instalación (D-42)
CREATE OR REPLACE FUNCTION afrisan.operacion_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; n afrisan."Cilindros"; v afrisan."Cilindros"; p afrisan."Pesaje"; salida bigint; ultimo bigint;
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar operaciones; descartar borrador o solicitar rectificación'; END IF;
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico']);
  IF TG_OP='INSERT' THEN
    IF o."Estado" NOT IN ('Asignada','EnProcesoDespacho','EnTerreno') OR NEW."Estado"<>'Borrador' OR NEW."FechaEntregaInformada" IS NOT NULL THEN RAISE EXCEPTION 'Crear primero borrador editable'; END IF;
    NEW."Id_Tecnico":=o."Id_Tecnico"; NEW."Version":=1; NEW."FechaRecepcion":=clock_timestamp(); NEW."FechaConfirmacion":=NULL; RETURN NEW;
  END IF;
  IF NEW."Id_OrdenTrabajo"<>OLD."Id_OrdenTrabajo" OR NEW."Id_Operacion"<>OLD."Id_Operacion" OR NEW."Id_Tecnico"<>OLD."Id_Tecnico" OR NEW."Version"<>OLD."Version"+1 THEN RAISE EXCEPTION 'Identidad o versión de operación inválida'; END IF;
  IF NEW."FechaRecepcion" IS DISTINCT FROM OLD."FechaRecepcion" THEN RAISE EXCEPTION 'No sobrescribir la recepción del borrador'; END IF;
  IF OLD."Estado"='Registrada' THEN
    IF OLD."Tipo"<>'Reemplazo' OR OLD."FechaEntregaInformada" IS NOT NULL OR NEW."FechaEntregaInformada" IS NULL OR o."Estado" NOT IN ('EnTerreno','PendienteBodega') OR
       (to_jsonb(NEW)-ARRAY['FechaEntregaInformada','Id_EntregaInformadaPor','Version'])<>(to_jsonb(OLD)-ARRAY['FechaEntregaInformada','Id_EntregaInformadaPor','Version']) THEN RAISE EXCEPTION 'Trabajo confirmado no se sobrescribe'; END IF;
    NEW."Id_EntregaInformadaPor":=o."Id_Tecnico"; NEW."FechaEntregaInformada":=clock_timestamp(); RETURN NEW;
  END IF;
  IF OLD."Estado"='Descartada' OR o."Estado" NOT IN ('Asignada','EnProcesoDespacho','EnTerreno') THEN RAISE EXCEPTION 'Operación no editable'; END IF;
  IF NEW."FechaEntregaInformada" IS NOT NULL OR NEW."Id_EntregaInformadaPor" IS NOT NULL THEN RAISE EXCEPTION 'La devolución se informa después de confirmar el cambio'; END IF;
  IF NEW."Estado"<>'Registrada' THEN RETURN NEW; END IF;
  IF NEW."Id_Instalado" IS NULL OR NEW."Id_Despacho" IS NULL OR NEW."Id_PesajeInstalado" IS NULL THEN RAISE EXCEPTION 'Falta cilindro/entrega/pesaje instalado'; END IF;
  PERFORM 1 FROM afrisan."Cilindros" WHERE "Id_Cilindro" IN (NEW."Id_Instalado",NEW."Id_Retirado") ORDER BY "Id_Cilindro" FOR UPDATE;
  SELECT * INTO STRICT n FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  IF NOT EXISTS(SELECT 1 FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=n."Id_Cilindro" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Despacho"=NEW."Id_Despacho") THEN RAISE EXCEPTION 'Instalado fuera de custodia de esta OT'; END IF;
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_PesajeInstalado";
  IF p."Tipo"<>'Instalado' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>n."Id_Cilindro" THEN RAISE EXCEPTION 'Pesaje instalado incompatible'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del instalado'; END IF;
  SELECT pv."PesoBrutoGr" INTO STRICT salida FROM afrisan."Despacho" d JOIN afrisan."PesajeVigente" pv ON pv."Id_Pesaje"=d."Id_Pesaje" WHERE d."Id_Despacho"=NEW."Id_Despacho";
  IF abs(p."PesoBrutoGr"-salida)>200 THEN
    RAISE EXCEPTION 'El peso instalado (% g) difiere en más de 200 g del peso de salida de Bodega (% g). Vuelve a pesar el cilindro; si la diferencia es real, devuélvelo a Bodega sin usar', p."PesoBrutoGr", salida;
  END IF;
  IF NEW."Tipo"='Reemplazo' THEN
    IF NEW."Id_Retirado" IS NULL OR NEW."Id_PesajeRetirado" IS NULL THEN RAISE EXCEPTION 'Falta cilindro/pesaje retirado'; END IF;
    SELECT * INTO STRICT v FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Retirado";
    IF v."Ubicacion"<>'Sucursal' OR v."Id_Sucursal"<>o."Id_Sucursal" OR v."Id_Refrigerante"<>n."Id_Refrigerante" OR NOT EXISTS(SELECT 1 FROM afrisan."RetiroPrevisto" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Cilindro"=v."Id_Cilindro" AND "Estado"='Reservado') THEN RAISE EXCEPTION 'Retirado de otra sucursal, gas o pedido'; END IF;
    SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_PesajeRetirado";
    IF p."Tipo"<>'Retirado' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>v."Id_Cilindro" THEN RAISE EXCEPTION 'Pesaje retirado incompatible'; END IF;
    IF NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del retirado'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" a JOIN afrisan."EvidenciaPesaje" b USING("SHA256") WHERE a."Id_Pesaje"=NEW."Id_PesajeRetirado" AND b."Id_Pesaje"=NEW."Id_PesajeInstalado") THEN RAISE EXCEPTION 'No reutilizar la misma foto para ambos cilindros'; END IF;
    IF v."Id_UltimoPesaje" IS NOT NULL THEN
      SELECT pv."PesoBrutoGr" INTO STRICT ultimo FROM afrisan."PesajeVigente" pv WHERE pv."Id_Pesaje"=v."Id_UltimoPesaje";
      IF p."PesoBrutoGr">ultimo+200 THEN
        RAISE EXCEPTION 'El cilindro retirado pesa % g, más que su último peso registrado (% g). Un cilindro en uso no gana gas: vuelve a pesarlo', p."PesoBrutoGr", ultimo;
      END IF;
    END IF;
  END IF;
  NEW."FechaConfirmacion":=clock_timestamp(); RETURN NEW;
END $$;

-- 5. El retirado vuelve a Bodega como Vacio (pendiente de recarga) (D-43)
CREATE OR REPLACE FUNCTION afrisan.recepcion_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  DELETE FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  UPDATE afrisan."Cilindros" c SET "Ubicacion"='Bodega',"Id_Sucursal"=NULL,"Id_Tecnico"=NULL,"Id_UltimoPesaje"=NEW."Id_Pesaje",
    "Estado"=CASE WHEN NEW."Id_Operacion" IS NOT NULL OR p."PesoBrutoGr"=c."TaraGr" THEN 'Vacio' ELSE 'Disponible' END
    FROM afrisan."Pesaje" p WHERE c."Id_Cilindro"=NEW."Id_Cilindro" AND p."Id_Pesaje"=NEW."Id_Pesaje";
  IF NEW."Id_DespachoSinUso" IS NOT NULL THEN UPDATE afrisan."Despacho" SET "Estado"='Devuelto' WHERE "Id_Despacho"=NEW."Id_DespachoSinUso"; END IF;
  INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Recepción oficial Bodega',NEW."Id_Bodeguero",NEW."Id_Cilindro"::text);
  RETURN NULL;
END $$;

-- Recarga: Bodega reingresa un cilindro de Bodega con su nuevo peso y foto (D-43)
CREATE OR REPLACE FUNCTION afrisan.pesaje_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE c afrisan."Cilindros"; o afrisan."OrdenTrabajo";
BEGIN
  IF NEW."Tipo" IN ('Inicial','Recarga') THEN
    NEW."Id_Usuario":=afrisan.actor(ARRAY['Bodega','Administrador']);
    IF NEW."Tipo"='Recarga' AND NEW."Id_OrdenTrabajo" IS NOT NULL THEN RAISE EXCEPTION 'La recarga no pertenece a una OT'; END IF;
  ELSIF NEW."Tipo"='Recepcion' THEN o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Bodega']); NEW."Id_Usuario":=afrisan.actor(ARRAY['Bodega']);
  ELSE o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico'],true); NEW."Id_Usuario":=o."Id_Tecnico"; END IF;
  SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  PERFORM afrisan.peso_valido(c."Id_Cilindro",NEW."PesoBrutoGr");
  IF NEW."Tipo"='Inicial' AND (c."Id_UltimoPesaje" IS NOT NULL OR EXISTS(SELECT 1 FROM afrisan."Pesaje" WHERE "Id_Cilindro"=c."Id_Cilindro")) THEN RAISE EXCEPTION 'Ya existe pesaje inicial u operativo'; END IF;
  IF NEW."Tipo"='Recarga' AND (c."Ubicacion"<>'Bodega' OR c."Estado" NOT IN ('Vacio','Disponible') OR NEW."PesoBrutoGr"<=c."TaraGr") THEN
    RAISE EXCEPTION 'Solo se recarga un cilindro en Bodega, vacío o disponible, y con gas'; END IF;
  IF NEW."Tipo"='Recepcion' AND (o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo")) THEN RAISE EXCEPTION 'Recepción ya revisada/cerrada'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION afrisan.pesaje_inicial_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NEW."Tipo"='Inicial' THEN
    UPDATE afrisan."Cilindros" SET "Id_UltimoPesaje"=NEW."Id_Pesaje", "Estado"=CASE WHEN "Ubicacion"='Sucursal' THEN 'Instalado' WHEN NEW."PesoBrutoGr"="TaraGr" THEN 'Vacio' ELSE 'Disponible' END WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  ELSIF NEW."Tipo"='Recarga' THEN
    UPDATE afrisan."Cilindros" SET "Id_UltimoPesaje"=NEW."Id_Pesaje", "Estado"='Disponible' WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  END IF; RETURN NULL;
END $$;

CREATE OR REPLACE FUNCTION afrisan.evidencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE p afrisan."Pesaje"; o afrisan."OrdenTrabajo";
BEGIN
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF p."Id_OrdenTrabajo" IS NOT NULL THEN
    o:=afrisan.ot_actor(p."Id_OrdenTrabajo",CASE WHEN p."Tipo"='Recepcion' THEN ARRAY['Bodega'] ELSE ARRAY['Tecnico'] END,p."Tipo"<>'Recepcion');
    IF EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=p."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'No se agregan fotos después de la revisión'; END IF;
  END IF;
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje" FOR UPDATE;
  NEW."Id_Usuario":=afrisan.actor(CASE WHEN p."Tipo" IN ('Inicial','Recepcion','Recarga') THEN ARRAY['Bodega','Administrador'] ELSE ARRAY['Tecnico'] END);
  IF p."Id_Usuario"<>NEW."Id_Usuario" THEN RAISE EXCEPTION 'La evidencia pertenece a otro registrador'; END IF;
  IF (SELECT count(*) FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje")>=8 THEN RAISE EXCEPTION 'Máximo ocho fotos por pesaje'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_Pesaje"=p."Id_Pesaje") OR EXISTS(SELECT 1 FROM afrisan."RecepcionBodega" WHERE "Id_Pesaje"=p."Id_Pesaje") OR
     EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Estado"='Registrada' AND p."Id_Pesaje" IN ("Id_PesajeRetirado","Id_PesajeInstalado")) THEN RAISE EXCEPTION 'No se agregan fotos a pesajes confirmados'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;

REVOKE ALL ON FUNCTION afrisan.ot_validar(),afrisan.ot_cierre_aplicar(),afrisan.pedido_validar(),afrisan.revision_validar(),
  afrisan.incidencia_validar(),afrisan.operacion_validar(),afrisan.recepcion_aplicar(),afrisan.pesaje_validar(),
  afrisan.pesaje_inicial_aplicar(),afrisan.evidencia_validar() FROM PUBLIC;
COMMIT;
