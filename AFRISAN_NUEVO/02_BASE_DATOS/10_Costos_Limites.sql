-- AFRISAN | Precios, conversiones manuales y límites por gas | 08/11/2026
-- Aditiva e idempotente. No aplica valores de mercado ni asigna gas a límites antiguos.
BEGIN;
ALTER TABLE afrisan."PreciosRefrigerantes" ADD COLUMN IF NOT EXISTS "Id_Solicitud" uuid UNIQUE;
-- Permite registrar una corrección de precio el mismo día sin reemplazar la anterior.
ALTER TABLE afrisan."PreciosRefrigerantes" DROP CONSTRAINT IF EXISTS "PreciosRefrigerantes_Id_Refrigerante_Moneda_VigenteDesde_key";
-- UF se presenta y almacena como UF; se conservan monedas históricas de tres letras.
ALTER TABLE afrisan."PreciosRefrigerantes" DROP CONSTRAINT IF EXISTS "PreciosRefrigerantes_Moneda_check";
ALTER TABLE afrisan."PreciosRefrigerantes" ADD CONSTRAINT "PreciosRefrigerantes_Moneda_check" CHECK ("Moneda" ~ '^[A-Z]{3}$' OR "Moneda"='UF');
ALTER TABLE afrisan."Costos" DROP CONSTRAINT IF EXISTS "Costos_Moneda_check";
ALTER TABLE afrisan."Costos" ADD CONSTRAINT "Costos_Moneda_check" CHECK ("Moneda" ~ '^[A-Z]{3}$' OR "Moneda"='UF');
CREATE TABLE IF NOT EXISTS afrisan."TiposCambio" (
  "Id_Cambio" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Solicitud" uuid NOT NULL UNIQUE,
  "Moneda" varchar(3) NOT NULL CHECK ("Moneda" IN ('EUR','USD','UF')),
  "ClpPorUnidad" numeric(20,8) NOT NULL CHECK ("ClpPorUnidad">0 AND "ClpPorUnidad"<=9999999),
  "FechaValor" date NOT NULL,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan."Usuario",
  "FechaRegistro" timestamptz NOT NULL DEFAULT now()
);
ALTER TABLE afrisan."Costos" ADD COLUMN IF NOT EXISTS "Id_Solicitud" uuid UNIQUE;
ALTER TABLE afrisan."Costos" ADD COLUMN IF NOT EXISTS "Id_Cambio" bigint REFERENCES afrisan."TiposCambio";
ALTER TABLE afrisan."Costos" ADD COLUMN IF NOT EXISTS "FactorClp" numeric(20,8);
ALTER TABLE afrisan."Costos" ADD COLUMN IF NOT EXISTS "TotalClp" numeric(30,8) GENERATED ALWAYS AS (round("Cantidad"*"TarifaUnitaria"*"FactorClp",8)) STORED;

ALTER TABLE afrisan."LimiteSucursal" ADD COLUMN IF NOT EXISTS "Id_Refrigerante" bigint REFERENCES afrisan."Refrigerantes";
ALTER TABLE afrisan."LimiteSucursalHistorial" ADD COLUMN IF NOT EXISTS "Id_Refrigerante" bigint REFERENCES afrisan."Refrigerantes";
ALTER TABLE afrisan."LimiteSucursal" DROP CONSTRAINT IF EXISTS "LimiteSucursal_Id_Sucursal_Periodicidad_key";
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Limite_Gas_Periodo" ON afrisan."LimiteSucursal" ("Id_Sucursal","Id_Refrigerante","Periodicidad") WHERE "Id_Refrigerante" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Limite_Legado" ON afrisan."LimiteSucursal" ("Id_Sucursal","Periodicidad") WHERE "Id_Refrigerante" IS NULL;
ALTER TABLE afrisan."MedicionLimite" ADD COLUMN IF NOT EXISTS "Id_Solicitud" uuid UNIQUE;
ALTER TABLE afrisan."MedicionLimiteOperaciones" ADD COLUMN IF NOT EXISTS "GrInstalados" bigint;

