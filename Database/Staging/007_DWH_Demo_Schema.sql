-- ============================================================================
-- SIRH.EY - Schema dwh (SQL Server) - 8 tables avec PK/FK + donnees de demo
-- Cible : Base SIRH_EY
-- ============================================================================

USE SIRH_EY;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dwh')
    EXEC('CREATE SCHEMA dwh');
GO

-- ============================================================================
-- 1. DIMENSIONS
-- ============================================================================

IF OBJECT_ID('dwh.dim_collaborateur', 'U') IS NOT NULL
    DROP TABLE dwh.dim_collaborateur;

CREATE TABLE dwh.dim_collaborateur (
    collab_key      INT IDENTITY(1,1) PRIMARY KEY,
    nom             NVARCHAR(100)   NOT NULL,
    prenom          NVARCHAR(100)   NOT NULL,
    poste           NVARCHAR(100)   NULL,
    departement     NVARCHAR(100)   NULL,
    actif           BIT             NOT NULL DEFAULT 1
);
GO

IF OBJECT_ID('dwh.dim_competence', 'U') IS NOT NULL
    DROP TABLE dwh.dim_competence;

CREATE TABLE dwh.dim_competence (
    competence_key  INT IDENTITY(1,1) PRIMARY KEY,
    nom             NVARCHAR(150)   NOT NULL,
    categorie       NVARCHAR(100)   NULL
);
GO

IF OBJECT_ID('dwh.dim_certification', 'U') IS NOT NULL
    DROP TABLE dwh.dim_certification;

CREATE TABLE dwh.dim_certification (
    cert_key        INT IDENTITY(1,1) PRIMARY KEY,
    nom             NVARCHAR(200)   NOT NULL,
    plateforme      NVARCHAR(100)   NULL
);
GO

IF OBJECT_ID('dwh.dim_formation', 'U') IS NOT NULL
    DROP TABLE dwh.dim_formation;

CREATE TABLE dwh.dim_formation (
    formation_key   INT IDENTITY(1,1) PRIMARY KEY,
    titre           NVARCHAR(300)   NOT NULL,
    duree           INT             NULL
);
GO

IF OBJECT_ID('dwh.dim_temps', 'U') IS NOT NULL
    DROP TABLE dwh.dim_temps;

CREATE TABLE dwh.dim_temps (
    date_key        INT             PRIMARY KEY,    -- YYYYMMDD
    annee           INT             NOT NULL,
    mois            INT             NOT NULL,
    jour            INT             NOT NULL
);
GO

-- ============================================================================
-- 2. FAITS
-- ============================================================================

IF OBJECT_ID('dwh.fact_evaluations_talent', 'U') IS NOT NULL
    DROP TABLE dwh.fact_evaluations_talent;

CREATE TABLE dwh.fact_evaluations_talent (
    eval_key        INT IDENTITY(1,1) PRIMARY KEY,
    collab_key      INT NOT NULL,
    date_key        INT NOT NULL,
    performance     INT NULL,
    potentiel       INT NULL,
    CONSTRAINT FK_fact_eval_collab FOREIGN KEY (collab_key) REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_eval_temps  FOREIGN KEY (date_key)   REFERENCES dwh.dim_temps (date_key)
);
GO

IF OBJECT_ID('dwh.fact_competences', 'U') IS NOT NULL
    DROP TABLE dwh.fact_competences;

CREATE TABLE dwh.fact_competences (
    fact_comp_key   INT IDENTITY(1,1) PRIMARY KEY,
    collab_key      INT NOT NULL,
    competence_key  INT NOT NULL,
    date_key        INT NOT NULL,
    niveau          INT NULL,
    CONSTRAINT FK_fact_comp_collab     FOREIGN KEY (collab_key)     REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_comp_competence FOREIGN KEY (competence_key) REFERENCES dwh.dim_competence (competence_key),
    CONSTRAINT FK_fact_comp_temps      FOREIGN KEY (date_key)       REFERENCES dwh.dim_temps (date_key)
);
GO

IF OBJECT_ID('dwh.fact_formations', 'U') IS NOT NULL
    DROP TABLE dwh.fact_formations;

