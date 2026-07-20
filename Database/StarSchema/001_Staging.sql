-- ============================================================================
-- SIRH.EY - BI Architecture - 001_Staging.sql
-- Cible : (localdb)\mssqllocaldb, base SIRH_EY (celle utilisee par
--         appsettings.json / l'application ASP.NET Core elle-meme)
--
-- AUDIT (verifie via sys.tables / sys.columns, en direct sur cette base) :
--   dbo compte 48 tables. En excluant les 7 tables ASP.NET Identity
--   (AspNetRoles, AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins,
--   AspNetUserRoles, AspNetUsers, AspNetUserTokens) et __EFMigrationsHistory
--   -- deliberement hors perimetre de staging, comme documente dans le
--   script original Database/Staging/001_CreateStagingSchema.sql -- il reste
--   40 tables "metier".
--
--   Le schema stg EXISTE DEJA (cree par Database/Staging/001_CreateStagingSchema.sql,
--   deja execute sur cette base) et couvre les 40 tables + stg.Users (sous-
--   ensemble metier de dbo.AspNetUsers). AUCUNE table de staging n'est
--   manquante : verifie par une requete de diff dbo/stg, resultat vide.
--
-- Ce script ne recree donc RIEN aujourd'hui (regle : ne jamais recreer
-- l'existant). Il reste neanmoins executable et utile : il audite dbo vs stg
-- a chaque execution et cree, si besoin futur, toute table de staging
-- manquante (structure dbo clonee + colonne _StagingLoadedAt), sans jamais
-- toucher a dbo.
-- ============================================================================

USE SIRH_EY;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'stg')
    EXEC('CREATE SCHEMA stg');
GO

-- ============================================================================
-- Detection + creation des tables de staging manquantes (self-healing,
-- idempotent). Aujourd'hui : 0 ligne retournee par le curseur -> rien cree.
-- ============================================================================

DECLARE @tbl NVARCHAR(256), @sql NVARCHAR(MAX), @hasId BIT;

DECLARE missing_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT d.name
FROM sys.tables d
JOIN sys.schemas sd ON d.schema_id = sd.schema_id
WHERE sd.name = 'dbo'
  AND d.name NOT IN (
      '__EFMigrationsHistory',
      'AspNetRoleClaims', 'AspNetRoles', 'AspNetUserClaims', 'AspNetUserLogins',
      'AspNetUserRoles', 'AspNetUsers', 'AspNetUserTokens'
  )
  AND NOT EXISTS (
      SELECT 1 FROM sys.tables s
      JOIN sys.schemas ss ON s.schema_id = ss.schema_id
      WHERE ss.name = 'stg' AND s.name = d.name
  );

OPEN missing_cursor;
FETCH NEXT FROM missing_cursor INTO @tbl;

IF @@FETCH_STATUS <> 0
    PRINT N'Audit : aucune table de staging manquante. stg deja complet (40 tables + stg.Users).';

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Clone la structure exacte de dbo.<tbl> (colonnes/types/identity), sans lignes
    SET @sql = N'SELECT * INTO stg.' + QUOTENAME(@tbl) + N' FROM dbo.' + QUOTENAME(@tbl) + N' WHERE 1 = 0;';
    EXEC sp_executesql @sql;

    -- Ajoute la colonne de tracabilite du chargement
    SET @sql = N'ALTER TABLE stg.' + QUOTENAME(@tbl)
             + N' ADD _StagingLoadedAt DATETIME2(7) NOT NULL '
             + N'CONSTRAINT DF_stg_' + @tbl + N'_LoadedAt DEFAULT SYSUTCDATETIME();';
    EXEC sp_executesql @sql;

    -- Best-effort : si la table source a une colonne Id, on la prend comme PK
    IF EXISTS (SELECT 1 FROM sys.columns c
               JOIN sys.tables t ON c.object_id = t.object_id
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'stg' AND t.name = @tbl AND c.name = 'Id')
    BEGIN
        SET @sql = N'ALTER TABLE stg.' + QUOTENAME(@tbl)
                 + N' ADD CONSTRAINT PK_stg_' + @tbl + N' PRIMARY KEY (Id);';
        EXEC sp_executesql @sql;
    END

    PRINT N'Cree (manquant) : stg.' + @tbl;

    FETCH NEXT FROM missing_cursor INTO @tbl;
END

CLOSE missing_cursor;
DEALLOCATE missing_cursor;
GO

-- ============================================================================
-- RAPPORT : tables existantes reutilisees (aucune action)
-- ============================================================================
SELECT s.name AS schema_name, t.name AS table_name, 'EXISTING - reused' AS status
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'stg'
ORDER BY t.name;
GO
