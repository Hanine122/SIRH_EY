-- ============================================================================
-- SIRH.EY - Architecture BI - 003_DataWarehouse.sql
-- Star schema physique : schemas techniques dim / fact (anglais, conforme aux
-- conventions BI) ; TOUS les noms de tables, colonnes et commentaires sont en
-- francais, pour une restitution Power BI entierement francophone (demo PFE).
--
-- Voir RAPPORT_AUDIT.md pour le detail des ecarts Kimball corriges dans cette
-- version : ajout d'une date de snapshot sur fact.Promotion (fait
-- periodique sans date = non conforme Kimball), contrainte CHECK de
-- coherence SCD2 sur dim.Collaborateur, index sur toutes les colonnes de
-- cle etrangere des faits (absents de la version precedente).
--
-- SCD :
--   - dim.Collaborateur en SCD Type 2 (DateEffective/DateFin/EstVersionCourante) :
--     seule dimension ou l'historique (grade/poste/service au fil du temps)
--     a une vraie valeur analytique.
--   - Toutes les autres dimensions en SCD Type 1 (recree a chaque rechargement).
--   - Simplification assumee (documentee dans RAPPORT_AUDIT.md) : chaque fait
--     est lie a la version COURANTE du collaborateur, pas a sa version en
--     vigueur a la date du fait.
--
-- Regles respectees : dbo jamais touche ; source = stg uniquement.
-- ============================================================================

USE SIRH_EY;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dim')
    EXEC('CREATE SCHEMA dim');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'fact')
    EXEC('CREATE SCHEMA fact');
GO

-- Nettoyage de l'ancienne version anglaise de ce meme star schema, si
-- presente (cree par une iteration precedente de ce travail, avant la
-- francisation demandee -- pas une table "existante" au sens metier, donc
-- pas couverte par la regle "ne jamais recreer l'existant").
DECLARE @sqlDropOld NVARCHAR(MAX) = N'';
SELECT @sqlDropOld = @sqlDropOld + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) IN ('dim', 'fact');
EXEC sp_executesql @sqlDropOld;
GO
DROP TABLE IF EXISTS fact.Skills, fact.Training, fact.Talent, fact.Promotion, fact.Succession;
DROP TABLE IF EXISTS dim.Skill, dim.BusinessUnit, dim.Location, dim.ReviewCycle, dim.[Date];
GO

-- ============================================================================
-- 1. DIMENSIONS
-- ============================================================================

