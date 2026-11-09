-- Actualización aditiva. Ejecutar después de 07; no modifica pesajes ni revisiones originales.
BEGIN;
CREATE TABLE IF NOT EXISTS afrisan."RectificacionPesaje" (
  "Id" uuid PRIMARY KEY,
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Id_Pesaje" uuid NOT NULL REFERENCES afrisan."Pesaje",
  "PesoAnteriorGr" bigint NOT NULL,
  "PesoPropuestoGr" bigint NOT NULL CHECK ("PesoPropuestoGr">=0),
  "Motivo" text NOT NULL CHECK (length(btrim("Motivo")) BETWEEN 5 AND 2000),
  "Evidencia" jsonb NOT NULL CHECK (jsonb_typeof("Evidencia")='object'),
  "Id_Solicitante" integer NOT NULL REFERENCES afrisan."Usuario",
  "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Estado" text NOT NULL DEFAULT 'Pendiente' CHECK ("Estado" IN ('Pendiente','Aprobada','Rechazada')),
  "Respuesta" text, "Id_Revisor" integer REFERENCES afrisan."Usuario", "FechaRevision" timestamptz,
  CHECK (("Estado"='Pendiente' AND "Respuesta" IS NULL AND "Id_Revisor" IS NULL AND "FechaRevision" IS NULL)
    OR ("Estado"<>'Pendiente' AND length(btrim("Respuesta")) BETWEEN 5 AND 2000 AND "Id_Revisor" IS NOT NULL AND "FechaRevision" IS NOT NULL))
);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Rectificacion_Pendiente" ON afrisan."RectificacionPesaje"("Id_Pesaje") WHERE "Estado"='Pendiente';
CREATE INDEX IF NOT EXISTS "IX_Rectificacion_OT" ON afrisan."RectificacionPesaje"("Id_OrdenTrabajo");
CREATE OR REPLACE VIEW afrisan."PesajeVigente" WITH (security_invoker=true) AS
SELECT p."Id_Pesaje",p."Id_Cilindro",p."Id_OrdenTrabajo",p."Tipo",
  coalesce(r."PesoPropuestoGr",p."PesoBrutoGr") AS "PesoBrutoGr",p."Id_Usuario",p."Fecha",p."Observacion",
  p."PesoBrutoGr" AS "PesoOriginalGr",r."Id" AS "RectificacionId"
FROM afrisan."Pesaje" p
LEFT JOIN LATERAL (SELECT x."Id",x."PesoPropuestoGr" FROM afrisan."RectificacionPesaje" x
  WHERE x."Id_Pesaje"=p."Id_Pesaje" AND x."Estado"='Aprobada'
  ORDER BY x."FechaRevision" DESC,x."Id" DESC LIMIT 1) r ON true;

