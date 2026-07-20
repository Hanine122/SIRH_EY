-- ============================================================================
-- SIRH.EY - Architecture BI - 002_ETL.sql
-- Procedures ETL (schemas techniques dim/fact en anglais ; noms de
-- procedures, colonnes et commentaires en francais).
-- Prerequis : 001_Staging.sql et 003_DataWarehouse.sql deja executes.
--
-- PARTIE A - dbo -> stg : audit + auto-reparation idempotente (43 procedures
-- deja existantes, cf. Database/Staging/002_PopulateStagingProcedures.sql).
-- PARTIE B - stg -> dim/fact : toutes nouvelles (schema dim/fact inexistant
-- avant 003_DataWarehouse.sql).
-- ============================================================================

USE SIRH_EY;
GO

-- ============================================================================
-- PARTIE A - dbo -> stg : audit + auto-reparation (inchange, deja en francais)
-- ============================================================================

DECLARE @tbl NVARCHAR(256), @procName NVARCHAR(300), @cols NVARCHAR(MAX), @sql NVARCHAR(MAX);

DECLARE curseur_procs_manquantes CURSOR LOCAL FAST_FORWARD FOR
SELECT t.name
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'stg' AND t.name <> 'Users'
AND NOT EXISTS (
    SELECT 1 FROM sys.procedures p JOIN sys.schemas ps ON p.schema_id = ps.schema_id
    WHERE ps.name = 'stg' AND p.name = 'usp_Load_' + t.name
)
AND EXISTS (SELECT 1 FROM sys.tables d JOIN sys.schemas ds ON d.schema_id = ds.schema_id WHERE ds.name = 'dbo' AND d.name = t.name);

OPEN curseur_procs_manquantes;
FETCH NEXT FROM curseur_procs_manquantes INTO @tbl;

IF @@FETCH_STATUS <> 0
    PRINT N'Audit dbo->stg : les 43 procedures usp_Load_* existent deja. Rien a creer.';

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @procName = 'usp_Load_' + @tbl;
    SELECT @cols = STRING_AGG(QUOTENAME(c1.name), ', ')
    FROM sys.columns c1
    WHERE c1.object_id = OBJECT_ID('dbo.' + @tbl)
    AND EXISTS (SELECT 1 FROM sys.columns c2 WHERE c2.object_id = OBJECT_ID('stg.' + @tbl) AND c2.name = c1.name);

    SET @sql = N'CREATE OR ALTER PROCEDURE stg.' + QUOTENAME(@procName) + N' AS
BEGIN
    SET NOCOUNT ON;
    TRUNCATE TABLE stg.' + QUOTENAME(@tbl) + N';
    INSERT INTO stg.' + QUOTENAME(@tbl) + N' (' + @cols + N')
    SELECT ' + @cols + N' FROM dbo.' + QUOTENAME(@tbl) + N';
END';
    EXEC sp_executesql @sql;
    PRINT N'Cree (manquant) : stg.' + @procName;

    FETCH NEXT FROM curseur_procs_manquantes INTO @tbl;
END

CLOSE curseur_procs_manquantes;
DEALLOCATE curseur_procs_manquantes;
GO

-- ============================================================================
-- PARTIE B - stg -> dim : une procedure par dimension
-- ============================================================================

-- Nettoyage des anciennes procedures anglaises (iteration precedente, avant
-- francisation)
DROP PROCEDURE IF EXISTS dim.usp_Load_Date, dim.usp_Load_Collaborateur, dim.usp_Load_Organisation,
    dim.usp_Load_Grade, dim.usp_Load_BusinessUnit, dim.usp_Load_Location, dim.usp_Load_Skill,
    dim.usp_Load_Formation, dim.usp_Load_ReviewCycle;
DROP PROCEDURE IF EXISTS fact.usp_Load_Skills, fact.usp_Load_Training, fact.usp_Load_Talent,
    fact.usp_Load_Promotion, fact.usp_Load_Succession, fact.usp_Load_DWH_Full;
GO

