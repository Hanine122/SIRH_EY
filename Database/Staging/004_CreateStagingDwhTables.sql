/* ============================================================================
   SIRH.EY — Staging / DWH test tables (SQL Server)

   Assumes the 'staging' and 'dwh' schemas already exist on this server.
   Scope:
   - staging.stg_collaborateurs / stg_competences / stg_evaluations
     Loose, unconstrained pass-through tables for quick testing (no PK/FK).
   - dwh.dim_collaborateur / dim_competence / fact_evaluations
     Minimal star schema: surrogate IDENTITY keys on the dimensions, fact
     table wired to both via FK.
   - 5 sample EY collaborator rows (Consultant / Manager / Senior Manager)
     inserted into staging.stg_collaborateurs for testing.
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'staging')
    EXEC('CREATE SCHEMA staging');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dwh')
    EXEC('CREATE SCHEMA dwh');
GO

/* ============================================================================
   1. STAGING TABLES (no constraints — raw landing zone)
   ============================================================================ */

CREATE TABLE staging.stg_collaborateurs (
    collaborateur_id    INT             NULL,
    nom                 NVARCHAR(100)   NULL,
    prenom              NVARCHAR(100)   NULL,
    email               NVARCHAR(200)   NULL,
    grade               NVARCHAR(50)    NULL,
    departement         NVARCHAR(100)   NULL,
    date_embauche       DATE            NULL
);
GO

CREATE TABLE staging.stg_competences (
    competence_id       INT             NULL,
    collaborateur_id    INT             NULL,
    nom_competence       NVARCHAR(150)   NULL,
    niveau_actuel        INT             NULL,
    niveau_cible         INT             NULL,
    date_evaluation      DATE            NULL
);
GO

CREATE TABLE staging.stg_evaluations (
    evaluation_id        INT             NULL,
    collaborateur_id     INT             NULL,
    competence_id        INT             NULL,
    evaluateur           NVARCHAR(150)   NULL,
    score                INT             NULL,
    commentaire          NVARCHAR(MAX)   NULL,
    date_evaluation       DATE            NULL
);
GO

/* ============================================================================
   2. DWH TABLES (dimensions with surrogate keys + fact table)
   ============================================================================ */

CREATE TABLE dwh.dim_collaborateur (
    collab_key          INT             IDENTITY(1,1) NOT NULL,
    collaborateur_id    INT             NOT NULL,
    nom                 NVARCHAR(100)   NULL,
    prenom              NVARCHAR(100)   NULL,
    email               NVARCHAR(200)   NULL,
    grade               NVARCHAR(50)    NULL,
    departement         NVARCHAR(100)   NULL,
    date_embauche       DATE            NULL,
    CONSTRAINT PK_dim_collaborateur PRIMARY KEY (collab_key)
);
GO

CREATE TABLE dwh.dim_competence (
    competence_key      INT             IDENTITY(1,1) NOT NULL,
    competence_id       INT             NOT NULL,
    nom_competence      NVARCHAR(150)   NULL,
    CONSTRAINT PK_dim_competence PRIMARY KEY (competence_key)
);
GO

CREATE TABLE dwh.fact_evaluations (
    fact_evaluation_id  INT             IDENTITY(1,1) NOT NULL,
    collab_key          INT             NOT NULL,
    competence_key      INT             NOT NULL,
    niveau_actuel       INT             NULL,
    niveau_cible        INT             NULL,
    score               INT             NULL,
    date_evaluation     DATE            NULL,
    CONSTRAINT PK_fact_evaluations PRIMARY KEY (fact_evaluation_id),
    CONSTRAINT FK_fact_evaluations_dim_collaborateur FOREIGN KEY (collab_key)
        REFERENCES dwh.dim_collaborateur (collab_key),
    CONSTRAINT FK_fact_evaluations_dim_competence FOREIGN KEY (competence_key)
        REFERENCES dwh.dim_competence (competence_key)
);
GO

/* ============================================================================
   3. TEST DATA — 5 fake EY profiles (staging only)
   ============================================================================ */

INSERT INTO staging.stg_collaborateurs
    (collaborateur_id, nom, prenom, email, grade, departement, date_embauche)
VALUES
    (1, 'Benali',    'Yasmine', 'yasmine.benali@ey.com',    'Consultant',     'Audit',              '2023-09-01'),
    (2, 'Traore',    'Moussa',  'moussa.traore@ey.com',     'Consultant',     'Conseil',            '2022-10-15'),
    (3, 'Lefevre',   'Camille', 'camille.lefevre@ey.com',   'Manager',        'Audit',              '2019-03-10'),
    (4, 'El Amrani', 'Karim',   'karim.elamrani@ey.com',    'Manager',        'Tax',                '2018-06-01'),
    (5, 'Dubreuil',  'Aurelie', 'aurelie.dubreuil@ey.com',  'Senior Manager', 'Conseil',            '2014-01-20');
GO