IF OBJECT_ID('dim.Calendrier', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Calendrier (
        CleDate       INT         NOT NULL PRIMARY KEY,   -- aaaammjj
        [Date]        DATE        NOT NULL,
        Annee         INT         NOT NULL,
        Trimestre     INT         NOT NULL,
        LibelleTrimestre NVARCHAR(5) NOT NULL,
        Mois          INT         NOT NULL,
        NomMois       NVARCHAR(20) NOT NULL,
        Semaine       INT         NOT NULL,
        JourSemaine   INT         NOT NULL,
        NomJour       NVARCHAR(10) NOT NULL,
        EstWeekEnd    BIT         NOT NULL
    );
    PRINT N'Cree : dim.Calendrier';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Calendrier';
GO

IF OBJECT_ID('dim.Collaborateur', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Collaborateur (
        CleCollaborateur            INT IDENTITY(1,1) PRIMARY KEY,
        CollaborateurId              INT NOT NULL,           -- cle metier (dbo.Collaborateurs.Id)
        Nom                          NVARCHAR(100)   NULL,
        Prenom                       NVARCHAR(100)   NULL,
        Actif                        BIT             NOT NULL,
        Statut                       INT             NULL,
        DateEmbauche                 DATE            NULL,
        PotentielCarriere            NVARCHAR(30)    NULL,
        NiveauPreparationSuccession  INT             NULL,
        ManagerId                    INT             NULL,
        PositionId                   INT             NULL,
        GradeId                      INT             NULL,
        UniteAffairesId              INT             NULL,
        LocalisationId               INT             NULL,
        DateEffective                DATE            NOT NULL,
        DateFin                      DATE            NULL,
        EstVersionCourante           BIT             NOT NULL DEFAULT 1,
        CONSTRAINT CK_dim_Collaborateur_SCD2 CHECK (
            (EstVersionCourante = 1 AND DateFin IS NULL) OR
            (EstVersionCourante = 0 AND DateFin IS NOT NULL)
        )
    );
    CREATE INDEX IX_dim_Collaborateur_CleMetier_Courante ON dim.Collaborateur (CollaborateurId, EstVersionCourante);
    PRINT N'Cree : dim.Collaborateur';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Collaborateur';
GO

IF OBJECT_ID('dim.Organisation', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Organisation (
        CleOrganisation      INT IDENTITY(1,1) PRIMARY KEY,
        PositionId           INT NOT NULL,       -- cle metier
        NomPoste             NVARCHAR(100)   NULL,
        SousDepartementId    INT             NULL,
        NomSousDepartement   NVARCHAR(100)   NULL,
        DepartementId        INT             NULL,
        NomDepartement       NVARCHAR(100)   NULL,
        CONSTRAINT UQ_dim_Organisation_CleMetier UNIQUE (PositionId)
    );
    PRINT N'Cree : dim.Organisation';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Organisation';
GO

IF OBJECT_ID('dim.Grade', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Grade (
        CleGrade                 INT IDENTITY(1,1) PRIMARY KEY,
        GradeId                  INT NOT NULL,      -- cle metier
        Nom                      NVARCHAR(50)    NULL,
        Niveau                   INT             NULL,
        NiveauMinCompetences     FLOAT           NULL,
        AncienneteMinAns         INT             NULL,
        NombreImplementationsMin INT             NULL,
        ExperienceDomainMinAns   INT             NULL,
        GradeSuivant             NVARCHAR(50)    NULL,
        CONSTRAINT UQ_dim_Grade_CleMetier UNIQUE (GradeId)
    );
    PRINT N'Cree : dim.Grade';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Grade';
GO

IF OBJECT_ID('dim.UniteAffaires', 'U') IS NULL
BEGIN
    CREATE TABLE dim.UniteAffaires (
        CleUniteAffaires  INT IDENTITY(1,1) PRIMARY KEY,
        UniteAffairesId   INT NOT NULL,     -- cle metier
        Nom               NVARCHAR(100)   NULL,
        CONSTRAINT UQ_dim_UniteAffaires_CleMetier UNIQUE (UniteAffairesId)
    );
    PRINT N'Cree : dim.UniteAffaires';
END
ELSE PRINT N'EXISTANT - reutilise : dim.UniteAffaires';
GO

IF OBJECT_ID('dim.Localisation', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Localisation (
        CleLocalisation  INT IDENTITY(1,1) PRIMARY KEY,
        LocalisationId   INT NOT NULL,     -- cle metier
        Nom              NVARCHAR(100)   NULL,
        Ville            NVARCHAR(100)   NULL,
        Pays             NVARCHAR(100)   NULL,
        CONSTRAINT UQ_dim_Localisation_CleMetier UNIQUE (LocalisationId)
    );
    PRINT N'Cree : dim.Localisation';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Localisation';
GO

IF OBJECT_ID('dim.Competence', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Competence (
        CleCompetence      INT IDENTITY(1,1) PRIMARY KEY,
        CompetenceId       INT NOT NULL,      -- cle metier (dbo.Skills.Id - catalogue)
        Nom                NVARCHAR(150)   NULL,
        NomCategorie       NVARCHAR(100)   NULL,
        CodeCriticite      INT             NULL,
        LibelleCriticite   NVARCHAR(20)    NULL,
        PoidsCriticite     INT             NOT NULL DEFAULT 2,
        CONSTRAINT UQ_dim_Competence_CleMetier UNIQUE (CompetenceId)
    );
    PRINT N'Cree : dim.Competence';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Competence';
GO

IF OBJECT_ID('dim.Formation', 'U') IS NULL
BEGIN
    CREATE TABLE dim.Formation (
        CleFormation     INT IDENTITY(1,1) PRIMARY KEY,
        FormationId      INT NOT NULL,      -- cle metier
        Titre            NVARCHAR(300)   NULL,
        Categorie        NVARCHAR(100)   NULL,
        Plateforme       NVARCHAR(100)   NULL,
        EstCertifiante   BIT             NULL,
        CapaciteMax      INT             NULL,
        CONSTRAINT UQ_dim_Formation_CleMetier UNIQUE (FormationId)
    );
    PRINT N'Cree : dim.Formation';
END
ELSE PRINT N'EXISTANT - reutilise : dim.Formation';
GO

IF OBJECT_ID('dim.CycleEvaluation', 'U') IS NULL
BEGIN
    CREATE TABLE dim.CycleEvaluation (
        CleCycleEvaluation  INT IDENTITY(1,1) PRIMARY KEY,
        CycleEvaluationId   INT NOT NULL,      -- cle metier
        Nom                 NVARCHAR(100)   NULL,
        DateDebut           DATE            NULL,
        DateFin             DATE            NULL,
        Statut              INT             NULL,
        CONSTRAINT UQ_dim_CycleEvaluation_CleMetier UNIQUE (CycleEvaluationId)
    );
    PRINT N'Cree : dim.CycleEvaluation';
END
ELSE PRINT N'EXISTANT - reutilise : dim.CycleEvaluation';
GO

-- ============================================================================
-- 2. FAITS
-- ============================================================================

IF OBJECT_ID('fact.EvaluationCompetences', 'U') IS NULL
BEGIN
    CREATE TABLE fact.EvaluationCompetences (
        CleEvaluationCompetence  INT IDENTITY(1,1) PRIMARY KEY,
        CompetenceEvalueeId      INT NOT NULL,          -- cle metier (dbo.Competences.Id)
        CleCollaborateur         INT NOT NULL,
        CleCompetence            INT NULL,
        CleDate                  INT NOT NULL,
        NiveauActuel             INT NULL,
        NiveauCible              INT NULL,
        Ecart                    INT NULL,
        AtteintCible             BIT NULL,
        SeveriteEcart            NVARCHAR(20) NULL,
        CONSTRAINT UQ_fact_EvaluationCompetences_CleMetier UNIQUE (CompetenceEvalueeId),
        CONSTRAINT FK_fact_EvalComp_Collaborateur FOREIGN KEY (CleCollaborateur) REFERENCES dim.Collaborateur (CleCollaborateur),
        CONSTRAINT FK_fact_EvalComp_Competence     FOREIGN KEY (CleCompetence)    REFERENCES dim.Competence (CleCompetence),
        CONSTRAINT FK_fact_EvalComp_Date            FOREIGN KEY (CleDate)          REFERENCES dim.Calendrier (CleDate)
    );
    CREATE INDEX IX_fact_EvalComp_CleCollaborateur ON fact.EvaluationCompetences (CleCollaborateur);
    CREATE INDEX IX_fact_EvalComp_CleCompetence    ON fact.EvaluationCompetences (CleCompetence);
    CREATE INDEX IX_fact_EvalComp_CleDate           ON fact.EvaluationCompetences (CleDate);
    PRINT N'Cree : fact.EvaluationCompetences';
END
ELSE PRINT N'EXISTANT - reutilise : fact.EvaluationCompetences';
GO

IF OBJECT_ID('fact.Formation', 'U') IS NULL
BEGIN
    CREATE TABLE fact.Formation (
        CleInscriptionFait   INT IDENTITY(1,1) PRIMARY KEY,
        InscriptionId        INT NOT NULL,        -- cle metier
        CleCollaborateur     INT NOT NULL,
        CleFormation         INT NOT NULL,
        CleDateInscription   INT NOT NULL,
        CleDateCompletion    INT NULL,
        Terminee             BIT NULL,
        Progression          INT NULL,
        NoteGlobaleChaud     INT NULL,
        NoteContenuChaud     INT NULL,
        NoteFormateurChaud   INT NULL,
        RecommandeChaud      BIT NULL,
        NoteApplicationFroid INT NULL,
        NoteImpactBusinessFroid INT NULL,
        TauxUtilisationCapacite FLOAT NULL,
        CONSTRAINT UQ_fact_Formation_CleMetier UNIQUE (InscriptionId),
        CONSTRAINT FK_fact_Formation_Collaborateur    FOREIGN KEY (CleCollaborateur)   REFERENCES dim.Collaborateur (CleCollaborateur),
        CONSTRAINT FK_fact_Formation_Formation         FOREIGN KEY (CleFormation)        REFERENCES dim.Formation (CleFormation),
        CONSTRAINT FK_fact_Formation_DateInscription    FOREIGN KEY (CleDateInscription)  REFERENCES dim.Calendrier (CleDate),
        CONSTRAINT FK_fact_Formation_DateCompletion      FOREIGN KEY (CleDateCompletion)    REFERENCES dim.Calendrier (CleDate)
    );
    CREATE INDEX IX_fact_Formation_CleCollaborateur   ON fact.Formation (CleCollaborateur);
    CREATE INDEX IX_fact_Formation_CleFormation        ON fact.Formation (CleFormation);
    CREATE INDEX IX_fact_Formation_CleDateInscription  ON fact.Formation (CleDateInscription);
    CREATE INDEX IX_fact_Formation_CleDateCompletion    ON fact.Formation (CleDateCompletion);
    PRINT N'Cree : fact.Formation';
END
ELSE PRINT N'EXISTANT - reutilise : fact.Formation';
GO

IF OBJECT_ID('fact.EvaluationTalent', 'U') IS NULL
BEGIN
    CREATE TABLE fact.EvaluationTalent (
        CleEvaluationTalent   INT IDENTITY(1,1) PRIMARY KEY,
        EvaluationTalentId    INT NOT NULL,        -- cle metier
        CleCollaborateur      INT NOT NULL,
        CleCycleEvaluation    INT NULL,
        CleDate               INT NOT NULL,
        ScorePerformance      INT NULL,
        ScorePotentiel        INT NULL,
        Code9Boites           INT NULL,
        CodeStatutEvaluation  INT NULL,
        Actif                 BIT NULL,
        TotalOKR              INT NULL,
        OKRTermines           INT NULL,
        CONSTRAINT UQ_fact_EvaluationTalent_CleMetier UNIQUE (EvaluationTalentId),
        CONSTRAINT FK_fact_EvalTalent_Collaborateur FOREIGN KEY (CleCollaborateur)  REFERENCES dim.Collaborateur (CleCollaborateur),
        CONSTRAINT FK_fact_EvalTalent_CycleEval      FOREIGN KEY (CleCycleEvaluation) REFERENCES dim.CycleEvaluation (CleCycleEvaluation),
        CONSTRAINT FK_fact_EvalTalent_Date           FOREIGN KEY (CleDate)           REFERENCES dim.Calendrier (CleDate)
    );
    CREATE INDEX IX_fact_EvalTalent_CleCollaborateur  ON fact.EvaluationTalent (CleCollaborateur);
    CREATE INDEX IX_fact_EvalTalent_CleCycleEvaluation ON fact.EvaluationTalent (CleCycleEvaluation);
    CREATE INDEX IX_fact_EvalTalent_CleDate            ON fact.EvaluationTalent (CleDate);
    PRINT N'Cree : fact.EvaluationTalent';
END
ELSE PRINT N'EXISTANT - reutilise : fact.EvaluationTalent';
GO

IF OBJECT_ID('fact.Promotion', 'U') IS NULL
BEGIN
    CREATE TABLE fact.Promotion (
        ClePromotion              INT IDENTITY(1,1) PRIMARY KEY,
        CleCollaborateur          INT NOT NULL,     -- 1 ligne par collaborateur actif (photo courante)
        CleGrade                  INT NULL,
        CleDateSnapshot           INT NOT NULL,     -- date de calcul de la photo (correction Kimball, cf. audit)
        AncienneteAnnees          FLOAT NULL,
        MoyenneNiveauActuel       FLOAT NULL,
        MoyenneNiveauCible        FLOAT NULL,
        TauxAtteinteCompetences   FLOAT NULL,
        DernierScorePerformance   INT NULL,
        DernierScorePotentiel     INT NULL,
        BandeEligibilite          NVARCHAR(30) NULL,
        CONSTRAINT UQ_fact_Promotion_CleMetier UNIQUE (CleCollaborateur),
        CONSTRAINT FK_fact_Promotion_Collaborateur FOREIGN KEY (CleCollaborateur) REFERENCES dim.Collaborateur (CleCollaborateur),
        CONSTRAINT FK_fact_Promotion_Grade          FOREIGN KEY (CleGrade)          REFERENCES dim.Grade (CleGrade),
        CONSTRAINT FK_fact_Promotion_DateSnapshot    FOREIGN KEY (CleDateSnapshot)   REFERENCES dim.Calendrier (CleDate)
    );
    CREATE INDEX IX_fact_Promotion_CleGrade ON fact.Promotion (CleGrade);
    CREATE INDEX IX_fact_Promotion_CleDateSnapshot ON fact.Promotion (CleDateSnapshot);
    PRINT N'Cree : fact.Promotion';
END
ELSE PRINT N'EXISTANT - reutilise : fact.Promotion';
GO

IF OBJECT_ID('fact.Succession', 'U') IS NULL
BEGIN
    CREATE TABLE fact.Succession (
        CleSuccession               INT IDENTITY(1,1) PRIMARY KEY,
        SuccessionPlanId            INT NOT NULL,
        IdSnapshot                  INT NULL,
        CleCollaborateurTitulaire   INT NULL,
        CleCollaborateurCandidat    INT NULL,
        CleDateCreation             INT NULL,
        CleDateSnapshot             INT NULL,
        CodeStatutPlan              INT NULL,
        Rang                        INT NULL,
        ScoreSuccession             INT NULL,
        ScoreCouverture             INT NULL,
        CodeHorizonPreparation      INT NULL,
        CONSTRAINT UQ_fact_Succession_CleMetier UNIQUE (SuccessionPlanId, IdSnapshot),
        CONSTRAINT FK_fact_Succession_Titulaire   FOREIGN KEY (CleCollaborateurTitulaire) REFERENCES dim.Collaborateur (CleCollaborateur),
        CONSTRAINT FK_fact_Succession_Candidat    FOREIGN KEY (CleCollaborateurCandidat)  REFERENCES dim.Collaborateur (CleCollaborateur),
        CONSTRAINT FK_fact_Succession_DateCreation FOREIGN KEY (CleDateCreation) REFERENCES dim.Calendrier (CleDate),
        CONSTRAINT FK_fact_Succession_DateSnapshot  FOREIGN KEY (CleDateSnapshot)  REFERENCES dim.Calendrier (CleDate)
    );
    CREATE INDEX IX_fact_Succession_Titulaire ON fact.Succession (CleCollaborateurTitulaire);
    CREATE INDEX IX_fact_Succession_Candidat  ON fact.Succession (CleCollaborateurCandidat);
    CREATE INDEX IX_fact_Succession_DateCreation ON fact.Succession (CleDateCreation);
    CREATE INDEX IX_fact_Succession_DateSnapshot  ON fact.Succession (CleDateSnapshot);
    PRINT N'Cree : fact.Succession';
END
ELSE PRINT N'EXISTANT - reutilise : fact.Succession';
GO

PRINT N'dim/fact : schema francise pret (9 dimensions, 5 faits, index FK complets).';
GO