-- dim.Calendrier : calendrier statique 2020-2035, peuple une seule fois
CREATE OR ALTER PROCEDURE dim.usp_ChargerCalendrier
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dim.Calendrier) RETURN;

    ;WITH CTE_Dates AS (
        SELECT CAST('2020-01-01' AS DATE) AS dt
        UNION ALL
        SELECT DATEADD(DAY, 1, dt) FROM CTE_Dates WHERE dt < '2035-12-31'
    )
    INSERT INTO dim.Calendrier (CleDate, [Date], Annee, Trimestre, LibelleTrimestre, Mois, NomMois, Semaine, JourSemaine, NomJour, EstWeekEnd)
    SELECT
        CAST(FORMAT(dt, 'yyyyMMdd') AS INT),
        dt,
        YEAR(dt),
        DATEPART(QUARTER, dt),
        'T' + CAST(DATEPART(QUARTER, dt) AS VARCHAR),
        MONTH(dt),
        DATENAME(MONTH, dt),
        DATEPART(ISO_WEEK, dt),
        DATEPART(WEEKDAY, dt),
        DATENAME(WEEKDAY, dt),
        CASE WHEN DATEPART(WEEKDAY, dt) IN (1, 7) THEN 1 ELSE 0 END
    FROM CTE_Dates
    OPTION (MAXRECURSION 0);   -- 2020-01-01..2035-12-31 = 5843 recursions ; 5000 (valeur precedente) faisait echouer l'INSERT (erreur 530)
END;
GO

-- dim.Collaborateur : SCD Type 2
CREATE OR ALTER PROCEDURE dim.usp_ChargerCollaborateur
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Ferme les versions courantes dont un attribut suivi a change
    UPDATE d
    SET d.DateFin = CAST(GETDATE() AS DATE), d.EstVersionCourante = 0
    FROM dim.Collaborateur d
    JOIN stg.Collaborateurs s ON s.Id = d.CollaborateurId
    WHERE d.EstVersionCourante = 1
    AND (
        ISNULL(d.Nom, '') <> ISNULL(s.Nom, '')
        OR ISNULL(d.Prenom, '') <> ISNULL(s.Prenom, '')
        OR d.Actif <> s.Actif
        OR ISNULL(d.Statut, -1) <> ISNULL(s.Statut, -1)
        OR ISNULL(d.PositionId, -1) <> ISNULL(s.PositionId, -1)
        OR ISNULL(d.GradeId, -1) <> ISNULL(s.GradeId, -1)
        OR ISNULL(d.UniteAffairesId, -1) <> ISNULL(s.BusinessUnitId, -1)
        OR ISNULL(d.LocalisationId, -1) <> ISNULL(s.LocationId, -1)
        OR ISNULL(d.ManagerId, -1) <> ISNULL(s.ManagerId, -1)
        OR ISNULL(d.PotentielCarriere, '') <> ISNULL(s.PotentielCarriere, '')
    );

    -- 2. Insere une version courante pour les collaborateurs nouveaux OU
    --    dont la version vient d'etre fermee ci-dessus
    INSERT INTO dim.Collaborateur (
        CollaborateurId, Nom, Prenom, Actif, Statut, DateEmbauche,
        PotentielCarriere, NiveauPreparationSuccession, ManagerId,
        PositionId, GradeId, UniteAffairesId, LocalisationId,
        DateEffective, DateFin, EstVersionCourante
    )
    SELECT
        s.Id, s.Nom, s.Prenom, s.Actif, s.Statut, CAST(s.DateEmbauche AS DATE),
        s.PotentielCarriere, s.NiveauPreparationSuccession, s.ManagerId,
        s.PositionId, s.GradeId, s.BusinessUnitId, s.LocationId,
        CAST(GETDATE() AS DATE), NULL, 1
    FROM stg.Collaborateurs s
    WHERE NOT EXISTS (
        SELECT 1 FROM dim.Collaborateur d WHERE d.CollaborateurId = s.Id AND d.EstVersionCourante = 1
    );
END;
GO