CREATE OR REPLACE FUNCTION afrisan.validar_limite_sucursal() RETURNS trigger
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan AS $$
DECLARE a integer; rol text; cambio boolean; dia date;
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'El límite y su historial se conservan'; END IF;
  a:=afrisan.actor(ARRAY['Bodega','Jefatura']);
  SELECT r."Nombre_Rol" INTO rol FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING("Id_Rol") WHERE u."Id_Usuario"=a;
  IF NEW."Id_Refrigerante" IS NULL THEN RAISE EXCEPTION 'Define un límite nuevo por gas; se conserva el límite general anterior'; END IF;
  dia:=(NEW."VigenteDesde" AT TIME ZONE 'America/Santiago')::date;
  IF NEW."VigenteDesde" <> dia::timestamp AT TIME ZONE 'America/Santiago'
    OR (NEW."Periodicidad"='Semana' AND extract(isodow FROM dia)<>1)
    OR (NEW."Periodicidad"='Mes' AND extract(day FROM dia)<>1) THEN
    RAISE EXCEPTION 'La vigencia debe comenzar el lunes o primer día del mes en horario chileno'; END IF;
  IF NEW."EstadoDefinicion"<>'Definido' OR NEW."CriterioComputo"<>'GasInstaladoConfirmadoChile' THEN RAISE EXCEPTION 'Usa la regla de gas instalado confirmado por calendario chileno'; END IF;
  IF TG_OP='INSERT' THEN
    PERFORM 1 FROM afrisan."Sucursales" s JOIN afrisan."Cliente" c USING("Id_Cliente") WHERE s."Id_Sucursal"=NEW."Id_Sucursal" AND s."Activo" AND c."Activo";
    IF NOT FOUND THEN RAISE EXCEPTION 'Selecciona una sucursal activa'; END IF;
    PERFORM 1 FROM afrisan."Refrigerantes" WHERE "Id_Refrigerante"=NEW."Id_Refrigerante" AND "Activo";
    IF NOT FOUND THEN RAISE EXCEPTION 'Selecciona un gas activo'; END IF;
    NEW."Version":=1; NEW."BloqueadoBodega":=rol='Jefatura';
  ELSE
    IF (NEW."Id_Limite",NEW."Id_Sucursal",NEW."Id_Refrigerante",NEW."Periodicidad") IS DISTINCT FROM
       (OLD."Id_Limite",OLD."Id_Sucursal",OLD."Id_Refrigerante",OLD."Periodicidad") THEN RAISE EXCEPTION 'La identidad del límite se conserva'; END IF;
    IF rol='Bodega' AND (OLD."BloqueadoBodega" OR NEW."BloqueadoBodega" IS DISTINCT FROM OLD."BloqueadoBodega") THEN RAISE EXCEPTION 'Jefatura controla la edición del límite por Bodega'; END IF;
    -- No se reescriben vigencias pasadas: pueden añadirse versiones para el mismo inicio o uno posterior.
    IF NEW."VigenteDesde"<OLD."VigenteDesde" THEN RAISE EXCEPTION 'La nueva vigencia no puede retroceder'; END IF;
    cambio:=(NEW."EstadoDefinicion",NEW."GrLimite",NEW."CriterioComputo",NEW."VigenteDesde") IS DISTINCT FROM
      (OLD."EstadoDefinicion",OLD."GrLimite",OLD."CriterioComputo",OLD."VigenteDesde");
    IF rol='Jefatura' AND cambio THEN NEW."BloqueadoBodega":=true; END IF;
    NEW."Version":=OLD."Version"+1;
  END IF;
  NEW."Id_UltimoEditor":=a; NEW."FechaActualizacion":=clock_timestamp(); RETURN NEW;
