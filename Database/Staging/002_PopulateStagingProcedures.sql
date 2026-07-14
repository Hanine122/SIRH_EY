/* ============================================================================
   SIRH.EY — Analytics Staging Layer
   Stored procedures to populate stg.* from the transactional dbo.* tables.

   Pattern per table: DELETE FROM stg.<Table>; INSERT INTO stg.<Table> (...)
   SELECT ... FROM dbo.<Table>; — a full reload, matching the "reference /
   full snapshot" and "incremental" tables alike for simplicity, since no
   watermark/control table exists in the current schema to drive true
   incremental loads. DELETE is used instead of TRUNCATE throughout because
   most stg tables are referenced by a foreign key from another stg table,
   and TRUNCATE refuses to run against any table with an incoming FK
   regardless of its enabled/disabled state.

   Each table's proc is self-contained and callable on its own. The
   orchestrator stg.usp_LoadAllStaging suspends FK enforcement for the
   duration of a full reload (so the 43 procs can run in any order without
   regard to dependency), then re-enables and revalidates every constraint
   before committing.
   ============================================================================ */

-- ── Identity (business subset only) ────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_Users
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Users;
    INSERT INTO stg.Users (Id, UserName, Email, Nom, Prenom)
    SELECT Id, UserName, Email, Nom, Prenom
    FROM dbo.AspNetUsers;
END
GO