-- dim.Organisation : SCD Type 1
CREATE OR ALTER PROCEDURE dim.usp_ChargerOrganisation
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.Organisation;
    INSERT INTO dim.Organisation (PositionId, NomPoste, SousDepartementId, NomSousDepartement, DepartementId, NomDepartement)
    SELECT p.Id, p.Name, sd.Id, sd.Name, d.Id, d.Name
    FROM stg.Positions p
    LEFT JOIN stg.SubDepartments sd ON sd.Id = p.SubDepartmentId
    LEFT JOIN stg.Departments d     ON d.Id = sd.DepartmentId;
END;
GO

-- dim.Grade : SCD Type 1
CREATE OR ALTER PROCEDURE dim.usp_ChargerGrade
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.Grade;
    INSERT INTO dim.Grade (GradeId, Nom, Niveau, NiveauMinCompetences, AncienneteMinAns, NombreImplementationsMin, ExperienceDomainMinAns, GradeSuivant)
    SELECT g.Id, g.Name, g.Level, gr.NiveauMinCompetences, gr.AncienneteMinAns, gr.NombreImplementationsMin, gr.ExperienceDomainMinAns, gr.GradeSuivant
    FROM stg.Grades g
    LEFT JOIN stg.GradeReferentiels gr ON gr.Grade = g.Name;
END;
GO

-- dim.UniteAffaires : SCD Type 1
CREATE OR ALTER PROCEDURE dim.usp_ChargerUniteAffaires
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.UniteAffaires;
    INSERT INTO dim.UniteAffaires (UniteAffairesId, Nom)
    SELECT Id, Name FROM stg.BusinessUnits;
END;
GO

-- dim.Localisation : SCD Type 1
CREATE OR ALTER PROCEDURE dim.usp_ChargerLocalisation
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.Localisation;
    INSERT INTO dim.Localisation (LocalisationId, Nom, Ville, Pays)
    SELECT Id, Name, City, Country FROM stg.Locations;
END;
GO

-- dim.Competence : SCD Type 1 (catalogue Skills + SkillCategories + derniere criticite)
CREATE OR ALTER PROCEDURE dim.usp_ChargerCompetence
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.Competence;
    INSERT INTO dim.Competence (CompetenceId, Nom, NomCategorie, CodeCriticite, LibelleCriticite, PoidsCriticite)
    SELECT
        sk.Id, sk.Nom, cat.Nom,
        crit.Niveau,
        CASE crit.Niveau WHEN 3 THEN N'Strategique' WHEN 2 THEN N'Elevee' WHEN 1 THEN N'Moyenne' WHEN 0 THEN N'Faible' END,
        CASE crit.Niveau WHEN 3 THEN 4 WHEN 2 THEN 3 WHEN 1 THEN 2 WHEN 0 THEN 1 ELSE 2 END
    FROM stg.Skills sk
    LEFT JOIN stg.SkillCategories cat ON cat.Id = sk.SkillCategoryId
    OUTER APPLY (
        SELECT TOP 1 sc.Niveau
        FROM stg.SkillCriticalities sc
        WHERE sc.SkillId = sk.Id
        ORDER BY sc.DateEvaluation DESC
    ) crit;
END;
GO

-- dim.Formation : SCD Type 1
CREATE OR ALTER PROCEDURE dim.usp_ChargerFormation
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.Formation;
    INSERT INTO dim.Formation (FormationId, Titre, Categorie, Plateforme, EstCertifiante, CapaciteMax)
    SELECT Id, Titre, Categorie, Plateforme, EstCertifiante, CapaciteMax FROM stg.Formations;
END;
GO

-- dim.CycleEvaluation : SCD Type 1
CREATE OR ALTER PROCEDURE dim.usp_ChargerCycleEvaluation
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dim.CycleEvaluation;
    INSERT INTO dim.CycleEvaluation (CycleEvaluationId, Nom, DateDebut, DateFin, Statut)
    SELECT Id, Nom, CAST(DateDebut AS DATE), CAST(DateFin AS DATE), Statut FROM stg.ReviewCycles;
END;
GO

-- ============================================================================
-- PARTIE B (suite) - stg (+dim) -> fact
-- ============================================================================