CREATE TABLE dwh.fact_formations (
    fact_form_key   INT IDENTITY(1,1) PRIMARY KEY,
    collab_key      INT NOT NULL,
    formation_key   INT NOT NULL,
    cert_key        INT NOT NULL,
    date_key        INT NOT NULL,
    terminee        BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_fact_form_collab    FOREIGN KEY (collab_key)    REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_form_formation FOREIGN KEY (formation_key) REFERENCES dwh.dim_formation (formation_key),
    CONSTRAINT FK_fact_form_cert      FOREIGN KEY (cert_key)      REFERENCES dwh.dim_certification (cert_key),
    CONSTRAINT FK_fact_form_temps     FOREIGN KEY (date_key)      REFERENCES dwh.dim_temps (date_key)
);
GO

-- ============================================================================
-- 3. DONNEES DE DEMO (5 lignes par table)
-- ============================================================================

-- dim_collaborateur (collab_key genere : 1..5)
INSERT INTO dwh.dim_collaborateur (nom, prenom, poste, departement, actif) VALUES
    (N'Benali',    N'Yasmine', N'Consultant',     N'Audit',   1),
    (N'Traore',    N'Moussa',  N'Consultant',     N'Conseil', 1),
    (N'Lefevre',   N'Camille', N'Manager',        N'Audit',   1),
    (N'El Amrani', N'Karim',   N'Manager',        N'Tax',     1),
    (N'Dubreuil',  N'Aurelie', N'Senior Manager', N'Conseil', 1);
GO

-- dim_competence (competence_key genere : 1..5)
INSERT INTO dwh.dim_competence (nom, categorie) VALUES
    (N'Audit financier',        N'Technique'),
    (N'Power BI',               N'Technique'),
    (N'Gestion de projet',      N'Transverse'),
    (N'Fiscalite internationale', N'Fonctionnel'),
    (N'Leadership',             N'Transverse');
GO

-- dim_certification (cert_key genere : 1..5)
INSERT INTO dwh.dim_certification (nom, plateforme) VALUES
    (N'ACCA',                N'ACCA Global'),
    (N'CFA Level 1',         N'CFA Institute'),
    (N'PMP',                 N'PMI'),
    (N'Power BI Data Analyst', N'Microsoft Learn'),
    (N'CIA',                 N'IIA');
GO

-- dim_formation (formation_key genere : 1..5)
INSERT INTO dwh.dim_formation (titre, duree) VALUES
    (N'Introduction a l''audit IFRS', 16),
    (N'Power BI avance',              12),
    (N'Management d''equipe',         8),
    (N'Fiscalite des groupes',        20),
    (N'Techniques de negociation',    6);
GO

-- dim_temps (5 dates)
INSERT INTO dwh.dim_temps (date_key, annee, mois, jour) VALUES
    (20260101, 2026, 1, 1),
    (20260401, 2026, 4, 1),
    (20260701, 2026, 7, 1),
    (20261001, 2026, 10, 1),
    (20261231, 2026, 12, 31);
GO

-- fact_evaluations_talent (1 par collaborateur)
INSERT INTO dwh.fact_evaluations_talent (collab_key, date_key, performance, potentiel) VALUES
    (1, 20260101, 3, 4),
    (2, 20260401, 4, 3),
    (3, 20260701, 5, 5),
    (4, 20261001, 4, 4),
    (5, 20261231, 5, 4);
GO

-- fact_competences (1 par collaborateur x competence)
INSERT INTO dwh.fact_competences (collab_key, competence_key, date_key, niveau) VALUES
    (1, 1, 20260101, 3),
    (2, 2, 20260401, 4),
    (3, 3, 20260701, 5),
    (4, 4, 20261001, 4),
    (5, 5, 20261231, 5);
GO

-- fact_formations (1 par collaborateur x formation x certification)
INSERT INTO dwh.fact_formations (collab_key, formation_key, cert_key, date_key, terminee) VALUES
    (1, 1, 1, 20260101, 1),
    (2, 2, 4, 20260401, 1),
    (3, 3, 3, 20260701, 0),
    (4, 4, 2, 20261001, 1),
    (5, 5, 5, 20261231, 0);
GO
