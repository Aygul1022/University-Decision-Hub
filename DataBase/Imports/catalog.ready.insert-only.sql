-- Deprecated entry point: the old SQL import was a 92-programme snapshot.
-- Current authoritative data: DataBase/Imports/catalog.ready.json (104 programmes).
-- Use the existing application importer; it validates the file and pending migrations.
-- From the repository root (Development environment, private local DB settings):
-- dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- import-catalog DataBase/Imports/catalog.ready.json --apply
-- See Doc/Katalog_Entegrasyon_2026-11-19.md before importing into an older database.
-- Historical SQL is preserved under history/ and must not be used as current data.
DO $deprecated$
BEGIN
 RAISE EXCEPTION 'This SQL entry point is retired. Use import-catalog with the current catalog.ready.json; see Doc/Katalog_Entegrasyon_2026-11-19.md.';
END;
$deprecated$;