END $$;
CREATE OR REPLACE FUNCTION afrisan.historiar_limite_sucursal() RETURNS trigger
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan AS $$
BEGIN
  INSERT INTO afrisan."LimiteSucursalHistorial" ("Id_Limite","Version","Id_Sucursal","Id_Refrigerante","Periodicidad","EstadoDefinicion","GrLimite","CriterioComputo","VigenteDesde","BloqueadoBodega","Id_Actor","FechaRegistro")
  VALUES(NEW."Id_Limite",NEW."Version",NEW."Id_Sucursal",NEW."Id_Refrigerante",NEW."Periodicidad",NEW."EstadoDefinicion",NEW."GrLimite",NEW."CriterioComputo",NEW."VigenteDesde",NEW."BloqueadoBodega",NEW."Id_UltimoEditor",NEW."FechaActualizacion"); RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION afrisan.control_costos_limites() RETURNS trigger
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan AS $$
DECLARE p afrisan."PreciosRefrigerantes"%ROWTYPE; tc afrisan."TiposCambio"%ROWTYPE;
  h afrisan."LimiteSucursalHistorial"%ROWTYPE; m afrisan."MedicionLimite"%ROWTYPE; total bigint;
BEGIN
  IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'El registro histórico se conserva sin cambios'; END IF;
  IF TG_TABLE_NAME='TiposCambio' THEN
    NEW."Id_Registrador":=afrisan.actor(ARRAY['Administrador']); NEW."FechaRegistro":=clock_timestamp();
    IF NEW."FechaValor">(now() AT TIME ZONE 'America/Santiago')::date THEN RAISE EXCEPTION 'No se registra un valor de cambio futuro'; END IF;
  ELSIF TG_TABLE_NAME='PreciosRefrigerantes' THEN
    IF NEW."Moneda" NOT IN ('CLP','EUR','USD','UF') OR NEW."Id_Solicitud" IS NULL OR NEW."PrecioGrRefrigerante">999999 THEN RAISE EXCEPTION 'Revisa moneda, precio y solicitud'; END IF;
    PERFORM 1 FROM afrisan."Refrigerantes" WHERE "Id_Refrigerante"=NEW."Id_Refrigerante" AND "Activo";
    IF NOT FOUND THEN RAISE EXCEPTION 'Selecciona un gas activo'; END IF;
  ELSIF TG_TABLE_NAME='Costos' THEN
    IF NEW."Concepto"<>'Refrigerante' OR NEW."Unidad"<>'g' OR NEW."Id_Precio" IS NULL OR NEW."Id_Solicitud" IS NULL OR NEW."Cantidad">999999999 THEN RAISE EXCEPTION 'Esta etapa solo valora gramos explícitos de refrigerante'; END IF;
    SELECT * INTO STRICT p FROM afrisan."PreciosRefrigerantes" WHERE "Id_Precio"=NEW."Id_Precio";
    IF p."VigenteDesde">now() THEN RAISE EXCEPTION 'El precio aún no está vigente'; END IF;
    IF p."Moneda"='CLP' THEN
      IF NEW."Id_Cambio" IS NOT NULL OR NEW."FactorClp" IS DISTINCT FROM 1::numeric THEN RAISE EXCEPTION 'CLP usa factor 1 sin conversión'; END IF;
    ELSE
      SELECT * INTO tc FROM afrisan."TiposCambio" WHERE "Id_Cambio"=NEW."Id_Cambio";
      IF tc."Id_Cambio" IS NULL OR tc."Moneda"<>p."Moneda" OR NEW."FactorClp" IS DISTINCT FROM tc."ClpPorUnidad" THEN RAISE EXCEPTION 'Selecciona el valor manual de cambio de la moneda del precio'; END IF;
    END IF;
  ELSIF TG_TABLE_NAME='MedicionLimite' THEN
    SELECT * INTO STRICT h FROM afrisan."LimiteSucursalHistorial" WHERE "Id_HistorialLimite"=NEW."Id_HistorialLimite";
    IF h."Id_Refrigerante" IS NULL OR h."CriterioComputo"<>'GasInstaladoConfirmadoChile' OR NEW."Id_Solicitud" IS NULL
      OR NEW."MetodologiaComputo"<>'GasInstaladoConfirmadoChile' THEN RAISE EXCEPTION 'La medición requiere la regla por gas aprobada'; END IF;
    IF NEW."PeriodoInicio">(now() AT TIME ZONE 'America/Santiago')::date OR h."VigenteDesde">NEW."PeriodoInicio"::timestamp AT TIME ZONE 'America/Santiago' THEN RAISE EXCEPTION 'No hay un límite vigente al inicio de ese período'; END IF;
    IF (h."Periodicidad"='Semana' AND (extract(isodow FROM NEW."PeriodoInicio")<>1 OR NEW."PeriodoFin"<>NEW."PeriodoInicio"+6))
      OR (h."Periodicidad"='Mes' AND (extract(day FROM NEW."PeriodoInicio")<>1 OR NEW."PeriodoFin"<>(NEW."PeriodoInicio"+interval '1 month'-interval '1 day')::date)) THEN RAISE EXCEPTION 'El período debe corresponder al calendario chileno acordado'; END IF;
    SELECT coalesce(sum(pv."PesoBrutoGr"-c."TaraGr"),0)::bigint INTO total FROM afrisan."OperacionTerreno" op
      JOIN afrisan."OrdenTrabajo" ot USING("Id_OrdenTrabajo") JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado"
      JOIN afrisan."PesajeVigente" pv ON pv."Id_Pesaje"=op."Id_PesajeInstalado"
      WHERE op."Estado"='Registrada' AND ot."Id_Sucursal"=h."Id_Sucursal" AND c."Id_Refrigerante"=h."Id_Refrigerante"
      AND (op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date BETWEEN NEW."PeriodoInicio" AND NEW."PeriodoFin";
    IF total<>NEW."GrComputados" THEN RAISE EXCEPTION 'La medición debe coincidir con los pesos confirmados vigentes'; END IF;
  ELSIF TG_TABLE_NAME='MedicionLimiteOperaciones' THEN
    SELECT * INTO STRICT m FROM afrisan."MedicionLimite" WHERE "Id_Medicion"=NEW."Id_Medicion";
    SELECT * INTO STRICT h FROM afrisan."LimiteSucursalHistorial" WHERE "Id_HistorialLimite"=m."Id_HistorialLimite";
    IF NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" op JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado"
      JOIN afrisan."PesajeVigente" pv ON pv."Id_Pesaje"=op."Id_PesajeInstalado" WHERE op."Id_Operacion"=NEW."Id_Operacion"
      AND c."Id_Refrigerante"=h."Id_Refrigerante" AND (op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date BETWEEN m."PeriodoInicio" AND m."PeriodoFin"
      AND NEW."GrInstalados"=pv."PesoBrutoGr"-c."TaraGr") THEN RAISE EXCEPTION 'La operación no corresponde al gas, período o peso del cómputo'; END IF;
  END IF;
  RETURN NEW;
END $$;
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['TiposCambio','PreciosRefrigerantes','Costos','MedicionLimite','MedicionLimiteOperaciones'] LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS "Control_CostosLimites" ON afrisan.%I',t);
    EXECUTE format('CREATE TRIGGER "Control_CostosLimites" BEFORE INSERT OR UPDATE OR DELETE ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.control_costos_limites()',t);
  END LOOP;
  FOREACH t IN ARRAY ARRAY['TiposCambio','PreciosRefrigerantes','Costos','LimiteSucursal','LimiteSucursalHistorial','MedicionLimite','MedicionLimiteOperaciones','RevisionExceso'] LOOP
    EXECUTE format('ALTER TABLE afrisan.%I ENABLE ROW LEVEL SECURITY',t);
    EXECUTE format('GRANT SELECT,INSERT ON afrisan.%I TO afrisan_api',t);
    IF NOT EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='afrisan' AND tablename=t AND policyname='BackendConfiable') THEN
      EXECUTE format('CREATE POLICY "BackendConfiable" ON afrisan.%I TO afrisan_api USING(true) WITH CHECK(true)',t); END IF;
  END LOOP;