CREATE OR ALTER PROCEDURE fact.usp_ChargerEvaluationCompetences
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM fact.EvaluationCompetences;
    INSERT INTO fact.EvaluationCompetences (CompetenceEvalueeId, CleCollaborateur, CleCompetence, CleDate, NiveauActuel, NiveauCible, Ecart, AtteintCible, SeveriteEcart)
    SELECT
        c.Id, dc.CleCollaborateur, dcp.CleCompetence,
        CAST(FORMAT(c.DateEvaluation, 'yyyyMMdd') AS INT),
        c.NiveauActuel, c.NiveauCible, (c.NiveauCible - c.NiveauActuel),
        CASE WHEN c.NiveauActuel >= c.NiveauCible THEN 1 ELSE 0 END,
        CASE
            WHEN (c.NiveauCible - c.NiveauActuel) >= 2 THEN N'Critique'
            WHEN (c.NiveauCible - c.NiveauActuel) = 1 THEN N'Alerte'
            ELSE N'OK'
        END
    FROM stg.Competences c
    JOIN dim.Collaborateur dc ON dc.CollaborateurId = c.CollaborateurId AND dc.EstVersionCourante = 1
    LEFT JOIN dim.Competence dcp ON dcp.CompetenceId = c.SkillId
    -- Garde d'integrite : n'insere que si la date existe dans dim.Calendrier
    -- (evite qu'une date hors plage 2020-2035 fasse echouer tout l'INSERT par
    -- violation de FK -- exclut la seule ligne concernee plutot que tout le lot)
    JOIN dim.Calendrier cal ON cal.CleDate = CAST(FORMAT(c.DateEvaluation, 'yyyyMMdd') AS INT);

    IF @@ROWCOUNT = 0 AND EXISTS (SELECT 1 FROM stg.Competences)
        PRINT N'Attention : fact.EvaluationCompetences vide malgre des lignes source -- verifier dim.Calendrier / dim.Collaborateur.';
END;
GO

