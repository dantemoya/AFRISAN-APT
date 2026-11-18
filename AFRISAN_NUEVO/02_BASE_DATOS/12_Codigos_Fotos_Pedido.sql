-- Actualización aditiva. Ejecutar después de 11. Corrige defectos del recorrido del 14/11/2026:
--   D-47 (DEF-09)  Códigos cortos de 8 caracteres: OT-00001 para las OT y AF-00001 para los
--                  cilindros, generados por secuencias de la base (hasta 99.999 de cada uno).
--                  Los registros existentes conservan su código.
--   D-48 (DEF-06)  Todo pesaje nuevo exige al menos una foto de la balanza. Se comprueba al
--                  confirmar la transacción, así el pesaje y su foto se guardan juntos.
--   D-49 (DEF-12)  La OT registra quién la pidió en el cliente, su prioridad y su fecha
--                  programada; los tres datos son obligatorios al crearla y no se modifican.
-- No modifica registros existentes.
BEGIN;

-- 1. Códigos cortos (D-47)
CREATE SEQUENCE IF NOT EXISTS afrisan."SecuenciaCodigoOT" AS integer MINVALUE 1 MAXVALUE 99999 NO CYCLE;
CREATE SEQUENCE IF NOT EXISTS afrisan."SecuenciaCodigoCilindro" AS integer MINVALUE 1 MAXVALUE 99999 NO CYCLE;
ALTER TABLE afrisan."OrdenTrabajo" ALTER COLUMN "CodigoOT"
  SET DEFAULT ('OT-'||lpad(nextval('afrisan."SecuenciaCodigoOT"')::text,5,'0'));
ALTER TABLE afrisan."Cilindros" ALTER COLUMN "CilindroQR"
  SET DEFAULT ('AF-'||lpad(nextval('afrisan."SecuenciaCodigoCilindro"')::text,5,'0'));
GRANT USAGE,SELECT ON SEQUENCE afrisan."SecuenciaCodigoOT",afrisan."SecuenciaCodigoCilindro" TO afrisan_api;

-- 2. Foto obligatoria en todo pesaje nuevo (D-48)
CREATE OR REPLACE FUNCTION afrisan.pesaje_requiere_foto() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje") THEN
    RAISE EXCEPTION 'Todo pesaje requiere al menos una foto de la balanza';
  END IF;
  RETURN NULL;
END $$;
DROP TRIGGER IF EXISTS "TR_Pesaje_Requiere_Foto" ON afrisan."Pesaje";
CREATE CONSTRAINT TRIGGER "TR_Pesaje_Requiere_Foto" AFTER INSERT ON afrisan."Pesaje"
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION afrisan.pesaje_requiere_foto();

-- 3. Datos del pedido en la OT (D-49)
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "Solicitante" varchar(150);
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "Prioridad" varchar(10);
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "FechaProgramada" date;
ALTER TABLE afrisan."OrdenTrabajo" DROP CONSTRAINT IF EXISTS "OrdenTrabajo_Prioridad_check";
ALTER TABLE afrisan."OrdenTrabajo" ADD CONSTRAINT "OrdenTrabajo_Prioridad_check"
  CHECK ("Prioridad" IS NULL OR "Prioridad" IN ('Baja','Normal','Alta','Urgente'));
ALTER TABLE afrisan."OrdenTrabajo" DROP CONSTRAINT IF EXISTS "OrdenTrabajo_Solicitante_check";
ALTER TABLE afrisan."OrdenTrabajo" ADD CONSTRAINT "OrdenTrabajo_Solicitante_check"
  CHECK ("Solicitante" IS NULL OR length(btrim("Solicitante")) BETWEEN 3 AND 150);
CREATE OR REPLACE FUNCTION afrisan.ot_pedido_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF TG_OP='INSERT' THEN
    IF NEW."Solicitante" IS NULL OR NEW."Prioridad" IS NULL OR NEW."FechaProgramada" IS NULL THEN
      RAISE EXCEPTION 'La OT requiere solicitante, prioridad y fecha programada'; END IF;
    IF NEW."FechaProgramada" < (clock_timestamp() AT TIME ZONE 'America/Santiago')::date THEN
      RAISE EXCEPTION 'La fecha programada no puede ser anterior a hoy'; END IF;
  ELSIF (NEW."Solicitante",NEW."Prioridad",NEW."FechaProgramada")
        IS DISTINCT FROM (OLD."Solicitante",OLD."Prioridad",OLD."FechaProgramada") THEN
    RAISE EXCEPTION 'Solicitante, prioridad y fecha programada no se modifican';
  END IF;
  RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS "TR_OT_Pedido_Validar" ON afrisan."OrdenTrabajo";
CREATE TRIGGER "TR_OT_Pedido_Validar" BEFORE INSERT OR UPDATE ON afrisan."OrdenTrabajo"
  FOR EACH ROW EXECUTE FUNCTION afrisan.ot_pedido_validar();

REVOKE ALL ON FUNCTION afrisan.pesaje_requiere_foto(),afrisan.ot_pedido_validar() FROM PUBLIC;
COMMIT;
