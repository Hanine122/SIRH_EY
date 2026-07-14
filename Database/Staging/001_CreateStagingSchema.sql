/* ============================================================================
   SIRH.EY — Analytics Staging Layer
   Schema: stg
   Scope : CREATE TABLE / PRIMARY KEY / FOREIGN KEY / INDEX only. No ETL.

   Design rules applied:
   - stg tables are typed 1:1 pass-through copies of their dbo source table.
     Column names, order and nullability mirror the source exactly, so no
     transformation logic is embedded at this layer.
   - Every stg table keeps the source's own Id (or composite key) as its
     Primary Key — no new surrogate keys are minted here. Surrogate keys are
     a dim/fact-layer concern (out of scope for this script).
   - One metadata column per table, _StagingLoadedAt, records when a row was
     staged. It is the only addition beyond the source schema.
   - Foreign keys are declared between stg tables only (stg never points back
     to dbo), so the staging schema is self-contained and can be reloaded
     independently of the OLTP schema's own constraints.
   - dbo.SystemParameters and dbo.Parametres are two parallel config tables
     in the source (known duplication, flagged in the prior architecture
     pass). They are staged separately, unchanged — reconciling them into one
     table is a transformation decision that belongs to the ETL/dim layer,
     not to staging DDL.
   - dbo.AspNetUsers is staged as stg.Users with only the business-relevant
     columns (Id, UserName, Email, Nom, Prenom). Identity security columns
     (PasswordHash, SecurityStamp, LockoutEnd, etc.) are deliberately not
     duplicated into the analytics schema — they carry no analytical value
     and copying them would be unnecessary duplication of sensitive data.
   - All other tables are staged with every source column — deciding which
     columns are analytically useful (e.g. Collaborateurs' legacy string
     fields vs. its normalized FK columns) is deferred to the ETL/dim layer.

   Source: live schema of SIRH_EY, read via sys.tables/sys.columns/
   sys.foreign_keys on 2026-07-14.
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'stg')
    EXEC('CREATE SCHEMA stg');
GO

/* ============================================================================
   1. CREATE TABLE  (Primary Keys declared inline)
   ============================================================================ */

-- ── Identity (business subset only) ────────────────────────────────────────

CREATE TABLE stg.Users (
    Id                  NVARCHAR(450)   NOT NULL,
    UserName            NVARCHAR(256)   NULL,
    Email               NVARCHAR(256)   NULL,
    Nom                 NVARCHAR(MAX)   NULL,
    Prenom              NVARCHAR(MAX)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Users_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Users PRIMARY KEY (Id)
);
GO

-- ── HR Master Data (referential) ───────────────────────────────────────────