END $$;
GRANT UPDATE ON afrisan."LimiteSucursal" TO afrisan_api;
DO $$ DECLARE s record; BEGIN
  FOR s IN SELECT seq.relname FROM pg_class seq JOIN pg_depend d ON d.objid=seq.oid AND d.classid='pg_class'::regclass
    JOIN pg_class t ON t.oid=d.refobjid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE seq.relkind='S' AND n.nspname='afrisan'
    AND d.deptype IN ('a','i') AND t.relname IN ('TiposCambio','PreciosRefrigerantes','Costos','LimiteSucursal','LimiteSucursalHistorial','MedicionLimite','RevisionExceso')
  LOOP EXECUTE format('GRANT USAGE,SELECT ON SEQUENCE afrisan.%I TO afrisan_api',s.relname); END LOOP;
END $$;
DROP TRIGGER IF EXISTS "TR_Auditoria" ON afrisan."TiposCambio";
CREATE TRIGGER "TR_Auditoria" AFTER INSERT OR UPDATE OR DELETE ON afrisan."TiposCambio" FOR EACH ROW EXECUTE FUNCTION afrisan.auditar();
REVOKE ALL ON afrisan."TiposCambio" FROM PUBLIC;
REVOKE ALL ON FUNCTION afrisan.control_costos_limites() FROM PUBLIC;
CREATE OR REPLACE VIEW afrisan."LimitesVigentes" AS
SELECT DISTINCT ON ("Id_Limite") "Id_Limite","Id_Sucursal","Periodicidad","EstadoDefinicion","GrLimite","CriterioComputo",
  "VigenteDesde","BloqueadoBodega","Version","FechaRegistro" AS "FechaActualizacion","Id_Refrigerante"
