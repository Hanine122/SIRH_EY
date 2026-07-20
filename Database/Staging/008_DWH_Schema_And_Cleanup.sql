-- ============================================================================
-- SIRH.EY - dwh : schema definitif (reset complet) - instance reelle utilisee
-- Cible : localhost\SQLEXPRESS, base SIRH_EY (verifie via sqlcmd : 21 tables
--         dans dbo, sans dbo.Certifications ni colonnes de certification)
--
-- Ce script DROP puis RECREE integralement les 7 tables dwh (pas de IF NOT
-- EXISTS partiel) : une precedente execution avait laisse un schema dwh
-- incompatible (dim_certification + fact_formations.cert_key, sans source
-- reelle sur cette instance), qui doit disparaitre completement.
--
-- Changements definitifs vs. les versions precedentes :
--   - dim_certification SUPPRIMEE : dbo.Certifications n'existe pas sur
--     cette instance.
--   - fact_formations.cert_key SUPPRIMEE (meme raison).
--   - dim_competence recoit src_competence_id (INT, NOT NULL, PAS unique) :
--     dim_competence reste un catalogue deduplique par nom (1 ligne par nom
--     de competence) ; src_competence_id stocke un Id dbo.Competences
--     representatif (le plus petit du groupe) a titre indicatif seulement.
--     La vraie cle naturelle de deduplication reste "nom" (UNIQUE).
-- ============================================================================

USE SIRH_EY;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dwh')
    EXEC('CREATE SCHEMA dwh');
GO

-- ============================================================================
-- 1. DROP de toute FK existante referencant une table dwh (tolerant a l'etat
--    partiel/incompatible laisse par une precedente tentative)
-- ============================================================================

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'ALTER TABLE dwh.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = 'dwh';

EXEC sp_executesql @sql;
GO

-- ============================================================================
-- 2. DROP de toutes les tables dwh existantes (facts puis dims)
-- ============================================================================

IF OBJECT_ID('dwh.fact_formations', 'U') IS NOT NULL DROP TABLE dwh.fact_formations;
IF OBJECT_ID('dwh.fact_competences', 'U') IS NOT NULL DROP TABLE dwh.fact_competences;
IF OBJECT_ID('dwh.fact_evaluations_talent', 'U') IS NOT NULL DROP TABLE dwh.fact_evaluations_talent;
IF OBJECT_ID('dwh.dim_temps', 'U') IS NOT NULL DROP TABLE dwh.dim_temps;
IF OBJECT_ID('dwh.dim_formation', 'U') IS NOT NULL DROP TABLE dwh.dim_formation;
IF OBJECT_ID('dwh.dim_certification', 'U') IS NOT NULL DROP TABLE dwh.dim_certification;
IF OBJECT_ID('dwh.dim_competence', 'U') IS NOT NULL DROP TABLE dwh.dim_competence;
IF OBJECT_ID('dwh.dim_collaborateur', 'U') IS NOT NULL DROP TABLE dwh.dim_collaborateur;
GO

-- ============================================================================
-- 3. CREATION (7 tables : plus de dim_certification / cert_key)
-- ============================================================================

CREATE TABLE dwh.dim_collaborateur (
    collab_key          INT IDENTITY(1,1) PRIMARY KEY,
    src_collaborateur_id INT NOT NULL,
    nom                 NVARCHAR(100)   NOT NULL,
    prenom              NVARCHAR(100)   NOT NULL,
    poste               NVARCHAR(100)   NULL,
    departement         NVARCHAR(100)   NULL,
    actif               BIT             NOT NULL DEFAULT 1,
    CONSTRAINT UQ_dim_collaborateur_src UNIQUE (src_collaborateur_id)
);
GO

CREATE TABLE dwh.dim_competence (
    competence_key      INT IDENTITY(1,1) PRIMARY KEY,
    src_competence_id   INT NOT NULL,       -- indicatif : MIN(dbo.Competences.Id) du groupe
    nom                 NVARCHAR(150)   NOT NULL,
    categorie           NVARCHAR(100)   NULL,
    CONSTRAINT UQ_dim_competence_nom UNIQUE (nom)
);
GO

CREATE TABLE dwh.dim_formation (
    formation_key       INT IDENTITY(1,1) PRIMARY KEY,
    src_formation_id    INT NOT NULL,
    titre               NVARCHAR(300)   NOT NULL,
    duree               INT             NULL,
    CONSTRAINT UQ_dim_formation_src UNIQUE (src_formation_id)
);
GO

CREATE TABLE dwh.dim_temps (
    date_key            INT             PRIMARY KEY,    -- YYYYMMDD
    annee               INT             NOT NULL,
    mois                INT             NOT NULL,
    jour                INT             NOT NULL
);
GO

CREATE TABLE dwh.fact_evaluations_talent (
    eval_key            INT IDENTITY(1,1) PRIMARY KEY,
    collab_key          INT NOT NULL,
    date_key            INT NOT NULL,
    performance         INT NULL,
    potentiel           INT NULL,
    CONSTRAINT FK_fact_eval_collab FOREIGN KEY (collab_key) REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_eval_temps  FOREIGN KEY (date_key)   REFERENCES dwh.dim_temps (date_key)
);
GO

CREATE TABLE dwh.fact_competences (
    fact_comp_key       INT IDENTITY(1,1) PRIMARY KEY,
    collab_key          INT NOT NULL,
    competence_key      INT NOT NULL,
    date_key            INT NOT NULL,
    niveau              INT NULL,
    CONSTRAINT FK_fact_comp_collab     FOREIGN KEY (collab_key)     REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_comp_competence FOREIGN KEY (competence_key) REFERENCES dwh.dim_competence (competence_key),
    CONSTRAINT FK_fact_comp_temps      FOREIGN KEY (date_key)       REFERENCES dwh.dim_temps (date_key)
);
GO

CREATE TABLE dwh.fact_formations (
    fact_form_key       INT IDENTITY(1,1) PRIMARY KEY,
    collab_key          INT NOT NULL,
    formation_key       INT NOT NULL,
    date_key            INT NOT NULL,
    terminee            BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_fact_form_collab    FOREIGN KEY (collab_key)    REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_form_formation FOREIGN KEY (formation_key) REFERENCES dwh.dim_formation (formation_key),
    CONSTRAINT FK_fact_form_temps     FOREIGN KEY (date_key)      REFERENCES dwh.dim_temps (date_key)
);
GO

PRINT N'dwh : schema recree (7 tables, sans dim_certification/cert_key), pret pour ETL.';
GO
