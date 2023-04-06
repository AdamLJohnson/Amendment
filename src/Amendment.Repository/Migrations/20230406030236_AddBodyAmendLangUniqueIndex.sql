START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230406030236_AddBodyAmendLangUniqueIndex') THEN
    DROP INDEX "IX_AmendmentBody_AmendId_LanguageId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230406030236_AddBodyAmendLangUniqueIndex') THEN
    CREATE UNIQUE INDEX "IX_AmendmentBody_AmendId_LanguageId" ON "AmendmentBody" ("AmendId", "LanguageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20230406030236_AddBodyAmendLangUniqueIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20230406030236_AddBodyAmendLangUniqueIndex', '7.0.4');
    END IF;
END $EF$;
COMMIT;