CREATE TABLE stg.Departments (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Code                NVARCHAR(20)    NULL,
    Description         NVARCHAR(MAX)   NULL,
    IsActive            BIT             NOT NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Departments_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Departments PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SubDepartments (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Code                NVARCHAR(20)    NULL,
    DepartmentId        INT             NOT NULL,
    Description         NVARCHAR(MAX)   NULL,
    IsActive            BIT             NOT NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SubDepartments_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SubDepartments PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Positions (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Code                NVARCHAR(20)    NULL,
    SubDepartmentId     INT             NULL,
    Description         NVARCHAR(MAX)   NULL,
    IsActive            BIT             NOT NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Positions_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Positions PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Grades (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(50)    NOT NULL,
    Level               INT             NOT NULL,
    Description         NVARCHAR(MAX)   NULL,
    IsActive            BIT             NOT NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Grades_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Grades PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.BusinessUnits (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    Code                NVARCHAR(20)    NULL,
    Description         NVARCHAR(MAX)   NULL,
    IsActive            BIT             NOT NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_BusinessUnits_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_BusinessUnits PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Locations (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,
    City                NVARCHAR(100)   NULL,
    Country             NVARCHAR(100)   NULL,
    IsActive            BIT             NOT NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Locations_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Locations PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.ContractTypes (
    Id                  INT             NOT NULL,
    Name                NVARCHAR(80)    NOT NULL,
    Code                NVARCHAR(20)    NULL,
    Description         NVARCHAR(MAX)   NULL,
    MaxDurationMonths   INT             NULL,
    IsActive            BIT             NOT NULL,
    CreatedBy           NVARCHAR(256)   NULL,
    CreatedAt           DATETIME2(7)    NOT NULL,
    UpdatedBy           NVARCHAR(256)   NULL,
    UpdatedAt           DATETIME2(7)    NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_ContractTypes_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_ContractTypes PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.GradeReferentiels (
    Id                          INT             NOT NULL,
    Grade                       NVARCHAR(50)    NOT NULL,
    NiveauMinCompetences        FLOAT           NOT NULL,
    AncienneteMinAns            INT             NOT NULL,
    NombreImplementationsMin    INT             NOT NULL,
    ExperienceDomainMinAns      INT             NOT NULL,
    GradeSuivant                NVARCHAR(50)    NULL,
    Description                 NVARCHAR(MAX)   NULL,
    Niveau                      INT             NOT NULL,
    _StagingLoadedAt            DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_GradeReferentiels_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_GradeReferentiels PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SystemParameters (
    Id                  INT             NOT NULL,
    [Key]               NVARCHAR(100)   NOT NULL,
    [Value]             NVARCHAR(MAX)   NOT NULL,
    Category            NVARCHAR(50)    NULL,
    Description         NVARCHAR(MAX)   NULL,
    IsVisible           BIT             NOT NULL,
    IsEditable          BIT             NOT NULL,
    ModifiedBy          NVARCHAR(256)   NULL,
    ModifiedDate        DATETIME2(7)    NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SystemParameters_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SystemParameters PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Parametres (
    Id                      INT             NOT NULL,
    Code                    NVARCHAR(MAX)   NOT NULL,
    Valeur                  NVARCHAR(MAX)   NOT NULL,
    TypeValeur              NVARCHAR(MAX)   NOT NULL,
    Description             NVARCHAR(MAX)   NULL,
    EstModifiable           BIT             NOT NULL,
    DerniereModification    DATETIME2(7)    NOT NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Parametres_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Parametres PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.PositionRequiredCompetences (
    Id                  INT             NOT NULL,
    PositionId          INT             NOT NULL,
    CompetenceName      NVARCHAR(100)   NOT NULL,
    Category            NVARCHAR(50)    NULL,
    RequiredLevel       INT             NOT NULL,
    IsMandatory         BIT             NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_PositionRequiredCompetences_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_PositionRequiredCompetences PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.PositionMandatoryFormations (
    Id                  INT             NOT NULL,
    PositionId          INT             NOT NULL,
    FormationId         INT             NOT NULL,
    DeadlineMonths      INT             NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_PositionMandatoryFormations_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_PositionMandatoryFormations PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.PositionGradeEligibilities (
    Id                  INT             NOT NULL,
    PositionId          INT             NOT NULL,
    GradeEntityId       INT             NOT NULL,
    IsMinimum           BIT             NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_PositionGradeEligibilities_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_PositionGradeEligibilities PRIMARY KEY (Id)
);
GO

-- ── Core HR ─────────────────────────────────────────────────────────────────

CREATE TABLE stg.Collaborateurs (
    Id                              INT             NOT NULL,
    Nom                             NVARCHAR(MAX)   NOT NULL,
    Prenom                          NVARCHAR(MAX)   NOT NULL,
    Email                           NVARCHAR(MAX)   NULL,
    Departement                     NVARCHAR(MAX)   NULL,
    Poste                           NVARCHAR(MAX)   NULL,
    DateEmbauche                    DATETIME2(7)    NOT NULL,
    Actif                           BIT             NOT NULL,
    Grade                           NVARCHAR(MAX)   NULL,
    ManagerId                       INT             NULL,
    UserId                          NVARCHAR(450)   NULL,
    Statut                          INT             NOT NULL,
    Adresse                         NVARCHAR(MAX)   NULL,
    BusinessUnit                    NVARCHAR(MAX)   NULL,
    ContactUrgence                  NVARCHAR(MAX)   NULL,
    DateNaissance                   DATETIME2(7)    NULL,
    DatePrisePoste                  DATETIME2(7)    NULL,
    EtatCivil                       NVARCHAR(MAX)   NULL,
    FormationsObligatoires          NVARCHAR(MAX)   NULL,
    Genre                           NVARCHAR(MAX)   NULL,
    Localisation                    NVARCHAR(MAX)   NULL,
    Matricule                       NVARCHAR(MAX)   NULL,
    Nationalite                     NVARCHAR(MAX)   NULL,
    NiveauHierarchique              NVARCHAR(MAX)   NULL,
    NiveauPreparationSuccession     INT             NULL,
    Pays                            NVARCHAR(MAX)   NULL,
    PotentielCarriere               NVARCHAR(MAX)   NULL,
    TelephonePersonnel              NVARCHAR(MAX)   NULL,
    TypeContrat                     NVARCHAR(MAX)   NULL,
    Ville                           NVARCHAR(MAX)   NULL,
    PositionId                      INT             NULL,
    SubDepartmentId                 INT             NULL,
    RoleRH                          NVARCHAR(MAX)   NULL,
    BusinessUnitId                  INT             NULL,
    DepartmentId                    INT             NULL,
    GradeId                         INT             NULL,
    LocationId                      INT             NULL,
    ContractTypeId                  INT             NULL,
    ExperienceDomainAnnees          INT             NULL,
    ModeDeploiement                 NVARCHAR(20)    NULL,
    NombreImplementations           INT             NULL,
    _StagingLoadedAt                DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Collaborateurs_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Collaborateurs PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.CategoriesCompetences (
    Id                  INT             NOT NULL,
    Nom                 NVARCHAR(MAX)   NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_CategoriesCompetences_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_CategoriesCompetences PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Competences (
    Id                      INT             NOT NULL,
    Nom                     NVARCHAR(MAX)   NOT NULL,
    NiveauActuel            INT             NOT NULL,
    NiveauCible             INT             NOT NULL,
    DateEvaluation          DATETIME2(7)    NOT NULL,
    CollaborateurId         INT             NOT NULL,
    CategorieCompetenceId   INT             NULL,
    SkillId                 INT             NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Competences_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Competences PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.CompetencesRequisesParPoste (
    Id                  INT             NOT NULL,
    Poste               NVARCHAR(MAX)   NOT NULL,
    Competence          NVARCHAR(MAX)   NOT NULL,
    NiveauRequis        INT             NOT NULL,
    Priorite            NVARCHAR(MAX)   NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_CompetencesRequisesParPoste_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_CompetencesRequisesParPoste PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.EvaluationsHistoriques (
    Id                  INT             NOT NULL,
    CompetenceId        INT             NOT NULL,
    NiveauAncien        INT             NOT NULL,
    NiveauNouveau       INT             NOT NULL,
    DateChangement      DATETIME2(7)    NOT NULL,
    Raison              NVARCHAR(MAX)   NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_EvaluationsHistoriques_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_EvaluationsHistoriques PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Formations (
    Id                      INT             NOT NULL,
    Titre                   NVARCHAR(MAX)   NOT NULL,
    Formateur               NVARCHAR(MAX)   NULL,
    DureeHeures             INT             NOT NULL,
    CapaciteMax             INT             NOT NULL,
    PlacesPrises            INT             NOT NULL,
    Categorie               NVARCHAR(MAX)   NULL,
    DateDebut               DATETIME2(7)    NOT NULL,
    Organisme               NVARCHAR(MAX)   NULL,
    CompetenceVisee         NVARCHAR(MAX)   NULL,
    DepartementCible        NVARCHAR(MAX)   NULL,
    DomaineCompetence       NVARCHAR(MAX)   NULL,
    EstCertifiante          BIT             NOT NULL,
    MetierCible             NVARCHAR(MAX)   NULL,
    NiveauDifficulte        NVARCHAR(MAX)   NULL,
    PosteCible              NVARCHAR(MAX)   NULL,
    CertificationNom        NVARCHAR(200)   NULL,
    CompetencesRequises     NVARCHAR(500)   NULL,
    Description             NVARCHAR(MAX)   NULL,
    EstForteDemande         BIT             NOT NULL,
    EstStrategique          BIT             NOT NULL,
    ExternalUrl             NVARCHAR(500)   NULL,
    MentorEmail             NVARCHAR(200)   NULL,
    Plateforme              NVARCHAR(100)   NULL,
    SupportPdfUrl           NVARCHAR(500)   NULL,
    SkillId                 INT             NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Formations_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Formations PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.FormationCompetences (
    FormationId         INT             NOT NULL,
    CompetenceId         INT             NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_FormationCompetences_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_FormationCompetences PRIMARY KEY (FormationId, CompetenceId)
);
GO

CREATE TABLE stg.Inscriptions (
    Id                      INT             NOT NULL,
    DateInscription         DATETIME2(7)    NOT NULL,
    Terminee                BIT             NOT NULL,
    CollaborateurId         INT             NOT NULL,
    FormationId             INT             NOT NULL,
    DateExamen              DATETIME2(7)    NULL,
    Progression             INT             NOT NULL,
    DateCompletion          DATETIME2(7)    NULL,
    DateExpiration          DATETIME2(7)    NULL,
    SourceCertification     NVARCHAR(100)   NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Inscriptions_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Inscriptions PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.EvaluationsCompetences (
    Id                              INT             NOT NULL,
    CompetenceId                    INT             NOT NULL,
    SeuilRh                         INT             NOT NULL,
    AutoEvaluationCollaborateur     INT             NOT NULL,
    EvaluationManager               INT             NULL,
    ValidationManager               BIT             NOT NULL,
    DateAutoEvaluation              DATETIME2(7)    NULL,
    DateValidationManager           DATETIME2(7)    NULL,
    CommentaireCollaborateur        NVARCHAR(1000)  NULL,
    CommentaireManager              NVARCHAR(1000)  NULL,
    InscriptionId                   INT             NULL,
    _StagingLoadedAt                DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_EvaluationsCompetences_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_EvaluationsCompetences PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.EvaluationsPostFormation (
    Id                  INT             NOT NULL,
    InscriptionId       INT             NOT NULL,
    NoteGlobale         INT             NOT NULL,
    NoteContenu         INT             NULL,
    NoteFormateur       INT             NULL,
    Recommande          BIT             NOT NULL,
    Commentaire         NVARCHAR(1000)  NULL,
    DateEvaluation      DATETIME2(7)    NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_EvaluationsPostFormation_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_EvaluationsPostFormation PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.EvaluationsSuiviFormation (
    Id                              INT             NOT NULL,
    InscriptionId                   INT             NOT NULL,
    NoteApplicationCompetences      INT             NOT NULL,
    NoteImpactBusiness              INT             NOT NULL,
    ExemplesConcrets                NVARCHAR(1000)  NULL,
    Commentaire                     NVARCHAR(1000)  NULL,
    DateEvaluation                  DATETIME2(7)    NOT NULL,
    _StagingLoadedAt                DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_EvaluationsSuiviFormation_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_EvaluationsSuiviFormation PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.PlansDeveloppement (
    Id                      INT             NOT NULL,
    CollaborateurId         INT             NOT NULL,
    FormationId             INT             NOT NULL,
    DateRecommandation      DATETIME2(7)    NOT NULL,
    Statut                  NVARCHAR(MAX)   NOT NULL,
    Commentaire             NVARCHAR(MAX)   NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_PlansDeveloppement_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_PlansDeveloppement PRIMARY KEY (Id)
);
GO

-- ── Talent Management ───────────────────────────────────────────────────────

CREATE TABLE stg.ReviewCycles (
    Id                  INT             NOT NULL,
    Nom                 NVARCHAR(100)   NOT NULL,
    DateDebut           DATETIME2(7)    NOT NULL,
    DateFin             DATETIME2(7)    NOT NULL,
    Statut              INT             NOT NULL,
    Perimetre           NVARCHAR(MAX)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_ReviewCycles_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_ReviewCycles PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.TalentEvaluations (
    Id                          INT             NOT NULL,
    CollaborateurId             INT             NOT NULL,
    PerformanceScore            INT             NOT NULL,
    PotentielScore              INT             NOT NULL,
    Category                    INT             NOT NULL,
    CommentairesPerformance     NVARCHAR(MAX)   NULL,
    CommentairesPotentiel       NVARCHAR(MAX)   NULL,
    EvaluateurId                NVARCHAR(450)   NULL,
    DateEvaluation               DATETIME2(7)    NOT NULL,
    Actif                       BIT             NOT NULL,
    ApprouveParId               NVARCHAR(450)   NULL,
    DateApprobation             DATETIME2(7)    NULL,
    ReviewCycleId               INT             NULL,
    Statut                      INT             NOT NULL,
    _StagingLoadedAt            DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_TalentEvaluations_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_TalentEvaluations PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.OKRs (
    Id                      INT             NOT NULL,
    CollaborateurId         INT             NOT NULL,
    Objectif                NVARCHAR(200)   NOT NULL,
    Description             NVARCHAR(MAX)   NULL,
    Annee                   INT             NOT NULL,
    Trimestre               INT             NOT NULL,
    Statut                  INT             NOT NULL,
    ProgressionGlobale      INT             NOT NULL,
    DateDebut               DATETIME2(7)    NOT NULL,
    DateFinCible            DATETIME2(7)    NOT NULL,
    DateRealisation         DATETIME2(7)    NULL,
    ManagerId               NVARCHAR(450)   NULL,
    ValideParManager        BIT             NOT NULL,
    DateValidation          DATETIME2(7)    NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_OKRs_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_OKRs PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.KeyResults (
    Id                  INT             NOT NULL,
    OKRId               INT             NOT NULL,
    Description         NVARCHAR(200)   NOT NULL,
    ValeurCible         FLOAT           NOT NULL,
    ValeurActuelle      FLOAT           NOT NULL,
    Unite               NVARCHAR(MAX)   NULL,
    Difficulte          INT             NOT NULL,
    Statut              INT             NOT NULL,
    Ordre               INT             NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_KeyResults_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_KeyResults PRIMARY KEY (Id)
);
GO

-- ── Succession Planning ─────────────────────────────────────────────────────

CREATE TABLE stg.SuccessionPlans (
    Id                          INT             NOT NULL,
    CollaborateurTitulaireId    INT             NULL,
    Poste                       NVARCHAR(150)   NOT NULL,
    Departement                 NVARCHAR(MAX)   NULL,
    ReviewCycleId               INT             NULL,
    Statut                      INT             NOT NULL,
    DateCreation                DATETIME2(7)    NOT NULL,
    ProposeParId                NVARCHAR(450)   NULL,
    DateValidationManager       DATETIME2(7)    NULL,
    ApprouveParId               NVARCHAR(450)   NULL,
    DateApprobationRH           DATETIME2(7)    NULL,
    CommentaireRefus            NVARCHAR(MAX)   NULL,
    _StagingLoadedAt            DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SuccessionPlans_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SuccessionPlans PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SuccessorRankingSnapshots (
    Id                  INT             NOT NULL,
    SuccessionPlanId    INT             NOT NULL,
    CandidatId          INT             NOT NULL,
    Rang                INT             NOT NULL,
    ScoreSuccession     INT             NOT NULL,
    ScoreCouverture     INT             NOT NULL,
    Horizon             INT             NOT NULL,
    DateSnapshot        DATETIME2(7)    NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SuccessorRankingSnapshots_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SuccessorRankingSnapshots PRIMARY KEY (Id)
);
GO

-- ── Skill Ontology ──────────────────────────────────────────────────────────

CREATE TABLE stg.SkillCategories (
    Id                  INT             NOT NULL,
    Nom                 NVARCHAR(100)   NOT NULL,
    Description         NVARCHAR(MAX)   NULL,
    ParentCategoryId    INT             NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SkillCategories_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SkillCategories PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.Skills (
    Id                  INT             NOT NULL,
    Nom                 NVARCHAR(150)   NOT NULL,
    Description         NVARCHAR(MAX)   NULL,
    SkillCategoryId     INT             NULL,
    Actif               BIT             NOT NULL,
    DateCreation        DATETIME2(7)    NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Skills_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Skills PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SkillAliases (
    Id                  INT             NOT NULL,
    SkillId             INT             NOT NULL,
    Alias               NVARCHAR(150)   NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SkillAliases_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SkillAliases PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SkillLevels (
    Id                      INT             NOT NULL,
    SkillId                 INT             NOT NULL,
    Niveau                  INT             NOT NULL,
    Libelle                 NVARCHAR(200)   NOT NULL,
    CriteresValidation      NVARCHAR(MAX)   NULL,
    _StagingLoadedAt        DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SkillLevels_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SkillLevels PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SkillCriticalities (
    Id                  INT             NOT NULL,
    SkillId             INT             NOT NULL,
    Niveau              INT             NOT NULL,
    Justification       NVARCHAR(MAX)   NULL,
    Perimetre           NVARCHAR(MAX)   NULL,
    DateEvaluation      DATETIME2(7)    NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SkillCriticalities_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SkillCriticalities PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SkillVersions (
    Id                  INT             NOT NULL,
    SkillId             INT             NOT NULL,
    Version             INT             NOT NULL,
    Nom                 NVARCHAR(150)   NOT NULL,
    Description         NVARCHAR(MAX)   NULL,
    DateEffet           DATETIME2(7)    NOT NULL,
    Actif               BIT             NOT NULL,
    ModifieParId        NVARCHAR(450)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SkillVersions_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SkillVersions PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.SkillRelations (
    Id                  INT             NOT NULL,
    SourceSkillId       INT             NOT NULL,
    TargetSkillId       INT             NOT NULL,
    Type                INT             NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_SkillRelations_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_SkillRelations PRIMARY KEY (Id)
);
GO

-- ── Certifications ──────────────────────────────────────────────────────────

CREATE TABLE stg.Certifications (
    Id                  INT             NOT NULL,
    Nom                 NVARCHAR(200)   NOT NULL,
    Organisme           NVARCHAR(150)   NULL,
    Domaine             NVARCHAR(100)   NULL,
    CodeExamen          NVARCHAR(50)    NULL,
    EstReconnue         BIT             NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_Certifications_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_Certifications PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.CollaborateurCertifications (
    Id                  INT             NOT NULL,
    CollaborateurId     INT             NOT NULL,
    CertificationId     INT             NOT NULL,
    DateObtention       DATETIME2(7)    NULL,
    DateExpiration      DATETIME2(7)    NULL,
    Statut              NVARCHAR(MAX)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_CollaborateurCertifications_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_CollaborateurCertifications PRIMARY KEY (Id)
);
GO

-- ── Governance & Audit ──────────────────────────────────────────────────────

CREATE TABLE stg.DecisionRules (
    Id                  INT             NOT NULL,
    Code                NVARCHAR(100)   NOT NULL,
    ParametresJson      NVARCHAR(MAX)   NOT NULL,
    Version             INT             NOT NULL,
    DateEffet           DATETIME2(7)    NOT NULL,
    Actif               BIT             NOT NULL,
    ModifieParId        NVARCHAR(450)   NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_DecisionRules_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_DecisionRules PRIMARY KEY (Id)
);
GO

CREATE TABLE stg.AuditLogs (
    Id                  INT             NOT NULL,
    EntityType          NVARCHAR(100)   NOT NULL,
    EntityId            INT             NOT NULL,
    Action              INT             NOT NULL,
    AncienneValeur      NVARCHAR(MAX)   NULL,
    NouvelleValeur      NVARCHAR(MAX)   NULL,
    UtilisateurId       NVARCHAR(450)   NULL,
    DateAction          DATETIME2(7)    NOT NULL,
    _StagingLoadedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_stg_AuditLogs_LoadedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_stg_AuditLogs PRIMARY KEY (Id)
);
GO

/* ============================================================================
   2. FOREIGN KEYS  (stg → stg only; the staging schema is self-contained)
   ============================================================================ */

-- HR master data hierarchy
ALTER TABLE stg.SubDepartments  ADD CONSTRAINT FK_stg_SubDepartments_Departments        FOREIGN KEY (DepartmentId)        REFERENCES stg.Departments(Id);
ALTER TABLE stg.Positions       ADD CONSTRAINT FK_stg_Positions_SubDepartments          FOREIGN KEY (SubDepartmentId)     REFERENCES stg.SubDepartments(Id);

-- Collaborateurs
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_Users              FOREIGN KEY (UserId)              REFERENCES stg.Users(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_Manager            FOREIGN KEY (ManagerId)           REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_Departments        FOREIGN KEY (DepartmentId)        REFERENCES stg.Departments(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_SubDepartments     FOREIGN KEY (SubDepartmentId)     REFERENCES stg.SubDepartments(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_Positions          FOREIGN KEY (PositionId)          REFERENCES stg.Positions(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_Grades             FOREIGN KEY (GradeId)             REFERENCES stg.Grades(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_BusinessUnits      FOREIGN KEY (BusinessUnitId)      REFERENCES stg.BusinessUnits(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_Locations          FOREIGN KEY (LocationId)          REFERENCES stg.Locations(Id);
ALTER TABLE stg.Collaborateurs  ADD CONSTRAINT FK_stg_Collaborateurs_ContractTypes      FOREIGN KEY (ContractTypeId)      REFERENCES stg.ContractTypes(Id);

-- Position relationship junctions
ALTER TABLE stg.PositionRequiredCompetences  ADD CONSTRAINT FK_stg_PositionRequiredCompetences_Positions FOREIGN KEY (PositionId)      REFERENCES stg.Positions(Id);
ALTER TABLE stg.PositionMandatoryFormations  ADD CONSTRAINT FK_stg_PositionMandatoryFormations_Positions FOREIGN KEY (PositionId)      REFERENCES stg.Positions(Id);
ALTER TABLE stg.PositionMandatoryFormations  ADD CONSTRAINT FK_stg_PositionMandatoryFormations_Formations FOREIGN KEY (FormationId)    REFERENCES stg.Formations(Id);
ALTER TABLE stg.PositionGradeEligibilities   ADD CONSTRAINT FK_stg_PositionGradeEligibilities_Positions  FOREIGN KEY (PositionId)      REFERENCES stg.Positions(Id);
ALTER TABLE stg.PositionGradeEligibilities   ADD CONSTRAINT FK_stg_PositionGradeEligibilities_Grades     FOREIGN KEY (GradeEntityId)   REFERENCES stg.Grades(Id);

-- Competencies
ALTER TABLE stg.Competences               ADD CONSTRAINT FK_stg_Competences_Collaborateurs           FOREIGN KEY (CollaborateurId)       REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.Competences               ADD CONSTRAINT FK_stg_Competences_CategoriesCompetences    FOREIGN KEY (CategorieCompetenceId) REFERENCES stg.CategoriesCompetences(Id);
ALTER TABLE stg.Competences               ADD CONSTRAINT FK_stg_Competences_Skills                   FOREIGN KEY (SkillId)               REFERENCES stg.Skills(Id);
ALTER TABLE stg.EvaluationsHistoriques    ADD CONSTRAINT FK_stg_EvaluationsHistoriques_Competences   FOREIGN KEY (CompetenceId)          REFERENCES stg.Competences(Id);
ALTER TABLE stg.EvaluationsCompetences    ADD CONSTRAINT FK_stg_EvaluationsCompetences_Competences   FOREIGN KEY (CompetenceId)          REFERENCES stg.Competences(Id);

-- Formations / enrollment chain
ALTER TABLE stg.Formations                ADD CONSTRAINT FK_stg_Formations_Skills                    FOREIGN KEY (SkillId)               REFERENCES stg.Skills(Id);
ALTER TABLE stg.FormationCompetences      ADD CONSTRAINT FK_stg_FormationCompetences_Formations      FOREIGN KEY (FormationId)           REFERENCES stg.Formations(Id);
ALTER TABLE stg.FormationCompetences      ADD CONSTRAINT FK_stg_FormationCompetences_Competences     FOREIGN KEY (CompetenceId)          REFERENCES stg.Competences(Id);
ALTER TABLE stg.Inscriptions              ADD CONSTRAINT FK_stg_Inscriptions_Collaborateurs          FOREIGN KEY (CollaborateurId)       REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.Inscriptions              ADD CONSTRAINT FK_stg_Inscriptions_Formations              FOREIGN KEY (FormationId)           REFERENCES stg.Formations(Id);
ALTER TABLE stg.EvaluationsCompetences    ADD CONSTRAINT FK_stg_EvaluationsCompetences_Inscriptions  FOREIGN KEY (InscriptionId)         REFERENCES stg.Inscriptions(Id);
ALTER TABLE stg.EvaluationsPostFormation  ADD CONSTRAINT FK_stg_EvaluationsPostFormation_Inscriptions  FOREIGN KEY (InscriptionId)       REFERENCES stg.Inscriptions(Id);
ALTER TABLE stg.EvaluationsSuiviFormation ADD CONSTRAINT FK_stg_EvaluationsSuiviFormation_Inscriptions FOREIGN KEY (InscriptionId)       REFERENCES stg.Inscriptions(Id);
ALTER TABLE stg.PlansDeveloppement        ADD CONSTRAINT FK_stg_PlansDeveloppement_Collaborateurs    FOREIGN KEY (CollaborateurId)       REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.PlansDeveloppement        ADD CONSTRAINT FK_stg_PlansDeveloppement_Formations        FOREIGN KEY (FormationId)           REFERENCES stg.Formations(Id);

-- Certifications
ALTER TABLE stg.CollaborateurCertifications ADD CONSTRAINT FK_stg_CollaborateurCertifications_Collaborateurs FOREIGN KEY (CollaborateurId) REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.CollaborateurCertifications ADD CONSTRAINT FK_stg_CollaborateurCertifications_Certifications FOREIGN KEY (CertificationId) REFERENCES stg.Certifications(Id);

-- Talent management
ALTER TABLE stg.TalentEvaluations  ADD CONSTRAINT FK_stg_TalentEvaluations_Collaborateurs  FOREIGN KEY (CollaborateurId)  REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.TalentEvaluations  ADD CONSTRAINT FK_stg_TalentEvaluations_ReviewCycles    FOREIGN KEY (ReviewCycleId)    REFERENCES stg.ReviewCycles(Id);
ALTER TABLE stg.TalentEvaluations  ADD CONSTRAINT FK_stg_TalentEvaluations_Evaluateur      FOREIGN KEY (EvaluateurId)     REFERENCES stg.Users(Id);
ALTER TABLE stg.TalentEvaluations  ADD CONSTRAINT FK_stg_TalentEvaluations_Approuve        FOREIGN KEY (ApprouveParId)    REFERENCES stg.Users(Id);
ALTER TABLE stg.OKRs               ADD CONSTRAINT FK_stg_OKRs_Collaborateurs               FOREIGN KEY (CollaborateurId)  REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.OKRs               ADD CONSTRAINT FK_stg_OKRs_Manager                      FOREIGN KEY (ManagerId)        REFERENCES stg.Users(Id);
ALTER TABLE stg.KeyResults         ADD CONSTRAINT FK_stg_KeyResults_OKRs                   FOREIGN KEY (OKRId)            REFERENCES stg.OKRs(Id);

-- Succession planning
ALTER TABLE stg.SuccessionPlans             ADD CONSTRAINT FK_stg_SuccessionPlans_Collaborateurs        FOREIGN KEY (CollaborateurTitulaireId) REFERENCES stg.Collaborateurs(Id);
ALTER TABLE stg.SuccessionPlans             ADD CONSTRAINT FK_stg_SuccessionPlans_ReviewCycles          FOREIGN KEY (ReviewCycleId)            REFERENCES stg.ReviewCycles(Id);
ALTER TABLE stg.SuccessionPlans             ADD CONSTRAINT FK_stg_SuccessionPlans_Propose               FOREIGN KEY (ProposeParId)             REFERENCES stg.Users(Id);
ALTER TABLE stg.SuccessionPlans             ADD CONSTRAINT FK_stg_SuccessionPlans_Approuve              FOREIGN KEY (ApprouveParId)            REFERENCES stg.Users(Id);
ALTER TABLE stg.SuccessorRankingSnapshots   ADD CONSTRAINT FK_stg_SuccessorRankingSnapshots_Plans       FOREIGN KEY (SuccessionPlanId)         REFERENCES stg.SuccessionPlans(Id);
ALTER TABLE stg.SuccessorRankingSnapshots   ADD CONSTRAINT FK_stg_SuccessorRankingSnapshots_Candidat    FOREIGN KEY (CandidatId)               REFERENCES stg.Collaborateurs(Id);

-- Skill ontology
ALTER TABLE stg.Skills            ADD CONSTRAINT FK_stg_Skills_SkillCategories        FOREIGN KEY (SkillCategoryId)   REFERENCES stg.SkillCategories(Id);
ALTER TABLE stg.SkillCategories   ADD CONSTRAINT FK_stg_SkillCategories_Parent        FOREIGN KEY (ParentCategoryId)  REFERENCES stg.SkillCategories(Id);
ALTER TABLE stg.SkillAliases      ADD CONSTRAINT FK_stg_SkillAliases_Skills           FOREIGN KEY (SkillId)           REFERENCES stg.Skills(Id);
ALTER TABLE stg.SkillLevels       ADD CONSTRAINT FK_stg_SkillLevels_Skills            FOREIGN KEY (SkillId)           REFERENCES stg.Skills(Id);
ALTER TABLE stg.SkillCriticalities ADD CONSTRAINT FK_stg_SkillCriticalities_Skills    FOREIGN KEY (SkillId)           REFERENCES stg.Skills(Id);
ALTER TABLE stg.SkillVersions     ADD CONSTRAINT FK_stg_SkillVersions_Skills          FOREIGN KEY (SkillId)           REFERENCES stg.Skills(Id);
ALTER TABLE stg.SkillVersions     ADD CONSTRAINT FK_stg_SkillVersions_ModifiePar      FOREIGN KEY (ModifieParId)      REFERENCES stg.Users(Id);
ALTER TABLE stg.SkillRelations    ADD CONSTRAINT FK_stg_SkillRelations_Source         FOREIGN KEY (SourceSkillId)     REFERENCES stg.Skills(Id);
ALTER TABLE stg.SkillRelations    ADD CONSTRAINT FK_stg_SkillRelations_Target         FOREIGN KEY (TargetSkillId)     REFERENCES stg.Skills(Id);

-- Governance & audit
ALTER TABLE stg.DecisionRules ADD CONSTRAINT FK_stg_DecisionRules_ModifiePar FOREIGN KEY (ModifieParId)  REFERENCES stg.Users(Id);
ALTER TABLE stg.AuditLogs     ADD CONSTRAINT FK_stg_AuditLogs_Utilisateur    FOREIGN KEY (UtilisateurId) REFERENCES stg.Users(Id);
GO

/* ============================================================================
   3. INDEXES
      - One nonclustered index per FK column not already covered by the PK
        (SQL Server does not auto-index FK columns, unlike the PK itself).
      - Watermark indexes on the date/timestamp columns identified as the
        incremental-load key for each transaction table in the staging design.
      - A small number of natural-key lookup indexes for tables joined by
        business key rather than surrogate key.
   ============================================================================ */

-- FK-column indexes
CREATE INDEX IX_stg_SubDepartments_DepartmentId              ON stg.SubDepartments (DepartmentId);
CREATE INDEX IX_stg_Positions_SubDepartmentId                 ON stg.Positions (SubDepartmentId);
CREATE INDEX IX_stg_Collaborateurs_UserId                     ON stg.Collaborateurs (UserId);
CREATE INDEX IX_stg_Collaborateurs_ManagerId                  ON stg.Collaborateurs (ManagerId);
CREATE INDEX IX_stg_Collaborateurs_DepartmentId                ON stg.Collaborateurs (DepartmentId);
CREATE INDEX IX_stg_Collaborateurs_SubDepartmentId             ON stg.Collaborateurs (SubDepartmentId);
CREATE INDEX IX_stg_Collaborateurs_PositionId                  ON stg.Collaborateurs (PositionId);
CREATE INDEX IX_stg_Collaborateurs_GradeId                     ON stg.Collaborateurs (GradeId);
CREATE INDEX IX_stg_Collaborateurs_BusinessUnitId              ON stg.Collaborateurs (BusinessUnitId);
CREATE INDEX IX_stg_Collaborateurs_LocationId                  ON stg.Collaborateurs (LocationId);
CREATE INDEX IX_stg_Collaborateurs_ContractTypeId              ON stg.Collaborateurs (ContractTypeId);
CREATE INDEX IX_stg_PositionRequiredCompetences_PositionId    ON stg.PositionRequiredCompetences (PositionId);
CREATE INDEX IX_stg_PositionMandatoryFormations_PositionId    ON stg.PositionMandatoryFormations (PositionId);
CREATE INDEX IX_stg_PositionMandatoryFormations_FormationId   ON stg.PositionMandatoryFormations (FormationId);
CREATE INDEX IX_stg_PositionGradeEligibilities_PositionId     ON stg.PositionGradeEligibilities (PositionId);
CREATE INDEX IX_stg_PositionGradeEligibilities_GradeEntityId  ON stg.PositionGradeEligibilities (GradeEntityId);
CREATE INDEX IX_stg_Competences_CollaborateurId                ON stg.Competences (CollaborateurId);
CREATE INDEX IX_stg_Competences_CategorieCompetenceId          ON stg.Competences (CategorieCompetenceId);
CREATE INDEX IX_stg_Competences_SkillId                        ON stg.Competences (SkillId);
CREATE INDEX IX_stg_EvaluationsHistoriques_CompetenceId       ON stg.EvaluationsHistoriques (CompetenceId);
CREATE INDEX IX_stg_EvaluationsCompetences_CompetenceId       ON stg.EvaluationsCompetences (CompetenceId);
CREATE INDEX IX_stg_EvaluationsCompetences_InscriptionId      ON stg.EvaluationsCompetences (InscriptionId);
CREATE INDEX IX_stg_Formations_SkillId                         ON stg.Formations (SkillId);
CREATE INDEX IX_stg_Inscriptions_CollaborateurId                ON stg.Inscriptions (CollaborateurId);
CREATE INDEX IX_stg_Inscriptions_FormationId                    ON stg.Inscriptions (FormationId);
CREATE INDEX IX_stg_EvaluationsPostFormation_InscriptionId    ON stg.EvaluationsPostFormation (InscriptionId);
CREATE INDEX IX_stg_EvaluationsSuiviFormation_InscriptionId   ON stg.EvaluationsSuiviFormation (InscriptionId);
CREATE INDEX IX_stg_PlansDeveloppement_CollaborateurId          ON stg.PlansDeveloppement (CollaborateurId);
CREATE INDEX IX_stg_PlansDeveloppement_FormationId               ON stg.PlansDeveloppement (FormationId);
CREATE INDEX IX_stg_CollaborateurCertifications_CollaborateurId ON stg.CollaborateurCertifications (CollaborateurId);
CREATE INDEX IX_stg_CollaborateurCertifications_CertificationId ON stg.CollaborateurCertifications (CertificationId);
CREATE INDEX IX_stg_TalentEvaluations_CollaborateurId            ON stg.TalentEvaluations (CollaborateurId);
CREATE INDEX IX_stg_TalentEvaluations_ReviewCycleId              ON stg.TalentEvaluations (ReviewCycleId);
CREATE INDEX IX_stg_OKRs_CollaborateurId                          ON stg.OKRs (CollaborateurId);
CREATE INDEX IX_stg_KeyResults_OKRId                              ON stg.KeyResults (OKRId);
CREATE INDEX IX_stg_SuccessionPlans_CollaborateurTitulaireId     ON stg.SuccessionPlans (CollaborateurTitulaireId);
CREATE INDEX IX_stg_SuccessionPlans_ReviewCycleId                 ON stg.SuccessionPlans (ReviewCycleId);
CREATE INDEX IX_stg_SuccessorRankingSnapshots_SuccessionPlanId  ON stg.SuccessorRankingSnapshots (SuccessionPlanId);
CREATE INDEX IX_stg_SuccessorRankingSnapshots_CandidatId         ON stg.SuccessorRankingSnapshots (CandidatId);
CREATE INDEX IX_stg_Skills_SkillCategoryId                        ON stg.Skills (SkillCategoryId);
CREATE INDEX IX_stg_SkillCategories_ParentCategoryId              ON stg.SkillCategories (ParentCategoryId);
CREATE INDEX IX_stg_SkillAliases_SkillId                          ON stg.SkillAliases (SkillId);
CREATE INDEX IX_stg_SkillLevels_SkillId                           ON stg.SkillLevels (SkillId);
CREATE INDEX IX_stg_SkillCriticalities_SkillId                    ON stg.SkillCriticalities (SkillId);
CREATE INDEX IX_stg_SkillVersions_SkillId                         ON stg.SkillVersions (SkillId);
CREATE INDEX IX_stg_SkillRelations_SourceSkillId                  ON stg.SkillRelations (SourceSkillId);
CREATE INDEX IX_stg_SkillRelations_TargetSkillId                  ON stg.SkillRelations (TargetSkillId);
CREATE INDEX IX_stg_AuditLogs_UtilisateurId                       ON stg.AuditLogs (UtilisateurId);
GO

-- Watermark indexes (incremental-load key per staging design)
CREATE INDEX IX_stg_Inscriptions_DateInscription                  ON stg.Inscriptions (DateInscription);
CREATE INDEX IX_stg_EvaluationsHistoriques_DateChangement         ON stg.EvaluationsHistoriques (DateChangement);
CREATE INDEX IX_stg_EvaluationsPostFormation_DateEvaluation       ON stg.EvaluationsPostFormation (DateEvaluation);
CREATE INDEX IX_stg_EvaluationsSuiviFormation_DateEvaluation      ON stg.EvaluationsSuiviFormation (DateEvaluation);
CREATE INDEX IX_stg_PlansDeveloppement_DateRecommandation         ON stg.PlansDeveloppement (DateRecommandation);
CREATE INDEX IX_stg_TalentEvaluations_DateEvaluation              ON stg.TalentEvaluations (DateEvaluation);
CREATE INDEX IX_stg_CollaborateurCertifications_DateObtention     ON stg.CollaborateurCertifications (DateObtention);
CREATE INDEX IX_stg_AuditLogs_DateAction                          ON stg.AuditLogs (DateAction);
CREATE INDEX IX_stg_Departments_UpdatedAt                         ON stg.Departments (UpdatedAt);
CREATE INDEX IX_stg_SubDepartments_UpdatedAt                      ON stg.SubDepartments (UpdatedAt);
CREATE INDEX IX_stg_Positions_UpdatedAt                           ON stg.Positions (UpdatedAt);
CREATE INDEX IX_stg_Grades_UpdatedAt                              ON stg.Grades (UpdatedAt);
CREATE INDEX IX_stg_BusinessUnits_UpdatedAt                       ON stg.BusinessUnits (UpdatedAt);
CREATE INDEX IX_stg_Locations_UpdatedAt                           ON stg.Locations (UpdatedAt);
CREATE INDEX IX_stg_ContractTypes_UpdatedAt                       ON stg.ContractTypes (UpdatedAt);
GO

-- Natural-key lookup indexes
CREATE INDEX IX_stg_Skills_Nom                                    ON stg.Skills (Nom);
-- stg.CompetencesRequisesParPoste.Poste/Competence mirror the source's
-- NVARCHAR(MAX) typing, which SQL Server cannot use as an index key; no
-- lookup index is created here without narrowing the type (a transformation
-- decision, deferred to the ETL/dim layer per this script's scope).
CREATE UNIQUE INDEX UX_stg_SystemParameters_Key                   ON stg.SystemParameters ([Key]);
-- stg.Collaborateurs.Email mirrors the source's NVARCHAR(MAX) typing for the
-- same reason as above; no lookup index without narrowing the type.
GO