FROM afrisan."LimiteSucursalHistorial" WHERE "VigenteDesde"<=current_timestamp
ORDER BY "Id_Limite","VigenteDesde" DESC,"Version" DESC;
CREATE OR REPLACE VIEW afrisan."ExcesosRegistrados" AS
SELECT m."Id_Medicion",h."Id_Sucursal",h."Periodicidad",m."PeriodoInicio",m."PeriodoFin",h."Version" AS "VersionLimiteAplicada",h."GrLimite",m."GrComputados",
  greatest(m."GrComputados"-h."GrLimite",0) AS "GrExcesoDeclarado",h."CriterioComputo",m."MetodologiaComputo",m."FechaRegistro",r."Conclusion",r."FechaRevision",h."Id_Refrigerante"
FROM afrisan."MedicionLimite" m JOIN afrisan."LimiteSucursalHistorial" h USING("Id_HistorialLimite") LEFT JOIN afrisan."RevisionExceso" r USING("Id_Medicion");
CREATE OR REPLACE VIEW afrisan."CostosOperaciones" AS
SELECT c."Id_Costos",c."Id_Operacion",op."Id_OrdenTrabajo",ot."Id_Sucursal",c."NumeroLinea",c."Concepto",c."Descripcion",c."Cantidad",c."Unidad",c."TarifaUnitaria",c."Moneda",c."PrecioTotal",c."FechaValoracion",
  c."Id_Cambio",c."FactorClp",c."TotalClp"
FROM afrisan."Costos" c JOIN afrisan."OperacionTerreno" op USING("Id_Operacion") JOIN afrisan."OrdenTrabajo" ot USING("Id_OrdenTrabajo");
COMMENT ON TABLE afrisan."MedicionLimite" IS 'Desde 10: gramos netos instalados confirmados por gas, lunes-domingo y mes calendario America/Santiago, con PesajeVigente. Cada medición conserva el resultado y las operaciones de ese corte.';
COMMIT;
