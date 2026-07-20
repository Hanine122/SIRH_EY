-- ============================================================================
-- SIRH.EY - ETL reel : dbo (application) -> dwh (analyse)
-- Instance reelle : localhost\SQLEXPRESS, base SIRH_EY (21 tables dans dbo,
-- verifie via sqlcmd -- pas de dbo.Certifications, pas de colonnes de
-- certification sur Formations/Inscriptions).
-- Prerequis : executer 008_DWH_Schema_And_Cleanup.sql juste avant (7 tables
--             dwh recreees vides, contraintes FK en place).
--
-- Perimetre source reellement present sur cette instance :
--   dbo.Collaborateurs   (Id, Nom, Prenom, Poste, Departement, Actif, ...)
--   dbo.Competences      (Id, Nom, NiveauActuel, NiveauCible, DateEvaluation,
--                         CollaborateurId, CategorieCompetenceId)
--   dbo.CategoriesCompetences (Id, Nom)
--   dbo.Formations       (Id, Titre, DureeHeures, DateDebut, ... -- pas de
--                         CertificationNom / EstCertifiante sur cette instance)
--   dbo.Inscriptions     (Id, DateInscription, Terminee, CollaborateurId,
--                         FormationId, DateExamen, Progression -- pas de
--                         DateCompletion / DateExpiration / SourceCertification)
--   dbo.TalentEvaluations (Id, CollaborateurId, PerformanceScore,
--                         PotentielScore, DateEvaluation, ...)
--
-- Mapping source -> cible :
--   dbo.Collaborateurs                              -> dwh.dim_collaborateur
--   dbo.Competences (catalogue deduplique par Nom)   -> dwh.dim_competence
--       categorie = dbo.CategoriesCompetences.Nom
--       src_competence_id = MIN(Competences.Id) du groupe (indicatif)
--   dbo.Formations                                    -> dwh.dim_formation
--       duree = DureeHeures
--   dbo.TalentEvaluations                             -> dwh.fact_evaluations_talent
--   dbo.Competences                                   -> dwh.fact_competences
--       niveau = NiveauActuel
--   dbo.Inscriptions                                  -> dwh.fact_formations
--       date_key = DateInscription (seule date d'inscription disponible ici)
--       (pas de cert_key : dbo.Certifications n'existe pas sur cette instance)
-- ============================================================================

USE SIRH_EY;
GO

SET NOCOUNT ON;

-- ============================================================================
-- 1. dim_temps EN PREMIER : peuplee sur la plage reelle des dates sources
--    (avec marge d'un mois de chaque cote). Seules les colonnes de date qui
--    existent reellement sur cette instance sont utilisees.
-- ============================================================================

DECLARE @minDate DATE, @maxDate DATE;

SELECT @minDate = MIN(d), @maxDate = MAX(d)
FROM (
    SELECT CAST(DateEvaluation AS DATE) AS d FROM dbo.TalentEvaluations
    UNION ALL
    SELECT CAST(DateEvaluation AS DATE) FROM dbo.Competences
    UNION ALL
    SELECT CAST(DateInscription AS DATE) FROM dbo.Inscriptions
    UNION ALL
    SELECT CAST(DateExamen AS DATE) FROM dbo.Inscriptions WHERE DateExamen IS NOT NULL
) all_dates;

SET @minDate = DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(@minDate), MONTH(@minDate), 1));
SET @maxDate = EOMONTH(DATEADD(MONTH, 1, @maxDate));

;WITH CTE_Dates AS (
    SELECT @minDate AS dt
    UNION ALL
    SELECT DATEADD(DAY, 1, dt) FROM CTE_Dates WHERE dt < @maxDate
)
INSERT INTO dwh.dim_temps (date_key, annee, mois, jour)
SELECT
    CAST(FORMAT(dt, 'yyyyMMdd') AS INT),
    YEAR(dt),
    MONTH(dt),
    DAY(dt)
FROM CTE_Dates
OPTION (MAXRECURSION 2000);
GO

-- ============================================================================
-- 2. DIMENSIONS
-- ============================================================================

-- dim_collaborateur (grain = 1 par dbo.Collaborateurs.Id)
INSERT INTO dwh.dim_collaborateur (src_collaborateur_id, nom, prenom, poste, departement, actif)
SELECT
    c.Id,
    c.Nom,
    c.Prenom,
    c.Poste,
    c.Departement,
    c.Actif
FROM dbo.Collaborateurs c;
GO

-- dim_competence (catalogue deduplique : une ligne par nom de competence ;
-- src_competence_id = plus petit Id du groupe, a titre indicatif uniquement)
INSERT INTO dwh.dim_competence (src_competence_id, nom, categorie)
SELECT
    MIN(co.Id),
    co.Nom,
    MAX(cat.Nom)
FROM dbo.Competences co
LEFT JOIN dbo.CategoriesCompetences cat ON co.CategorieCompetenceId = cat.Id
GROUP BY co.Nom;
GO

-- dim_formation
INSERT INTO dwh.dim_formation (src_formation_id, titre, duree)
SELECT Id, Titre, DureeHeures
FROM dbo.Formations;
GO

-- ============================================================================
-- 3. FAITS
-- ============================================================================

-- fact_evaluations_talent (grain = 1 par dbo.TalentEvaluations.Id)
INSERT INTO dwh.fact_evaluations_talent (collab_key, date_key, performance, potentiel)
SELECT
    dc.collab_key,
    CAST(FORMAT(te.DateEvaluation, 'yyyyMMdd') AS INT),
    te.PerformanceScore,
    te.PotentielScore
FROM dbo.TalentEvaluations te
INNER JOIN dwh.dim_collaborateur dc ON dc.src_collaborateur_id = te.CollaborateurId;
GO

-- fact_competences (grain = 1 par dbo.Competences.Id, soit 1 evaluation
-- de competence pour 1 collaborateur)
INSERT INTO dwh.fact_competences (collab_key, competence_key, date_key, niveau)
SELECT
    dc.collab_key,
    dcp.competence_key,
    CAST(FORMAT(co.DateEvaluation, 'yyyyMMdd') AS INT),
    co.NiveauActuel
FROM dbo.Competences co
INNER JOIN dwh.dim_collaborateur dc ON dc.src_collaborateur_id = co.CollaborateurId
INNER JOIN dwh.dim_competence dcp   ON dcp.nom = co.Nom;
GO

-- fact_formations (grain = 1 par dbo.Inscriptions.Id)
-- date_key = DateInscription : seule colonne de date fiable disponible ici
-- (pas de DateCompletion sur cette instance)
INSERT INTO dwh.fact_formations (collab_key, formation_key, date_key, terminee)
SELECT
    dc.collab_key,
    df.formation_key,
    CAST(FORMAT(i.DateInscription, 'yyyyMMdd') AS INT),
    i.Terminee
FROM dbo.Inscriptions i
INNER JOIN dwh.dim_collaborateur dc ON dc.src_collaborateur_id = i.CollaborateurId
INNER JOIN dwh.dim_formation df     ON df.src_formation_id = i.FormationId;
GO

-- ============================================================================
-- 4. RECAP
-- ============================================================================

SELECT 'dim_collaborateur' AS table_name, COUNT(*) AS lignes FROM dwh.dim_collaborateur
UNION ALL SELECT 'dim_competence', COUNT(*) FROM dwh.dim_competence
UNION ALL SELECT 'dim_formation', COUNT(*) FROM dwh.dim_formation
UNION ALL SELECT 'dim_temps', COUNT(*) FROM dwh.dim_temps
UNION ALL SELECT 'fact_evaluations_talent', COUNT(*) FROM dwh.fact_evaluations_talent
UNION ALL SELECT 'fact_competences', COUNT(*) FROM dwh.fact_competences
UNION ALL SELECT 'fact_formations', COUNT(*) FROM dwh.fact_formations;
GO
