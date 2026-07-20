-- ============================================================================
-- SIRH.EY - Data Warehouse Star Schema complet
-- Auteur      : Architecture BI - EY Tunisie
-- Date        : Juillet 2026
-- Cible       : SQL Server (LocalDB ou instance dediee)
-- Prerequis   : Base SIRH_EY existante avec donnees seedees
-- ============================================================================
-- Ce script cree :
--   - Schema staging   - 7 tables d'atterrissage (zone brute)
--   - Schema dwh       - 5 dimensions + 3 tables de faits
--
-- Convention de nommage :
--   staging.stg_<entite>      - donnees brutes extraites de l'OLTP
--   dwh.dim_<entite>          - dimension (cle surrogate IDENTITY)
--   dwh.fact_<processus>      - table de faits (mesures + FK dims)
-- ============================================================================

USE SIRH_EY;
GO

-- ============================================================================
-- 0. Creation des schemas
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'staging')
    EXEC('CREATE SCHEMA staging');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dwh')
    EXEC('CREATE SCHEMA dwh');
GO

-- ============================================================================
-- PARTIE 1 : TABLES STAGING (zone d'atterrissage brute)
-- ============================================================================
-- Aucune PK/FK/contrainte - on accepte tout depuis l'OLTP
-- SSIS charge ici en TRUNCATE + INSERT (full refresh)
-- ============================================================================

-- 1.1 Collaborateurs
IF OBJECT_ID('staging.stg_collaborateurs', 'U') IS NOT NULL
    DROP TABLE staging.stg_collaborateurs;

CREATE TABLE staging.stg_collaborateurs (
    collaborateur_id    INT,
    matricule           NVARCHAR(50),
    nom                 NVARCHAR(100),
    prenom              NVARCHAR(100),
    email               NVARCHAR(200),
    departement         NVARCHAR(100),
    sous_departement    NVARCHAR(100),
    poste               NVARCHAR(100),
    grade               NVARCHAR(50),
    date_embauche       DATE,
    anciennete_ans      DECIMAL(5,1),
    statut              NVARCHAR(30),       -- Actif/EnConge/EnPassation/Vacant
    manager_id          INT,
    potentiel_carriere  NVARCHAR(30),       -- High/Medium/Low
    mode_deploiement    NVARCHAR(30),       -- OnPremise/Cloud/Hybride
    nb_implementations  INT,
    exp_domaine_annees  INT,
    business_unit       NVARCHAR(100),
    localisation        NVARCHAR(100),
    type_contrat        NVARCHAR(50),
    actif               BIT,
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

-- 1.2 Competences
IF OBJECT_ID('staging.stg_competences', 'U') IS NOT NULL
    DROP TABLE staging.stg_competences;

CREATE TABLE staging.stg_competences (
    competence_id       INT,
    collaborateur_id    INT,
    nom_competence      NVARCHAR(200),
    categorie           NVARCHAR(100),
    type_competence     NVARCHAR(50),       -- Technique/Fonctionnel/Transverse
    niveau_actuel       INT,                -- 1-5
    niveau_cible        INT,                -- 1-5
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

-- 1.3 Evaluations Talent (9-box)
IF OBJECT_ID('staging.stg_evaluations_talent', 'U') IS NOT NULL
    DROP TABLE staging.stg_evaluations_talent;

CREATE TABLE staging.stg_evaluations_talent (
    evaluation_id       INT,
    collaborateur_id    INT,
    performance_score   INT,                -- 1-5
    potentiel_score     INT,                -- 1-5
    categorie_9box      NVARCHAR(50),       -- Star, FutureLeader, etc.
    categorie_label     NVARCHAR(100),
    source_eval         NVARCHAR(20),       -- manual/computed
    statut_eval         NVARCHAR(30),       -- Draft/Submitted/Calibrated/Approved/Locked
    commentaire_manager NVARCHAR(MAX),
    commentaire_employe NVARCHAR(MAX),
    date_evaluation     DATE,
    review_cycle_id     INT,
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

-- 1.4 Formations & Inscriptions
IF OBJECT_ID('staging.stg_formations', 'U') IS NOT NULL
    DROP TABLE staging.stg_formations;

CREATE TABLE staging.stg_formations (
    formation_id        INT,
    titre               NVARCHAR(300),
    competence_visee    NVARCHAR(200),
    duree_heures        INT,
    capacite_max        INT,
    places_prises       INT,
    plateforme          NVARCHAR(100),
    est_strategique     BIT,
    est_forte_demande   BIT,
    est_certifiante     BIT,
    niveau_difficulte   NVARCHAR(50),
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

IF OBJECT_ID('staging.stg_inscriptions', 'U') IS NOT NULL
    DROP TABLE staging.stg_inscriptions;

CREATE TABLE staging.stg_inscriptions (
    inscription_id      INT,
    collaborateur_id    INT,
    formation_id        INT,
    progression         DECIMAL(5,2),       -- 0-100
    terminee            BIT,
    date_inscription    DATE,
    date_completion     DATE,
    date_expiration     DATE,
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

-- 1.5 Certifications
IF OBJECT_ID('staging.stg_certifications', 'U') IS NOT NULL
    DROP TABLE staging.stg_certifications;

CREATE TABLE staging.stg_certifications (
    collab_certification_id INT,
    collaborateur_id    INT,
    certification_nom   NVARCHAR(200),
    organisme           NVARCHAR(100),
    domaine             NVARCHAR(100),
    statut              NVARCHAR(30),       -- Active/Expiree/En cours
    date_obtention      DATE,
    date_expiration     DATE,
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

-- 1.6 Historique d'evaluation des competences
IF OBJECT_ID('staging.stg_evaluation_historique', 'U') IS NOT NULL
    DROP TABLE staging.stg_evaluation_historique;

CREATE TABLE staging.stg_evaluation_historique (
    historique_id       INT,
    collaborateur_id    INT,
    competence_nom      NVARCHAR(200),
    niveau_ancien       INT,
    niveau_nouveau      INT,
    raison              NVARCHAR(100),      -- Manuel/Formation/Evaluation initiale
    date_changement     DATE,
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

-- 1.7 OKRs
IF OBJECT_ID('staging.stg_okrs', 'U') IS NOT NULL
    DROP TABLE staging.stg_okrs;

CREATE TABLE staging.stg_okrs (
    okr_id              INT,
    collaborateur_id    INT,
    objectif            NVARCHAR(500),
    annee               INT,
    trimestre           NVARCHAR(5),        -- Q1/Q2/Q3/Q4
    statut              NVARCHAR(30),       -- Draft/Active/OnTrack/AtRisk/Completed/Cancelled
    progression_globale INT,                -- 0-100
    date_extraction     DATETIME DEFAULT GETDATE()
);
GO

PRINT N'7 tables staging creees';
GO

-- ============================================================================
-- PARTIE 2 : TABLES DWH - DIMENSIONS (Star Schema)
-- ============================================================================

-- DIM 1 : dim_collaborateur (SCD Type 1)
IF OBJECT_ID('dwh.dim_collaborateur', 'U') IS NOT NULL
    DROP TABLE dwh.dim_collaborateur;

CREATE TABLE dwh.dim_collaborateur (
    collab_key          INT IDENTITY(1,1) PRIMARY KEY,
    collaborateur_id    INT NOT NULL,       -- Cle metier (NK)
    matricule           NVARCHAR(50),
    nom_complet         NVARCHAR(200),
    email               NVARCHAR(200),
    departement         NVARCHAR(100),
    sous_departement    NVARCHAR(100),
    poste               NVARCHAR(100),
    grade               NVARCHAR(50),
    grade_niveau        INT,                -- 1=Junior..6=Partner (derive)
    date_embauche       DATE,
    anciennete_ans      DECIMAL(5,1),
    statut              NVARCHAR(30),
    potentiel_carriere  NVARCHAR(30),
    mode_deploiement    NVARCHAR(30),
    nb_implementations  INT,
    exp_domaine_annees  INT,
    business_unit       NVARCHAR(100),
    localisation        NVARCHAR(100),
    type_contrat        NVARCHAR(50),
    manager_nom         NVARCHAR(200),      -- Denormalise pour BI
    est_actif           BIT DEFAULT 1,
    date_chargement     DATETIME DEFAULT GETDATE(),

    CONSTRAINT UQ_dim_collab_nk UNIQUE (collaborateur_id)
);
GO

-- DIM 2 : dim_competence
IF OBJECT_ID('dwh.dim_competence', 'U') IS NOT NULL
    DROP TABLE dwh.dim_competence;

CREATE TABLE dwh.dim_competence (
    competence_key      INT IDENTITY(1,1) PRIMARY KEY,
    competence_id       INT NOT NULL,       -- NK
    nom_competence      NVARCHAR(200),
    categorie           NVARCHAR(100),
    type_competence     NVARCHAR(50),

    CONSTRAINT UQ_dim_comp_nk UNIQUE (competence_id)
);
GO

-- DIM 3 : dim_formation
IF OBJECT_ID('dwh.dim_formation', 'U') IS NOT NULL
    DROP TABLE dwh.dim_formation;

CREATE TABLE dwh.dim_formation (
    formation_key       INT IDENTITY(1,1) PRIMARY KEY,
    formation_id        INT NOT NULL,       -- NK
    titre               NVARCHAR(300),
    competence_visee    NVARCHAR(200),
    duree_heures        INT,
    plateforme          NVARCHAR(100),
    est_strategique     BIT,
    est_certifiante     BIT,
    niveau_difficulte   NVARCHAR(50),

    CONSTRAINT UQ_dim_form_nk UNIQUE (formation_id)
);
GO

-- DIM 4 : dim_certification
IF OBJECT_ID('dwh.dim_certification', 'U') IS NOT NULL
    DROP TABLE dwh.dim_certification;

CREATE TABLE dwh.dim_certification (
    certification_key   INT IDENTITY(1,1) PRIMARY KEY,
    certification_nom   NVARCHAR(200),      -- NK (pas d'ID dedie)
    organisme           NVARCHAR(100),
    domaine             NVARCHAR(100),

    CONSTRAINT UQ_dim_certif_nk UNIQUE (certification_nom)
);
GO

-- DIM 5 : dim_temps (calendrier)
IF OBJECT_ID('dwh.dim_temps', 'U') IS NOT NULL
    DROP TABLE dwh.dim_temps;

CREATE TABLE dwh.dim_temps (
    date_key            INT PRIMARY KEY,    -- YYYYMMDD
    date_complete       DATE NOT NULL,
    annee               INT,
    trimestre           INT,                -- 1-4
    trimestre_label     NVARCHAR(5),        -- Q1..Q4
    mois                INT,
    mois_label          NVARCHAR(20),       -- Janvier..Decembre
    semaine_iso         INT,
    jour_semaine        INT,                -- 1=Lundi..7=Dimanche
    jour_semaine_label  NVARCHAR(10),
    est_fin_semaine     BIT,
    annee_mois          NVARCHAR(7),        -- 2026-07
    annee_trimestre     NVARCHAR(7)         -- 2026-Q3
);
GO

-- Peuplement dim_temps : 2024-01-01 -> 2028-12-31
;WITH CTE_Dates AS (
    SELECT CAST('2024-01-01' AS DATE) AS dt
    UNION ALL
    SELECT DATEADD(DAY, 1, dt) FROM CTE_Dates WHERE dt < '2028-12-31'
)
INSERT INTO dwh.dim_temps
SELECT
    CAST(FORMAT(dt, 'yyyyMMdd') AS INT)         AS date_key,
    dt                                           AS date_complete,
    YEAR(dt)                                     AS annee,
    DATEPART(QUARTER, dt)                        AS trimestre,
    'Q' + CAST(DATEPART(QUARTER, dt) AS VARCHAR) AS trimestre_label,
    MONTH(dt)                                    AS mois,
    CASE MONTH(dt)
        WHEN 1 THEN N'Janvier' WHEN 2 THEN N'Fevrier' WHEN 3 THEN N'Mars'
        WHEN 4 THEN N'Avril' WHEN 5 THEN N'Mai' WHEN 6 THEN N'Juin'
        WHEN 7 THEN N'Juillet' WHEN 8 THEN N'Aout' WHEN 9 THEN N'Septembre'
        WHEN 10 THEN N'Octobre' WHEN 11 THEN N'Novembre' WHEN 12 THEN N'Decembre'
    END AS mois_label,
    DATEPART(ISO_WEEK, dt)                       AS semaine_iso,
    DATEPART(WEEKDAY, dt)                        AS jour_semaine,
    DATENAME(WEEKDAY, dt)                        AS jour_semaine_label,
    CASE WHEN DATEPART(WEEKDAY, dt) IN (1,7) THEN 1 ELSE 0 END AS est_fin_semaine,
    FORMAT(dt, 'yyyy-MM')                        AS annee_mois,
    FORMAT(dt, 'yyyy') + '-Q' + CAST(DATEPART(QUARTER, dt) AS VARCHAR) AS annee_trimestre
FROM CTE_Dates
OPTION (MAXRECURSION 2000);
GO

PRINT N'5 dimensions creees (dim_temps peuplee : 2024-2028)';
GO

-- ============================================================================
-- PARTIE 3 : TABLES DWH - FAITS
-- ============================================================================

-- FACT 1 : fact_evaluations_talent (grain = 1 evaluation 9-box)
IF OBJECT_ID('dwh.fact_evaluations_talent', 'U') IS NOT NULL
    DROP TABLE dwh.fact_evaluations_talent;

CREATE TABLE dwh.fact_evaluations_talent (
    eval_key            INT IDENTITY(1,1) PRIMARY KEY,
    collab_key          INT NOT NULL REFERENCES dwh.dim_collaborateur(collab_key),
    date_key            INT NOT NULL REFERENCES dwh.dim_temps(date_key),

    -- Mesures
    performance_score   INT,                -- 1-5
    potentiel_score     INT,                -- 1-5
    categorie_9box      NVARCHAR(50),
    source_eval         NVARCHAR(20),
    statut_eval         NVARCHAR(30),

    -- Metriques derivees
    score_composite     DECIMAL(5,2),       -- (perf + pot) / 2
    est_haut_potentiel  BIT,                -- categ IN (Star, FutureLeader, RisingStar)

    date_chargement     DATETIME DEFAULT GETDATE()
);
GO

-- FACT 2 : fact_competences (grain = 1 competence a 1 collaborateur, snapshot)
IF OBJECT_ID('dwh.fact_competences', 'U') IS NOT NULL
    DROP TABLE dwh.fact_competences;

CREATE TABLE dwh.fact_competences (
    fact_comp_key       INT IDENTITY(1,1) PRIMARY KEY,
    collab_key          INT NOT NULL REFERENCES dwh.dim_collaborateur(collab_key),
    competence_key      INT NOT NULL REFERENCES dwh.dim_competence(competence_key),
    date_key            INT NOT NULL REFERENCES dwh.dim_temps(date_key),

    -- Mesures
    niveau_actuel       INT,                -- 1-5
    niveau_cible        INT,                -- 1-5
    gap                 INT,                -- cible - actuel (calcule a l'ETL)
    est_maitrisee       BIT,                -- niveau_actuel >= niveau_cible

    date_chargement     DATETIME DEFAULT GETDATE()
);
GO

-- FACT 3 : fact_formations (grain = 1 inscription = 1 collaborateur a 1 formation)
IF OBJECT_ID('dwh.fact_formations', 'U') IS NOT NULL
    DROP TABLE dwh.fact_formations;

CREATE TABLE dwh.fact_formations (
    fact_form_key       INT IDENTITY(1,1) PRIMARY KEY,
    collab_key          INT NOT NULL REFERENCES dwh.dim_collaborateur(collab_key),
    formation_key       INT NOT NULL REFERENCES dwh.dim_formation(formation_key),
    date_inscription_key INT REFERENCES dwh.dim_temps(date_key),
    date_completion_key  INT REFERENCES dwh.dim_temps(date_key),

    -- Mesures
    progression         DECIMAL(5,2),       -- 0-100
    est_terminee        BIT,
    duree_heures        INT,                -- denormalise pour agregation rapide

    date_chargement     DATETIME DEFAULT GETDATE()
);
GO

PRINT N'3 tables de faits creees';
GO

-- ============================================================================
-- PARTIE 4 : PROCEDURES D'EXTRACTION OLTP -> STAGING
-- ============================================================================
-- Ces SP sont appelees par SSIS via "SQL Command" dans OLE DB Source
-- Elles appliquent les transformations legeres (concatenation, cast, coalesce)
-- ============================================================================

-- SP 1 : Extraction collaborateurs
IF OBJECT_ID('staging.usp_Extract_Collaborateurs', 'P') IS NOT NULL
    DROP PROCEDURE staging.usp_Extract_Collaborateurs;
GO

CREATE PROCEDURE staging.usp_Extract_Collaborateurs
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE staging.stg_collaborateurs;

    INSERT INTO staging.stg_collaborateurs (
        collaborateur_id, matricule, nom, prenom, email,
        departement, sous_departement, poste, grade,
        date_embauche, anciennete_ans, statut, manager_id,
        potentiel_carriere, mode_deploiement, nb_implementations,
        exp_domaine_annees, business_unit, localisation, type_contrat, actif
    )
    SELECT
        c.Id,
        c.Matricule,
        c.Nom,
        c.Prenom,
        c.Email,
        COALESCE(d.Nom, c.Departement, N'Non assigne'),
        sd.Nom,
        COALESCE(p.Nom, c.Poste, N'Non defini'),
        COALESCE(g.Nom, c.Grade, N'Junior'),
        c.DateEmbauche,
        CASE
            WHEN c.DateEmbauche IS NOT NULL
            THEN ROUND(DATEDIFF(DAY, c.DateEmbauche, GETDATE()) / 365.25, 1)
            ELSE 0
        END,
        CASE c.Statut
            WHEN 0 THEN 'Actif'
            WHEN 1 THEN 'EnConge'
            WHEN 2 THEN 'EnPassation'
            WHEN 3 THEN 'Vacant'
            ELSE 'Actif'
        END,
        c.ManagerId,
        c.PotentielCarriere,
        CASE c.ModeDeploiement
            WHEN 0 THEN 'OnPremise'
            WHEN 1 THEN 'Cloud'
            WHEN 2 THEN 'Hybride'
            ELSE NULL
        END,
        c.NombreImplementations,
        c.ExperienceDomainAnnees,
        bu.Nom,
        loc.Nom,
        ct.Nom,
        c.Actif
    FROM dbo.Collaborateurs c
    LEFT JOIN dbo.Departments d      ON c.DepartmentId = d.Id
    LEFT JOIN dbo.SubDepartments sd  ON c.SubDepartmentId = sd.Id
    LEFT JOIN dbo.Positions p        ON c.PositionId = p.Id
    LEFT JOIN dbo.GradeEntities g    ON c.GradeId = g.Id
    LEFT JOIN dbo.BusinessUnits bu   ON c.BusinessUnitId = bu.Id
    LEFT JOIN dbo.Locations loc      ON c.LocationId = loc.Id
    LEFT JOIN dbo.ContractTypes ct   ON c.ContractTypeId = ct.Id
    WHERE c.Actif = 1;
END;
GO

-- SP 2 : Extraction competences
IF OBJECT_ID('staging.usp_Extract_Competences', 'P') IS NOT NULL
    DROP PROCEDURE staging.usp_Extract_Competences;
GO

CREATE PROCEDURE staging.usp_Extract_Competences
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE staging.stg_competences;

    INSERT INTO staging.stg_competences (
        competence_id, collaborateur_id, nom_competence,
        categorie, type_competence, niveau_actuel, niveau_cible
    )
    SELECT
        c.Id,
        c.CollaborateurId,
        c.Nom,
        cat.Nom,
        CASE
            WHEN cat.Nom LIKE '%Tech%' OR cat.Nom LIKE '%CRM%' OR cat.Nom LIKE '%D365%' THEN 'Technique'
            WHEN cat.Nom LIKE '%Manage%' OR cat.Nom LIKE '%Leader%' THEN 'Transverse'
            ELSE 'Fonctionnel'
        END,
        c.NiveauActuel,
        c.NiveauCible
    FROM dbo.Competences c
    LEFT JOIN dbo.CategorieCompetences cat ON c.CategorieCompetenceId = cat.Id
    INNER JOIN dbo.Collaborateurs col ON c.CollaborateurId = col.Id AND col.Actif = 1;
END;
GO

-- SP 3 : Extraction evaluations talent
IF OBJECT_ID('staging.usp_Extract_EvaluationsTalent', 'P') IS NOT NULL
    DROP PROCEDURE staging.usp_Extract_EvaluationsTalent;
GO

CREATE PROCEDURE staging.usp_Extract_EvaluationsTalent
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE staging.stg_evaluations_talent;

    INSERT INTO staging.stg_evaluations_talent (
        evaluation_id, collaborateur_id, performance_score, potentiel_score,
        categorie_9box, categorie_label, source_eval, statut_eval,
        commentaire_manager, commentaire_employe, date_evaluation, review_cycle_id
    )
    SELECT
        te.Id,
        te.CollaborateurId,
        te.PerformanceScore,
        te.PotentielScore,
        CASE te.Category
            WHEN 0 THEN 'Star'
            WHEN 1 THEN 'FutureLeader'
            WHEN 2 THEN 'HighProfessional'
            WHEN 3 THEN 'EmergingTalent'
            WHEN 4 THEN 'SolidProfessional'
            WHEN 5 THEN 'InPlace'
            WHEN 6 THEN 'RisingStar'
            WHEN 7 THEN 'NeedDevelopment'
            WHEN 8 THEN 'Underperformer'
            ELSE 'Unknown'
        END,
        CASE te.Category
            WHEN 0 THEN N'Talent strategique'
            WHEN 1 THEN N'Leader strategique'
            WHEN 2 THEN N'Expert metier'
            WHEN 3 THEN N'Potentiel emergent'
            WHEN 4 THEN N'Collaborateur cle'
            WHEN 5 THEN N'Stable dans le poste'
            WHEN 6 THEN N'Haut potentiel'
            WHEN 7 THEN N'Besoin accompagnement'
            WHEN 8 THEN N'Performance insuffisante'
            ELSE 'N/A'
        END,
        'manual',
        CASE te.Statut
            WHEN 0 THEN 'Draft'
            WHEN 1 THEN 'Submitted'
            WHEN 2 THEN 'Calibrated'
            WHEN 3 THEN 'Approved'
            WHEN 4 THEN 'Locked'
            ELSE 'Draft'
        END,
        te.CommentairesPerformance,
        te.CommentairesPotentiel,
        CAST(te.DateEvaluation AS DATE),
        te.ReviewCycleId
    FROM dbo.TalentEvaluations te
    INNER JOIN dbo.Collaborateurs col ON te.CollaborateurId = col.Id AND col.Actif = 1;
END;
GO

-- SP 4 : Extraction formations
IF OBJECT_ID('staging.usp_Extract_Formations', 'P') IS NOT NULL
    DROP PROCEDURE staging.usp_Extract_Formations;
GO

CREATE PROCEDURE staging.usp_Extract_Formations
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE staging.stg_formations;

    INSERT INTO staging.stg_formations (
        formation_id, titre, competence_visee, duree_heures,
        capacite_max, places_prises, plateforme,
        est_strategique, est_forte_demande, est_certifiante, niveau_difficulte
    )
    SELECT
        Id, Titre, CompetenceVisee, DureeHeures,
        CapaciteMax, PlacesPrises, Plateforme,
        ISNULL(EstStrategique, 0),
        ISNULL(EstForteDemande, 0),
        CASE WHEN CertificationNom IS NOT NULL AND CertificationNom <> '' THEN 1 ELSE 0 END,
        NiveauDifficulte
    FROM dbo.Formations;
END;
GO

-- SP 5 : Extraction inscriptions
IF OBJECT_ID('staging.usp_Extract_Inscriptions', 'P') IS NOT NULL
    DROP PROCEDURE staging.usp_Extract_Inscriptions;
GO

CREATE PROCEDURE staging.usp_Extract_Inscriptions
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE staging.stg_inscriptions;

    INSERT INTO staging.stg_inscriptions (
        inscription_id, collaborateur_id, formation_id,
        progression, terminee, date_inscription, date_completion, date_expiration
    )
    SELECT
        i.Id,
        i.CollaborateurId,
        i.FormationId,
        i.Progression,
        i.Terminee,
        CAST(i.DateInscription AS DATE),
        CAST(i.DateCompletion AS DATE),
        CAST(i.DateExpiration AS DATE)
    FROM dbo.Inscriptions i
    INNER JOIN dbo.Collaborateurs c ON i.CollaborateurId = c.Id AND c.Actif = 1;
END;
GO

-- SP 6 : Extraction certifications
IF OBJECT_ID('staging.usp_Extract_Certifications', 'P') IS NOT NULL
    DROP PROCEDURE staging.usp_Extract_Certifications;
GO

CREATE PROCEDURE staging.usp_Extract_Certifications
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE staging.stg_certifications;

    INSERT INTO staging.stg_certifications (
        collab_certification_id, collaborateur_id, certification_nom,
        organisme, domaine, statut, date_obtention, date_expiration
    )
    SELECT
        cc.Id,
        cc.CollaborateurId,
        cert.Nom,
        cert.Organisme,
        cert.Domaine,
        cc.Statut,
        CAST(cc.DateObtention AS DATE),
        CAST(cc.DateExpiration AS DATE)
    FROM dbo.CollaborateurCertifications cc
    INNER JOIN dbo.Certifications cert ON cc.CertificationId = cert.Id
    INNER JOIN dbo.Collaborateurs c ON cc.CollaborateurId = c.Id AND c.Actif = 1;
END;
GO

PRINT N'6 procedures d''extraction creees';
GO

-- ============================================================================
-- PARTIE 5 : PROCEDURES DE CHARGEMENT STAGING -> DWH
-- ============================================================================
-- Ces SP sont appelees par SSIS apres l'etape Extract
-- Elles implementent la logique MERGE (upsert) pour l'incremental
-- ============================================================================

-- LOAD dim_collaborateur (MERGE sur collaborateur_id)
IF OBJECT_ID('dwh.usp_Load_DimCollaborateur', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_DimCollaborateur;
GO

CREATE PROCEDURE dwh.usp_Load_DimCollaborateur
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dwh.dim_collaborateur AS target
    USING (
        SELECT
            s.collaborateur_id,
            s.matricule,
            RTRIM(LTRIM(COALESCE(s.prenom, '') + ' ' + COALESCE(s.nom, ''))) AS nom_complet,
            s.email,
            s.departement,
            s.sous_departement,
            s.poste,
            s.grade,
            CASE s.grade
                WHEN 'Junior' THEN 1
                WHEN 'Senior' THEN 2
                WHEN 'Manager' THEN 3
                WHEN 'Senior Manager' THEN 4
                WHEN 'Director' THEN 5
                WHEN 'Partner' THEN 6
                ELSE 0
            END AS grade_niveau,
            s.date_embauche,
            s.anciennete_ans,
            s.statut,
            s.potentiel_carriere,
            s.mode_deploiement,
            s.nb_implementations,
            s.exp_domaine_annees,
            s.business_unit,
            s.localisation,
            s.type_contrat,
            mgr.prenom + ' ' + mgr.nom AS manager_nom,
            s.actif
        FROM staging.stg_collaborateurs s
        LEFT JOIN staging.stg_collaborateurs mgr ON s.manager_id = mgr.collaborateur_id
    ) AS source
    ON target.collaborateur_id = source.collaborateur_id

    WHEN MATCHED THEN UPDATE SET
        target.matricule           = source.matricule,
        target.nom_complet         = source.nom_complet,
        target.email               = source.email,
        target.departement         = source.departement,
        target.sous_departement    = source.sous_departement,
        target.poste               = source.poste,
        target.grade               = source.grade,
        target.grade_niveau        = source.grade_niveau,
        target.date_embauche       = source.date_embauche,
        target.anciennete_ans      = source.anciennete_ans,
        target.statut              = source.statut,
        target.potentiel_carriere  = source.potentiel_carriere,
        target.mode_deploiement    = source.mode_deploiement,
        target.nb_implementations  = source.nb_implementations,
        target.exp_domaine_annees  = source.exp_domaine_annees,
        target.business_unit       = source.business_unit,
        target.localisation        = source.localisation,
        target.type_contrat        = source.type_contrat,
        target.manager_nom         = source.manager_nom,
        target.est_actif           = source.actif,
        target.date_chargement     = GETDATE()

    WHEN NOT MATCHED BY TARGET THEN INSERT (
        collaborateur_id, matricule, nom_complet, email,
        departement, sous_departement, poste, grade, grade_niveau,
        date_embauche, anciennete_ans, statut, potentiel_carriere,
        mode_deploiement, nb_implementations, exp_domaine_annees,
        business_unit, localisation, type_contrat, manager_nom, est_actif
    ) VALUES (
        source.collaborateur_id, source.matricule, source.nom_complet, source.email,
        source.departement, source.sous_departement, source.poste, source.grade, source.grade_niveau,
        source.date_embauche, source.anciennete_ans, source.statut, source.potentiel_carriere,
        source.mode_deploiement, source.nb_implementations, source.exp_domaine_annees,
        source.business_unit, source.localisation, source.type_contrat, source.manager_nom, source.actif
    );

    PRINT N'dim_collaborateur : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes traitees';
END;
GO

-- LOAD dim_competence
IF OBJECT_ID('dwh.usp_Load_DimCompetence', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_DimCompetence;
GO

CREATE PROCEDURE dwh.usp_Load_DimCompetence
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dwh.dim_competence AS target
    USING (
        SELECT DISTINCT competence_id, nom_competence, categorie, type_competence
        FROM staging.stg_competences
    ) AS source
    ON target.competence_id = source.competence_id

    WHEN MATCHED THEN UPDATE SET
        target.nom_competence  = source.nom_competence,
        target.categorie       = source.categorie,
        target.type_competence = source.type_competence

    WHEN NOT MATCHED BY TARGET THEN INSERT (
        competence_id, nom_competence, categorie, type_competence
    ) VALUES (
        source.competence_id, source.nom_competence, source.categorie, source.type_competence
    );

    PRINT N'dim_competence : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes traitees';
END;
GO

-- LOAD dim_formation
IF OBJECT_ID('dwh.usp_Load_DimFormation', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_DimFormation;
GO

CREATE PROCEDURE dwh.usp_Load_DimFormation
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dwh.dim_formation AS target
    USING (
        SELECT formation_id, titre, competence_visee, duree_heures,
               plateforme, est_strategique, est_certifiante, niveau_difficulte
        FROM staging.stg_formations
    ) AS source
    ON target.formation_id = source.formation_id

    WHEN MATCHED THEN UPDATE SET
        target.titre             = source.titre,
        target.competence_visee  = source.competence_visee,
        target.duree_heures      = source.duree_heures,
        target.plateforme        = source.plateforme,
        target.est_strategique   = source.est_strategique,
        target.est_certifiante   = source.est_certifiante,
        target.niveau_difficulte = source.niveau_difficulte

    WHEN NOT MATCHED BY TARGET THEN INSERT (
        formation_id, titre, competence_visee, duree_heures,
        plateforme, est_strategique, est_certifiante, niveau_difficulte
    ) VALUES (
        source.formation_id, source.titre, source.competence_visee, source.duree_heures,
        source.plateforme, source.est_strategique, source.est_certifiante, source.niveau_difficulte
    );

    PRINT N'dim_formation : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes traitees';
END;
GO

-- LOAD dim_certification
IF OBJECT_ID('dwh.usp_Load_DimCertification', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_DimCertification;
GO

CREATE PROCEDURE dwh.usp_Load_DimCertification
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dwh.dim_certification AS target
    USING (
        SELECT DISTINCT certification_nom, organisme, domaine
        FROM staging.stg_certifications
        WHERE certification_nom IS NOT NULL
    ) AS source
    ON target.certification_nom = source.certification_nom

    WHEN MATCHED THEN UPDATE SET
        target.organisme = source.organisme,
        target.domaine   = source.domaine

    WHEN NOT MATCHED BY TARGET THEN INSERT (
        certification_nom, organisme, domaine
    ) VALUES (
        source.certification_nom, source.organisme, source.domaine
    );

    PRINT N'dim_certification : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes traitees';
END;
GO

-- LOAD fact_evaluations_talent
IF OBJECT_ID('dwh.usp_Load_FactEvaluations', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_FactEvaluations;
GO

CREATE PROCEDURE dwh.usp_Load_FactEvaluations
AS
BEGIN
    SET NOCOUNT ON;

    -- Full refresh sur la table de faits
    TRUNCATE TABLE dwh.fact_evaluations_talent;

    INSERT INTO dwh.fact_evaluations_talent (
        collab_key, date_key,
        performance_score, potentiel_score, categorie_9box,
        source_eval, statut_eval, score_composite, est_haut_potentiel
    )
    SELECT
        dc.collab_key,
        CAST(FORMAT(s.date_evaluation, 'yyyyMMdd') AS INT),
        s.performance_score,
        s.potentiel_score,
        s.categorie_9box,
        s.source_eval,
        s.statut_eval,
        (CAST(s.performance_score AS DECIMAL) + s.potentiel_score) / 2.0,
        CASE WHEN s.categorie_9box IN ('Star','FutureLeader','RisingStar') THEN 1 ELSE 0 END
    FROM staging.stg_evaluations_talent s
    INNER JOIN dwh.dim_collaborateur dc ON s.collaborateur_id = dc.collaborateur_id
    WHERE s.date_evaluation IS NOT NULL;

    PRINT N'fact_evaluations_talent : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes chargees';
END;
GO

-- LOAD fact_competences
IF OBJECT_ID('dwh.usp_Load_FactCompetences', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_FactCompetences;
GO

CREATE PROCEDURE dwh.usp_Load_FactCompetences
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dwh.fact_competences;

    INSERT INTO dwh.fact_competences (
        collab_key, competence_key, date_key,
        niveau_actuel, niveau_cible, gap, est_maitrisee
    )
    SELECT
        dc.collab_key,
        dcomp.competence_key,
        CAST(FORMAT(GETDATE(), 'yyyyMMdd') AS INT),  -- snapshot du jour
        s.niveau_actuel,
        s.niveau_cible,
        s.niveau_cible - s.niveau_actuel,
        CASE WHEN s.niveau_actuel >= s.niveau_cible THEN 1 ELSE 0 END
    FROM staging.stg_competences s
    INNER JOIN dwh.dim_collaborateur dc ON s.collaborateur_id = dc.collaborateur_id
    INNER JOIN dwh.dim_competence dcomp ON s.competence_id = dcomp.competence_id;

    PRINT N'fact_competences : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes chargees';
END;
GO

-- LOAD fact_formations
IF OBJECT_ID('dwh.usp_Load_FactFormations', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_Load_FactFormations;
GO

CREATE PROCEDURE dwh.usp_Load_FactFormations
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE dwh.fact_formations;

    INSERT INTO dwh.fact_formations (
        collab_key, formation_key, date_inscription_key, date_completion_key,
        progression, est_terminee, duree_heures
    )
    SELECT
        dc.collab_key,
        df.formation_key,
        CASE WHEN s.date_inscription IS NOT NULL
             THEN CAST(FORMAT(s.date_inscription, 'yyyyMMdd') AS INT)
             ELSE NULL END,
        CASE WHEN s.date_completion IS NOT NULL
             THEN CAST(FORMAT(s.date_completion, 'yyyyMMdd') AS INT)
             ELSE NULL END,
        s.progression,
        s.terminee,
        sf.duree_heures
    FROM staging.stg_inscriptions s
    INNER JOIN dwh.dim_collaborateur dc ON s.collaborateur_id = dc.collaborateur_id
    INNER JOIN dwh.dim_formation df ON s.formation_id = df.formation_id
    INNER JOIN staging.stg_formations sf ON s.formation_id = sf.formation_id;

    PRINT N'fact_formations : ' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes chargees';
END;
GO

-- ============================================================================
-- PARTIE 6 : PROCEDURE MAITRE D'ORCHESTRATION
-- ============================================================================
-- Appelee par un Execute SQL Task SSIS unique
-- Ordre : Extract -> Load Dims -> Load Facts
-- ============================================================================

IF OBJECT_ID('dwh.usp_ETL_FullRefresh', 'P') IS NOT NULL
    DROP PROCEDURE dwh.usp_ETL_FullRefresh;
GO

CREATE PROCEDURE dwh.usp_ETL_FullRefresh
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @start DATETIME = GETDATE();

    PRINT N'=== ETL SIRH.EY - Debut : ' + CONVERT(VARCHAR, @start, 120) + N' ===';

    -- Phase 1 : Extraction OLTP -> Staging
    PRINT N'>> Phase 1 : Extraction';
    EXEC staging.usp_Extract_Collaborateurs;
    EXEC staging.usp_Extract_Competences;
    EXEC staging.usp_Extract_EvaluationsTalent;
    EXEC staging.usp_Extract_Formations;
    EXEC staging.usp_Extract_Inscriptions;
    EXEC staging.usp_Extract_Certifications;

    -- Phase 2 : Chargement Dimensions (MERGE)
    PRINT N'>> Phase 2 : Dimensions';
    EXEC dwh.usp_Load_DimCollaborateur;
    EXEC dwh.usp_Load_DimCompetence;
    EXEC dwh.usp_Load_DimFormation;
    EXEC dwh.usp_Load_DimCertification;
    -- dim_temps deja peuplee statiquement

    -- Phase 3 : Chargement Faits
    PRINT N'>> Phase 3 : Faits';
    EXEC dwh.usp_Load_FactEvaluations;
    EXEC dwh.usp_Load_FactCompetences;
    EXEC dwh.usp_Load_FactFormations;

    PRINT N'=== ETL termine en ' + CAST(DATEDIFF(SECOND, @start, GETDATE()) AS VARCHAR) + N' secondes ===';
END;
GO

PRINT N'';
PRINT N'============================================================';
PRINT N'  SIRH.EY Data Warehouse - Installation terminee';
PRINT N'  Schema staging : 7 tables, 6 procedures d''extraction';
PRINT N'  Schema dwh     : 5 dimensions, 3 faits, 7 proc. chargement';
PRINT N'  Executer : EXEC dwh.usp_ETL_FullRefresh';
PRINT N'============================================================';
GO
