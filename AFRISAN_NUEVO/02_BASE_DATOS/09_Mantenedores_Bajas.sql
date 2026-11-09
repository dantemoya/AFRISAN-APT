-- Actualización aditiva posterior a 07/08. No elimina registros ni cambia estados existentes.
BEGIN;
ALTER TABLE afrisan."Cliente" ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1 CHECK ("Version">0);
ALTER TABLE afrisan."Sucursales" ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1 CHECK ("Version">0);
ALTER TABLE afrisan."Refrigerantes" ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1 CHECK ("Version">0);

CREATE OR REPLACE FUNCTION afrisan.mantenedor_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Los mantenedores se desactivan; no se eliminan'; END IF;
  IF TG_TABLE_NAME='Refrigerantes' THEN
    PERFORM afrisan.actor(ARRAY['Administrador','Bodega','Jefatura']);
    NEW."CodigoRefrigerante":=upper(btrim(NEW."CodigoRefrigerante"));
    NEW."NombreRefrigerante":=btrim(NEW."NombreRefrigerante");
  ELSE PERFORM afrisan.actor(ARRAY['Administrador']); END IF;
  IF TG_TABLE_NAME='Cliente' THEN
    NEW."RutCliente":=btrim(NEW."RutCliente"); NEW."NombreEmpresa":=btrim(NEW."NombreEmpresa");
  ELSIF TG_TABLE_NAME='Sucursales' THEN
    NEW."NombreSucursal":=btrim(NEW."NombreSucursal"); NEW."Direccion":=btrim(NEW."Direccion");
    IF TG_OP='INSERT' OR (NEW."Activo" AND NOT OLD."Activo") THEN
      PERFORM 1 FROM afrisan."Cliente" WHERE "Id_Cliente"=NEW."Id_Cliente" AND "Activo" FOR SHARE;
      IF NOT FOUND THEN RAISE EXCEPTION 'Activa primero el cliente de la sucursal'; END IF;
    END IF;
  END IF;
  IF TG_OP='INSERT' THEN NEW."Version":=1; RETURN NEW; END IF;
  IF TG_TABLE_NAME='Cliente' THEN
    IF NEW."Id_Cliente"<>OLD."Id_Cliente" THEN RAISE EXCEPTION 'La identidad del cliente se conserva'; END IF;
    IF OLD."Activo" AND NOT NEW."Activo" AND (
      EXISTS(SELECT 1 FROM afrisan."OrdenTrabajo" o JOIN afrisan."Sucursales" s USING("Id_Sucursal") WHERE s."Id_Cliente"=OLD."Id_Cliente" AND o."Estado"<>'Cerrada')
      OR EXISTS(SELECT 1 FROM afrisan."Cilindros" c JOIN afrisan."Sucursales" s USING("Id_Sucursal") WHERE s."Id_Cliente"=OLD."Id_Cliente" AND c."Ubicacion"='Sucursal'))
      THEN RAISE EXCEPTION 'El cliente tiene OT abiertas o cilindros instalados'; END IF;
  ELSIF TG_TABLE_NAME='Sucursales' THEN
    IF (NEW."Id_Sucursal",NEW."Id_Cliente") IS DISTINCT FROM (OLD."Id_Sucursal",OLD."Id_Cliente") THEN RAISE EXCEPTION 'No se traslada una sucursal a otro cliente'; END IF;
    IF OLD."Activo" AND NOT NEW."Activo" AND (
      EXISTS(SELECT 1 FROM afrisan."OrdenTrabajo" WHERE "Id_Sucursal"=OLD."Id_Sucursal" AND "Estado"<>'Cerrada')
      OR EXISTS(SELECT 1 FROM afrisan."Cilindros" WHERE "Id_Sucursal"=OLD."Id_Sucursal" AND "Ubicacion"='Sucursal'))
      THEN RAISE EXCEPTION 'La sucursal tiene OT abiertas o cilindros instalados'; END IF;
  ELSE
    IF NEW."Id_Refrigerante"<>OLD."Id_Refrigerante" THEN RAISE EXCEPTION 'La identidad del refrigerante se conserva'; END IF;
    IF OLD."Activo" AND NOT NEW."Activo" AND EXISTS(SELECT 1 FROM afrisan."DetalleOT" d JOIN afrisan."OrdenTrabajo" o USING("Id_OrdenTrabajo") WHERE d."Id_Refrigerante"=OLD."Id_Refrigerante" AND o."Estado"<>'Cerrada')
      THEN RAISE EXCEPTION 'El refrigerante tiene OT abiertas'; END IF;
  END IF;
  NEW."Version":=OLD."Version"+1; RETURN NEW;
END $$;
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['Cliente','Sucursales','Refrigerantes'] LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS "Mantenedor_Validar" ON afrisan.%I',t);
    EXECUTE format('CREATE TRIGGER "Mantenedor_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.mantenedor_validar()',t);
  END LOOP;
END $$;

-- Compartir bloqueo con la edición/desactivación evita altas contra un catálogo inactivo.
CREATE OR REPLACE FUNCTION afrisan.catalogo_referencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE cliente integer;
BEGIN
  IF TG_TABLE_NAME='OrdenTrabajo' OR (TG_TABLE_NAME='Cilindros' AND to_jsonb(NEW)->>'Ubicacion'='Sucursal') THEN
    SELECT "Id_Cliente" INTO cliente FROM afrisan."Sucursales" WHERE "Id_Sucursal"=NEW."Id_Sucursal";
    PERFORM 1 FROM afrisan."Cliente" WHERE "Id_Cliente"=cliente AND "Activo" FOR SHARE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Selecciona un cliente activo'; END IF;
    PERFORM 1 FROM afrisan."Sucursales" WHERE "Id_Sucursal"=NEW."Id_Sucursal" AND "Activo" FOR SHARE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Selecciona una sucursal activa'; END IF;
  END IF;
  IF TG_TABLE_NAME IN ('Cilindros','DetalleOT') THEN
    PERFORM 1 FROM afrisan."Refrigerantes" WHERE "Id_Refrigerante"=NEW."Id_Refrigerante" AND "Activo" FOR SHARE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Selecciona un refrigerante activo'; END IF;
  END IF;
  RETURN NEW;
END $$;
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['OrdenTrabajo','Cilindros','DetalleOT'] LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS "Catalogo_Referencia" ON afrisan.%I',t);
    EXECUTE format('CREATE TRIGGER "Catalogo_Referencia" BEFORE INSERT ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.catalogo_referencia_validar()',t);
  END LOOP;
END $$;
GRANT UPDATE ON afrisan."Cliente",afrisan."Sucursales",afrisan."Refrigerantes" TO afrisan_api;
GRANT SELECT,INSERT ON afrisan."BajaCilindro" TO afrisan_api;
DROP POLICY IF EXISTS "BackendConfiable" ON afrisan."BajaCilindro";
CREATE POLICY "BackendConfiable" ON afrisan."BajaCilindro" TO afrisan_api USING(true) WITH CHECK(true);
REVOKE ALL ON FUNCTION afrisan.mantenedor_validar(),afrisan.catalogo_referencia_validar() FROM PUBLIC;
COMMIT;