CREATE OR REPLACE FUNCTION afrisan.rectificacion_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; p afrisan."Pesaje"; a integer;
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No se borran rectificaciones'; END IF;
  o:=afrisan.ot_actor(CASE WHEN TG_OP='INSERT' THEN NEW."Id_OrdenTrabajo" ELSE OLD."Id_OrdenTrabajo" END,
    CASE WHEN TG_OP='INSERT' THEN ARRAY['Tecnico','Bodega'] ELSE ARRAY['Autorizador'] END);
  IF o."Estado"='Cerrada' THEN RAISE EXCEPTION 'No se rectifica una OT cerrada'; END IF;
  IF TG_OP='INSERT' THEN
    SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
    IF p."Id_OrdenTrabajo" IS DISTINCT FROM o."Id_OrdenTrabajo" OR p."Tipo"='Inicial' THEN RAISE EXCEPTION 'Pesaje ajeno a la OT'; END IF;
    IF NOT (EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_Pesaje"=p."Id_Pesaje")
      OR EXISTS(SELECT 1 FROM afrisan."RecepcionBodega" WHERE "Id_Pesaje"=p."Id_Pesaje")
      OR EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Estado"='Registrada' AND p."Id_Pesaje" IN ("Id_PesajeInstalado","Id_PesajeRetirado"))) THEN RAISE EXCEPTION 'El pesaje debe estar confirmado'; END IF;
    PERFORM afrisan.peso_valido(p."Id_Cilindro",NEW."PesoPropuestoGr");
    SELECT "PesoBrutoGr" INTO NEW."PesoAnteriorGr" FROM afrisan."PesajeVigente" WHERE "Id_Pesaje"=p."Id_Pesaje";
    IF NEW."PesoAnteriorGr"=NEW."PesoPropuestoGr" THEN RAISE EXCEPTION 'El peso propuesto no cambia el vigente'; END IF;
    IF coalesce(NEW."Evidencia"->>'sha256','') !~ '^[0-9a-f]{64}$'
      OR coalesce(NEW."Evidencia"->>'ruta','')='' OR coalesce((NEW."Evidencia"->>'bytes')::bigint,0) NOT BETWEEN 12 AND 4194304 THEN RAISE EXCEPTION 'Falta evidencia privada válida'; END IF;
    IF NEW."Estado"<>'Pendiente' OR NEW."Respuesta" IS NOT NULL OR NEW."Id_Revisor" IS NOT NULL OR NEW."FechaRevision" IS NOT NULL THEN RAISE EXCEPTION 'La solicitud requiere aprobación'; END IF;
    NEW."Id_Solicitante":=afrisan.actor(ARRAY['Tecnico','Bodega']); NEW."Fecha":=clock_timestamp();
  ELSE
    a:=afrisan.actor(ARRAY['Autorizador']);
    IF OLD."Estado"<>'Pendiente' OR NEW."Estado" NOT IN ('Aprobada','Rechazada') OR
      length(coalesce(btrim(NEW."Respuesta"),'')) NOT BETWEEN 5 AND 2000 OR
      (to_jsonb(NEW)-ARRAY['Estado','Respuesta','Id_Revisor','FechaRevision']) IS DISTINCT FROM
      (to_jsonb(OLD)-ARRAY['Estado','Respuesta','Id_Revisor','FechaRevision']) THEN RAISE EXCEPTION 'Solo resolver una solicitud pendiente sin reescribirla'; END IF;
    NEW."Id_Revisor":=a; NEW."FechaRevision":=clock_timestamp();
  END IF;
  RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS "Rectificacion_Validar" ON afrisan."RectificacionPesaje";
CREATE TRIGGER "Rectificacion_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."RectificacionPesaje" FOR EACH ROW EXECUTE FUNCTION afrisan.rectificacion_validar();
CREATE OR REPLACE FUNCTION afrisan.rectificacion_evento() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF TG_OP='UPDATE' AND NEW."Estado"='Aprobada' THEN
    -- Solo cambia la proyección actual si sigue apuntando a este pesaje.
    -- Una corrección histórica nunca desplaza un pesaje posterior.
    UPDATE afrisan."Cilindros" SET "Estado"=CASE WHEN NEW."PesoPropuestoGr"="TaraGr" THEN 'Vacio' ELSE 'Disponible' END
    WHERE "Id_UltimoPesaje"=NEW."Id_Pesaje" AND "Ubicacion"='Bodega'
      AND "Estado" IS DISTINCT FROM CASE WHEN NEW."PesoPropuestoGr"="TaraGr" THEN 'Vacio' ELSE 'Disponible' END;
  END IF;
  INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle")
  VALUES(NEW."Id_OrdenTrabajo",'Rectificación '||NEW."Estado",coalesce(NEW."Id_Revisor",NEW."Id_Solicitante"),
    NEW."Id"::text||': '||NEW."PesoAnteriorGr"||' g → '||NEW."PesoPropuestoGr"||' g');
  RETURN NULL;
END $$;
CREATE OR REPLACE FUNCTION afrisan.cilindro_historial() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  INSERT INTO afrisan."HistorialCilindro"("Id_Cilindro","Ubicacion","Estado","Id_Sucursal","Id_Tecnico","Id_Pesaje","Id_Usuario")
  VALUES(NEW."Id_Cilindro",NEW."Ubicacion",NEW."Estado",NEW."Id_Sucursal",NEW."Id_Tecnico",NEW."Id_UltimoPesaje",afrisan.actor(ARRAY['Bodega','Tecnico','Administrador','Autorizador']));
  RETURN NULL;
END $$;
DROP TRIGGER IF EXISTS "Rectificacion_Evento" ON afrisan."RectificacionPesaje";
CREATE TRIGGER "Rectificacion_Evento" AFTER INSERT OR UPDATE ON afrisan."RectificacionPesaje" FOR EACH ROW EXECUTE FUNCTION afrisan.rectificacion_evento();
CREATE OR REPLACE FUNCTION afrisan.rectificacion_pendiente() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF EXISTS(SELECT 1 FROM afrisan."RectificacionPesaje" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Pendiente') THEN
    RAISE EXCEPTION 'Hay rectificaciones de pesajes sin resolver';
  END IF;
  RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS "OT_Rectificaciones" ON afrisan."OrdenTrabajo";
CREATE TRIGGER "OT_Rectificaciones" BEFORE UPDATE ON afrisan."OrdenTrabajo" FOR EACH ROW WHEN (NEW."Estado"='Cerrada') EXECUTE FUNCTION afrisan.rectificacion_pendiente();
DROP TRIGGER IF EXISTS "Revision_Rectificaciones" ON afrisan."RevisionOT";
CREATE TRIGGER "Revision_Rectificaciones" BEFORE INSERT ON afrisan."RevisionOT" FOR EACH ROW WHEN (NEW."Etapa"='Autorizador') EXECUTE FUNCTION afrisan.rectificacion_pendiente();
ALTER TABLE afrisan."RectificacionPesaje" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS "BackendConfiable" ON afrisan."RectificacionPesaje";
CREATE POLICY "BackendConfiable" ON afrisan."RectificacionPesaje" TO afrisan_api USING(true) WITH CHECK(true);
GRANT SELECT,INSERT,UPDATE ON afrisan."RectificacionPesaje" TO afrisan_api;
GRANT SELECT ON afrisan."PesajeVigente" TO afrisan_api;
REVOKE ALL ON FUNCTION afrisan.rectificacion_validar(),afrisan.rectificacion_evento(),afrisan.rectificacion_pendiente() FROM PUBLIC;
COMMIT;