-- ── HR Master Data (referential) ───────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_Departments
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Departments;
    INSERT INTO stg.Departments (Id, Name, Code, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT Id, Name, Code, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM dbo.Departments;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SubDepartments
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SubDepartments;
    INSERT INTO stg.SubDepartments (Id, Name, Code, DepartmentId, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT Id, Name, Code, DepartmentId, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM dbo.SubDepartments;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Positions
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Positions;
    INSERT INTO stg.Positions (Id, Name, Code, SubDepartmentId, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT Id, Name, Code, SubDepartmentId, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM dbo.Positions;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Grades
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Grades;
    INSERT INTO stg.Grades (Id, Name, Level, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT Id, Name, Level, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM dbo.Grades;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_BusinessUnits
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.BusinessUnits;
    INSERT INTO stg.BusinessUnits (Id, Name, Code, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT Id, Name, Code, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM dbo.BusinessUnits;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Locations
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Locations;
    INSERT INTO stg.Locations (Id, Name, City, Country, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    SELECT Id, Name, City, Country, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    FROM dbo.Locations;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_ContractTypes
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.ContractTypes;
    INSERT INTO stg.ContractTypes (Id, Name, Code, Description, MaxDurationMonths, IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
    SELECT Id, Name, Code, Description, MaxDurationMonths, IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt
    FROM dbo.ContractTypes;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_GradeReferentiels
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.GradeReferentiels;
    INSERT INTO stg.GradeReferentiels (Id, Grade, NiveauMinCompetences, AncienneteMinAns, NombreImplementationsMin, ExperienceDomainMinAns, GradeSuivant, Description, Niveau)
    SELECT Id, Grade, NiveauMinCompetences, AncienneteMinAns, NombreImplementationsMin, ExperienceDomainMinAns, GradeSuivant, Description, Niveau
    FROM dbo.GradeReferentiels;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SystemParameters
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SystemParameters;
    INSERT INTO stg.SystemParameters (Id, [Key], [Value], Category, Description, IsVisible, IsEditable, ModifiedBy, ModifiedDate)
    SELECT Id, [Key], [Value], Category, Description, IsVisible, IsEditable, ModifiedBy, ModifiedDate
    FROM dbo.SystemParameters;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Parametres
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Parametres;
    INSERT INTO stg.Parametres (Id, Code, Valeur, TypeValeur, Description, EstModifiable, DerniereModification)
    SELECT Id, Code, Valeur, TypeValeur, Description, EstModifiable, DerniereModification
    FROM dbo.Parametres;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_PositionRequiredCompetences
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.PositionRequiredCompetences;
    INSERT INTO stg.PositionRequiredCompetences (Id, PositionId, CompetenceName, Category, RequiredLevel, IsMandatory)
    SELECT Id, PositionId, CompetenceName, Category, RequiredLevel, IsMandatory
    FROM dbo.PositionRequiredCompetences;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_PositionMandatoryFormations
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.PositionMandatoryFormations;
    INSERT INTO stg.PositionMandatoryFormations (Id, PositionId, FormationId, DeadlineMonths)
    SELECT Id, PositionId, FormationId, DeadlineMonths
    FROM dbo.PositionMandatoryFormations;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_PositionGradeEligibilities
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.PositionGradeEligibilities;
    INSERT INTO stg.PositionGradeEligibilities (Id, PositionId, GradeEntityId, IsMinimum)
    SELECT Id, PositionId, GradeEntityId, IsMinimum
    FROM dbo.PositionGradeEligibilities;
END
GO

-- ── Core HR ─────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_Collaborateurs
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Collaborateurs;
    INSERT INTO stg.Collaborateurs (
        Id, Nom, Prenom, Email, Departement, Poste, DateEmbauche, Actif, Grade, ManagerId, UserId, Statut,
        Adresse, BusinessUnit, ContactUrgence, DateNaissance, DatePrisePoste, EtatCivil, FormationsObligatoires,
        Genre, Localisation, Matricule, Nationalite, NiveauHierarchique, NiveauPreparationSuccession, Pays,
        PotentielCarriere, TelephonePersonnel, TypeContrat, Ville, PositionId, SubDepartmentId, RoleRH,
        BusinessUnitId, DepartmentId, GradeId, LocationId, ContractTypeId, ExperienceDomainAnnees,
        ModeDeploiement, NombreImplementations
    )
    SELECT
        Id, Nom, Prenom, Email, Departement, Poste, DateEmbauche, Actif, Grade, ManagerId, UserId, Statut,
        Adresse, BusinessUnit, ContactUrgence, DateNaissance, DatePrisePoste, EtatCivil, FormationsObligatoires,
        Genre, Localisation, Matricule, Nationalite, NiveauHierarchique, NiveauPreparationSuccession, Pays,
        PotentielCarriere, TelephonePersonnel, TypeContrat, Ville, PositionId, SubDepartmentId, RoleRH,
        BusinessUnitId, DepartmentId, GradeId, LocationId, ContractTypeId, ExperienceDomainAnnees,
        ModeDeploiement, NombreImplementations
    FROM dbo.Collaborateurs;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_CategoriesCompetences
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.CategoriesCompetences;
    INSERT INTO stg.CategoriesCompetences (Id, Nom)
    SELECT Id, Nom
    FROM dbo.CategoriesCompetences;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Competences
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Competences;
    INSERT INTO stg.Competences (Id, Nom, NiveauActuel, NiveauCible, DateEvaluation, CollaborateurId, CategorieCompetenceId, SkillId)
    SELECT Id, Nom, NiveauActuel, NiveauCible, DateEvaluation, CollaborateurId, CategorieCompetenceId, SkillId
    FROM dbo.Competences;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_CompetencesRequisesParPoste
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.CompetencesRequisesParPoste;
    INSERT INTO stg.CompetencesRequisesParPoste (Id, Poste, Competence, NiveauRequis, Priorite)
    SELECT Id, Poste, Competence, NiveauRequis, Priorite
    FROM dbo.CompetencesRequisesParPoste;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_EvaluationsHistoriques
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.EvaluationsHistoriques;
    INSERT INTO stg.EvaluationsHistoriques (Id, CompetenceId, NiveauAncien, NiveauNouveau, DateChangement, Raison)
    SELECT Id, CompetenceId, NiveauAncien, NiveauNouveau, DateChangement, Raison
    FROM dbo.EvaluationsHistoriques;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Formations
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Formations;
    INSERT INTO stg.Formations (
        Id, Titre, Formateur, DureeHeures, CapaciteMax, PlacesPrises, Categorie, DateDebut, Organisme,
        CompetenceVisee, DepartementCible, DomaineCompetence, EstCertifiante, MetierCible, NiveauDifficulte,
        PosteCible, CertificationNom, CompetencesRequises, Description, EstForteDemande, EstStrategique,
        ExternalUrl, MentorEmail, Plateforme, SupportPdfUrl, SkillId
    )
    SELECT
        Id, Titre, Formateur, DureeHeures, CapaciteMax, PlacesPrises, Categorie, DateDebut, Organisme,
        CompetenceVisee, DepartementCible, DomaineCompetence, EstCertifiante, MetierCible, NiveauDifficulte,
        PosteCible, CertificationNom, CompetencesRequises, Description, EstForteDemande, EstStrategique,
        ExternalUrl, MentorEmail, Plateforme, SupportPdfUrl, SkillId
    FROM dbo.Formations;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_FormationCompetences
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.FormationCompetences;
    INSERT INTO stg.FormationCompetences (FormationId, CompetenceId)
    SELECT FormationId, CompetenceId
    FROM dbo.FormationCompetences;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Inscriptions
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Inscriptions;
    INSERT INTO stg.Inscriptions (Id, DateInscription, Terminee, CollaborateurId, FormationId, DateExamen, Progression, DateCompletion, DateExpiration, SourceCertification)
    SELECT Id, DateInscription, Terminee, CollaborateurId, FormationId, DateExamen, Progression, DateCompletion, DateExpiration, SourceCertification
    FROM dbo.Inscriptions;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_EvaluationsCompetences
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.EvaluationsCompetences;
    INSERT INTO stg.EvaluationsCompetences (
        Id, CompetenceId, SeuilRh, AutoEvaluationCollaborateur, EvaluationManager, ValidationManager,
        DateAutoEvaluation, DateValidationManager, CommentaireCollaborateur, CommentaireManager, InscriptionId
    )
    SELECT
        Id, CompetenceId, SeuilRh, AutoEvaluationCollaborateur, EvaluationManager, ValidationManager,
        DateAutoEvaluation, DateValidationManager, CommentaireCollaborateur, CommentaireManager, InscriptionId
    FROM dbo.EvaluationsCompetences;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_EvaluationsPostFormation
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.EvaluationsPostFormation;
    INSERT INTO stg.EvaluationsPostFormation (Id, InscriptionId, NoteGlobale, NoteContenu, NoteFormateur, Recommande, Commentaire, DateEvaluation)
    SELECT Id, InscriptionId, NoteGlobale, NoteContenu, NoteFormateur, Recommande, Commentaire, DateEvaluation
    FROM dbo.EvaluationsPostFormation;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_EvaluationsSuiviFormation
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.EvaluationsSuiviFormation;
    INSERT INTO stg.EvaluationsSuiviFormation (Id, InscriptionId, NoteApplicationCompetences, NoteImpactBusiness, ExemplesConcrets, Commentaire, DateEvaluation)
    SELECT Id, InscriptionId, NoteApplicationCompetences, NoteImpactBusiness, ExemplesConcrets, Commentaire, DateEvaluation
    FROM dbo.EvaluationsSuiviFormation;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_PlansDeveloppement
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.PlansDeveloppement;
    INSERT INTO stg.PlansDeveloppement (Id, CollaborateurId, FormationId, DateRecommandation, Statut, Commentaire)
    SELECT Id, CollaborateurId, FormationId, DateRecommandation, Statut, Commentaire
    FROM dbo.PlansDeveloppement;
END
GO

-- ── Talent Management ───────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_ReviewCycles
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.ReviewCycles;
    INSERT INTO stg.ReviewCycles (Id, Nom, DateDebut, DateFin, Statut, Perimetre)
    SELECT Id, Nom, DateDebut, DateFin, Statut, Perimetre
    FROM dbo.ReviewCycles;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_TalentEvaluations
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.TalentEvaluations;
    INSERT INTO stg.TalentEvaluations (
        Id, CollaborateurId, PerformanceScore, PotentielScore, Category, CommentairesPerformance,
        CommentairesPotentiel, EvaluateurId, DateEvaluation, Actif, ApprouveParId, DateApprobation,
        ReviewCycleId, Statut
    )
    SELECT
        Id, CollaborateurId, PerformanceScore, PotentielScore, Category, CommentairesPerformance,
        CommentairesPotentiel, EvaluateurId, DateEvaluation, Actif, ApprouveParId, DateApprobation,
        ReviewCycleId, Statut
    FROM dbo.TalentEvaluations;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_OKRs
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.OKRs;
    INSERT INTO stg.OKRs (
        Id, CollaborateurId, Objectif, Description, Annee, Trimestre, Statut, ProgressionGlobale,
        DateDebut, DateFinCible, DateRealisation, ManagerId, ValideParManager, DateValidation
    )
    SELECT
        Id, CollaborateurId, Objectif, Description, Annee, Trimestre, Statut, ProgressionGlobale,
        DateDebut, DateFinCible, DateRealisation, ManagerId, ValideParManager, DateValidation
    FROM dbo.OKRs;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_KeyResults
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.KeyResults;
    INSERT INTO stg.KeyResults (Id, OKRId, Description, ValeurCible, ValeurActuelle, Unite, Difficulte, Statut, Ordre)
    SELECT Id, OKRId, Description, ValeurCible, ValeurActuelle, Unite, Difficulte, Statut, Ordre
    FROM dbo.KeyResults;
END
GO

-- ── Succession Planning ─────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_SuccessionPlans
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SuccessionPlans;
    INSERT INTO stg.SuccessionPlans (
        Id, CollaborateurTitulaireId, Poste, Departement, ReviewCycleId, Statut, DateCreation,
        ProposeParId, DateValidationManager, ApprouveParId, DateApprobationRH, CommentaireRefus
    )
    SELECT
        Id, CollaborateurTitulaireId, Poste, Departement, ReviewCycleId, Statut, DateCreation,
        ProposeParId, DateValidationManager, ApprouveParId, DateApprobationRH, CommentaireRefus
    FROM dbo.SuccessionPlans;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SuccessorRankingSnapshots
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SuccessorRankingSnapshots;
    INSERT INTO stg.SuccessorRankingSnapshots (Id, SuccessionPlanId, CandidatId, Rang, ScoreSuccession, ScoreCouverture, Horizon, DateSnapshot)
    SELECT Id, SuccessionPlanId, CandidatId, Rang, ScoreSuccession, ScoreCouverture, Horizon, DateSnapshot
    FROM dbo.SuccessorRankingSnapshots;
END
GO

-- ── Skill Ontology ──────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_SkillCategories
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SkillCategories;
    INSERT INTO stg.SkillCategories (Id, Nom, Description, ParentCategoryId)
    SELECT Id, Nom, Description, ParentCategoryId
    FROM dbo.SkillCategories;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_Skills
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Skills;
    INSERT INTO stg.Skills (Id, Nom, Description, SkillCategoryId, Actif, DateCreation)
    SELECT Id, Nom, Description, SkillCategoryId, Actif, DateCreation
    FROM dbo.Skills;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SkillAliases
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SkillAliases;
    INSERT INTO stg.SkillAliases (Id, SkillId, Alias)
    SELECT Id, SkillId, Alias
    FROM dbo.SkillAliases;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SkillLevels
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SkillLevels;
    INSERT INTO stg.SkillLevels (Id, SkillId, Niveau, Libelle, CriteresValidation)
    SELECT Id, SkillId, Niveau, Libelle, CriteresValidation
    FROM dbo.SkillLevels;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SkillCriticalities
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SkillCriticalities;
    INSERT INTO stg.SkillCriticalities (Id, SkillId, Niveau, Justification, Perimetre, DateEvaluation)
    SELECT Id, SkillId, Niveau, Justification, Perimetre, DateEvaluation
    FROM dbo.SkillCriticalities;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SkillVersions
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SkillVersions;
    INSERT INTO stg.SkillVersions (Id, SkillId, Version, Nom, Description, DateEffet, Actif, ModifieParId)
    SELECT Id, SkillId, Version, Nom, Description, DateEffet, Actif, ModifieParId
    FROM dbo.SkillVersions;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_SkillRelations
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.SkillRelations;
    INSERT INTO stg.SkillRelations (Id, SourceSkillId, TargetSkillId, Type)
    SELECT Id, SourceSkillId, TargetSkillId, Type
    FROM dbo.SkillRelations;
END
GO

-- ── Certifications ──────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_Certifications
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.Certifications;
    INSERT INTO stg.Certifications (Id, Nom, Organisme, Domaine, CodeExamen, EstReconnue)
    SELECT Id, Nom, Organisme, Domaine, CodeExamen, EstReconnue
    FROM dbo.Certifications;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_CollaborateurCertifications
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.CollaborateurCertifications;
    INSERT INTO stg.CollaborateurCertifications (Id, CollaborateurId, CertificationId, DateObtention, DateExpiration, Statut)
    SELECT Id, CollaborateurId, CertificationId, DateObtention, DateExpiration, Statut
    FROM dbo.CollaborateurCertifications;
END
GO

-- ── Governance & Audit ──────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE stg.usp_Load_DecisionRules
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.DecisionRules;
    INSERT INTO stg.DecisionRules (Id, Code, ParametresJson, Version, DateEffet, Actif, ModifieParId)
    SELECT Id, Code, ParametresJson, Version, DateEffet, Actif, ModifieParId
    FROM dbo.DecisionRules;
END
GO

CREATE OR ALTER PROCEDURE stg.usp_Load_AuditLogs
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM stg.AuditLogs;
    INSERT INTO stg.AuditLogs (Id, EntityType, EntityId, Action, AncienneValeur, NouvelleValeur, UtilisateurId, DateAction)
    SELECT Id, EntityType, EntityId, Action, AncienneValeur, NouvelleValeur, UtilisateurId, DateAction
    FROM dbo.AuditLogs;
END
GO

/* ============================================================================
   Orchestrator — reloads every staging table in one call.

   FK enforcement is suspended for the duration of the reload (so the 43
   procs above can run in any order without regard to dependency), then
   re-enabled WITH CHECK so every constraint is revalidated against the
   freshly loaded data before the transaction commits. If any row would
   violate a constraint, the CHECK CONSTRAINT step fails and the whole
   reload rolls back atomically.
   ============================================================================ */

CREATE OR ALTER PROCEDURE stg.usp_LoadAllStaging
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @sql NVARCHAR(MAX) = N'';

        SELECT @sql = @sql + 'ALTER TABLE stg.' + QUOTENAME(t.name) + ' NOCHECK CONSTRAINT ALL;' + CHAR(10)
        FROM sys.tables t
        WHERE SCHEMA_NAME(t.schema_id) = 'stg';
        EXEC sp_executesql @sql;

        -- HR master data
        EXEC stg.usp_Load_Users;
        EXEC stg.usp_Load_Departments;
        EXEC stg.usp_Load_SubDepartments;
        EXEC stg.usp_Load_Positions;
        EXEC stg.usp_Load_Grades;
        EXEC stg.usp_Load_BusinessUnits;
        EXEC stg.usp_Load_Locations;
        EXEC stg.usp_Load_ContractTypes;
        EXEC stg.usp_Load_GradeReferentiels;
        EXEC stg.usp_Load_SystemParameters;
        EXEC stg.usp_Load_Parametres;
        EXEC stg.usp_Load_PositionRequiredCompetences;
        EXEC stg.usp_Load_PositionMandatoryFormations;
        EXEC stg.usp_Load_PositionGradeEligibilities;

        -- Core HR
        EXEC stg.usp_Load_Collaborateurs;
        EXEC stg.usp_Load_CategoriesCompetences;
        EXEC stg.usp_Load_Competences;
        EXEC stg.usp_Load_CompetencesRequisesParPoste;
        EXEC stg.usp_Load_EvaluationsHistoriques;
        EXEC stg.usp_Load_Formations;
        EXEC stg.usp_Load_FormationCompetences;
        EXEC stg.usp_Load_Inscriptions;
        EXEC stg.usp_Load_EvaluationsCompetences;
        EXEC stg.usp_Load_EvaluationsPostFormation;
        EXEC stg.usp_Load_EvaluationsSuiviFormation;
        EXEC stg.usp_Load_PlansDeveloppement;

        -- Talent management
        EXEC stg.usp_Load_ReviewCycles;
        EXEC stg.usp_Load_TalentEvaluations;
        EXEC stg.usp_Load_OKRs;
        EXEC stg.usp_Load_KeyResults;

        -- Succession planning
        EXEC stg.usp_Load_SuccessionPlans;
        EXEC stg.usp_Load_SuccessorRankingSnapshots;

        -- Skill ontology
        EXEC stg.usp_Load_SkillCategories;
        EXEC stg.usp_Load_Skills;
        EXEC stg.usp_Load_SkillAliases;
        EXEC stg.usp_Load_SkillLevels;
        EXEC stg.usp_Load_SkillCriticalities;
        EXEC stg.usp_Load_SkillVersions;
        EXEC stg.usp_Load_SkillRelations;

        -- Certifications
        EXEC stg.usp_Load_Certifications;
        EXEC stg.usp_Load_CollaborateurCertifications;

        -- Governance & audit
        EXEC stg.usp_Load_DecisionRules;
        EXEC stg.usp_Load_AuditLogs;

        SET @sql = N'';
        SELECT @sql = @sql + 'ALTER TABLE stg.' + QUOTENAME(t.name) + ' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
        FROM sys.tables t
        WHERE SCHEMA_NAME(t.schema_id) = 'stg';
        EXEC sp_executesql @sql;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