CREATE OR ALTER PROCEDURE fact.usp_ChargerFormations
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM fact.Formation;
    INSERT INTO fact.Formation (
        InscriptionId, CleCollaborateur, CleFormation, CleDateInscription, CleDateCompletion,
        Terminee, Progression, NoteGlobaleChaud, NoteContenuChaud, NoteFormateurChaud, RecommandeChaud,
        NoteApplicationFroid, NoteImpactBusinessFroid, TauxUtilisationCapacite
    )
    SELECT
        i.Id, dc.CleCollaborateur, df.CleFormation,
        cali.CleDate,
        calc.CleDate,   -- NULL si DateCompletion absente OU hors plage dim.Calendrier
        i.Terminee, i.Progression,
        pf.NoteGlobale, pf.NoteContenu, pf.NoteFormateur, pf.Recommande,
        sf.NoteApplicationCompetences, sf.NoteImpactBusiness,
        CASE WHEN f.CapaciteMax > 0 THEN CAST(f.PlacesPrises AS FLOAT) / f.CapaciteMax END
    FROM stg.Inscriptions i
    JOIN dim.Collaborateur dc ON dc.CollaborateurId = i.CollaborateurId AND dc.EstVersionCourante = 1
    JOIN dim.Formation df     ON df.FormationId = i.FormationId
    JOIN stg.Formations f     ON f.Id = i.FormationId
    LEFT JOIN stg.EvaluationsPostFormation pf  ON pf.InscriptionId = i.Id
    LEFT JOIN stg.EvaluationsSuiviFormation sf ON sf.InscriptionId = i.Id
    -- Garde d'integrite : CleDateInscription doit exister (NOT NULL FK) ;
    -- CleDateCompletion reste NULL si absente ou hors plage (evite un echec
    -- FK sur une date de completion invalide plutot qu'exclure toute la ligne)
    JOIN dim.Calendrier cali      ON cali.CleDate = CAST(FORMAT(i.DateInscription, 'yyyyMMdd') AS INT)
    LEFT JOIN dim.Calendrier calc ON calc.CleDate = CAST(FORMAT(i.DateCompletion, 'yyyyMMdd') AS INT);
END;
GO

CREATE OR ALTER PROCEDURE fact.usp_ChargerEvaluationTalent
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM fact.EvaluationTalent;
    INSERT INTO fact.EvaluationTalent (
        EvaluationTalentId, CleCollaborateur, CleCycleEvaluation, CleDate,
        ScorePerformance, ScorePotentiel, Code9Boites, CodeStatutEvaluation,
        Actif, TotalOKR, OKRTermines
    )
    SELECT
        te.Id, dc.CleCollaborateur, dce.CleCycleEvaluation,
        cal.CleDate,
        te.PerformanceScore, te.PotentielScore, te.Category, te.Statut, te.Actif,
        okr.TotalOKR, okr.OKRTermines
    FROM stg.TalentEvaluations te
    JOIN dim.Collaborateur dc ON dc.CollaborateurId = te.CollaborateurId AND dc.EstVersionCourante = 1
    LEFT JOIN dim.CycleEvaluation dce ON dce.CycleEvaluationId = te.ReviewCycleId
    OUTER APPLY (
        SELECT
            COUNT(*)                                      AS TotalOKR,
            SUM(CASE WHEN o.Statut = 4 THEN 1 ELSE 0 END) AS OKRTermines
        FROM stg.OKRs o
        WHERE o.CollaborateurId = te.CollaborateurId
    ) okr
    -- Garde d'integrite : CleDate doit exister dans dim.Calendrier (NOT NULL FK)
    JOIN dim.Calendrier cal ON cal.CleDate = CAST(FORMAT(te.DateEvaluation, 'yyyyMMdd') AS INT);
END;
GO

-- fact.Promotion : photo courante par collaborateur actif -> CleDateSnapshot
-- = date d'execution de l'ETL (correction Kimball : un fait periodique doit
-- porter sa date de photo)
CREATE OR ALTER PROCEDURE fact.usp_ChargerPromotion
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM fact.Promotion;

    DECLARE @cleDateAujourdhui INT = CAST(FORMAT(GETDATE(), 'yyyyMMdd') AS INT);

    IF NOT EXISTS (SELECT 1 FROM dim.Calendrier WHERE CleDate = @cleDateAujourdhui)
    BEGIN
        RAISERROR(N'dim.Calendrier ne couvre pas la date du jour : executer dim.usp_ChargerCalendrier avant fact.usp_ChargerPromotion.', 16, 1);
        RETURN;
    END

    INSERT INTO fact.Promotion (
        CleCollaborateur, CleGrade, CleDateSnapshot, AncienneteAnnees, MoyenneNiveauActuel, MoyenneNiveauCible,
        TauxAtteinteCompetences, DernierScorePerformance, DernierScorePotentiel, BandeEligibilite
    )
    SELECT
        dc.CleCollaborateur, dg.CleGrade, @cleDateAujourdhui,
        CAST(DATEDIFF(DAY, col.DateEmbauche, GETDATE()) / 365.25 AS FLOAT),
        comp.MoyenneNiveauActuel, comp.MoyenneNiveauCible,
        CASE WHEN comp.MoyenneNiveauCible > 0 THEN comp.MoyenneNiveauActuel / comp.MoyenneNiveauCible END,
        te.PerformanceScore, te.PotentielScore,
        CASE
            WHEN comp.MoyenneNiveauActuel IS NULL THEN N'Donnees insuffisantes'
            WHEN (DATEDIFF(DAY, col.DateEmbauche, GETDATE()) / 365.25) >= ISNULL(gr.AncienneteMinAns, 0)
                 AND comp.MoyenneNiveauActuel >= ISNULL(gr.NiveauMinCompetences, 0)
                THEN N'Pret'
            WHEN (DATEDIFF(DAY, col.DateEmbauche, GETDATE()) / 365.25) >= ISNULL(gr.AncienneteMinAns, 0) * 0.75
                 OR comp.MoyenneNiveauActuel >= ISNULL(gr.NiveauMinCompetences, 0) * 0.85
                THEN N'En developpement'
            ELSE N'Pas pret'
        END
    FROM stg.Collaborateurs col
    JOIN dim.Collaborateur dc ON dc.CollaborateurId = col.Id AND dc.EstVersionCourante = 1
    LEFT JOIN dim.Grade dg ON dg.GradeId = col.GradeId
    LEFT JOIN stg.GradeReferentiels gr ON gr.Grade = col.Grade
    OUTER APPLY (
        SELECT
            AVG(CAST(c.NiveauActuel AS FLOAT)) AS MoyenneNiveauActuel,
            AVG(CAST(c.NiveauCible AS FLOAT))  AS MoyenneNiveauCible
        FROM stg.Competences c
        WHERE c.CollaborateurId = col.Id
    ) comp
    OUTER APPLY (
        SELECT TOP 1 t.PerformanceScore, t.PotentielScore
        FROM stg.TalentEvaluations t
        WHERE t.CollaborateurId = col.Id
        ORDER BY t.DateEvaluation DESC
    ) te
    WHERE col.Actif = 1;
END;
GO

CREATE OR ALTER PROCEDURE fact.usp_ChargerSuccession
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM fact.Succession;
    INSERT INTO fact.Succession (
        SuccessionPlanId, IdSnapshot, CleCollaborateurTitulaire, CleCollaborateurCandidat,
        CleDateCreation, CleDateSnapshot, CodeStatutPlan, Rang, ScoreSuccession, ScoreCouverture, CodeHorizonPreparation
    )
    SELECT
        sp.Id, srs.Id,
        tit.CleCollaborateur, cand.CleCollaborateur,
        calCreation.CleDate,
        calSnapshot.CleDate,
        sp.Statut, srs.Rang, srs.ScoreSuccession, srs.ScoreCouverture, srs.Horizon
    FROM stg.SuccessionPlans sp
    LEFT JOIN dim.Collaborateur tit             ON tit.CollaborateurId = sp.CollaborateurTitulaireId AND tit.EstVersionCourante = 1
    LEFT JOIN stg.SuccessorRankingSnapshots srs ON srs.SuccessionPlanId = sp.Id
    LEFT JOIN dim.Collaborateur cand            ON cand.CollaborateurId = srs.CandidatId AND cand.EstVersionCourante = 1
    -- Garde d'integrite (colonnes nullable : NULL si date absente ou hors
    -- plage dim.Calendrier, plutot qu'une valeur qui violerait la FK)
    LEFT JOIN dim.Calendrier calCreation ON calCreation.CleDate = CAST(FORMAT(sp.DateCreation, 'yyyyMMdd') AS INT)
    LEFT JOIN dim.Calendrier calSnapshot ON calSnapshot.CleDate = CAST(FORMAT(srs.DateSnapshot, 'yyyyMMdd') AS INT);
END;
GO

-- ============================================================================
-- ORCHESTRATEUR MAITRE
-- Ordre obligatoire : vider les FAITS avant de recharger les DIMENSIONS SCD1
-- (sinon DELETE sur une dimension echoue tant qu'une ancienne ligne de fait
-- la reference encore).
-- ============================================================================

CREATE OR ALTER PROCEDURE fact.usp_ChargerEntrepotComplet
AS
BEGIN
    SET NOCOUNT ON;
    PRINT N'=== ETL dim/fact - debut ===';

    PRINT N'>> Purge des faits (avant rechargement des dimensions)';
    DELETE FROM fact.Succession;
    DELETE FROM fact.Promotion;
    DELETE FROM fact.EvaluationTalent;
    DELETE FROM fact.Formation;
    DELETE FROM fact.EvaluationCompetences;

    PRINT N'>> Rechargement des dimensions';
    EXEC dim.usp_ChargerCalendrier;
    EXEC dim.usp_ChargerCollaborateur;
    EXEC dim.usp_ChargerOrganisation;
    EXEC dim.usp_ChargerGrade;
    EXEC dim.usp_ChargerUniteAffaires;
    EXEC dim.usp_ChargerLocalisation;
    EXEC dim.usp_ChargerCompetence;
    EXEC dim.usp_ChargerFormation;
    EXEC dim.usp_ChargerCycleEvaluation;

    PRINT N'>> Rechargement des faits';
    EXEC fact.usp_ChargerEvaluationCompetences;
    EXEC fact.usp_ChargerFormations;
    EXEC fact.usp_ChargerEvaluationTalent;
    EXEC fact.usp_ChargerPromotion;
    EXEC fact.usp_ChargerSuccession;

    PRINT N'=== ETL dim/fact - termine ===';
END;
GO
