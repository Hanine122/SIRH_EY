-- ============================================================================
-- SIRH.EY - Architecture BI - 005_Seed.sql
-- Seed UNIQUEMENT des tables de reference vides (jamais dbo ; jamais les
-- tables transactionnelles vides, cf. justification ci-dessous).
--
-- AUDIT des tables stg vides :
--   Reference (seedees ici)      : ReviewCycles, SkillCategories, SkillCriticalities
--   Transactionnel (NON seedees) : AuditLogs, OKRs, KeyResults, SuccessionPlans,
--                                  SuccessorRankingSnapshots, EvaluationsSuiviFormation,
--                                  DecisionRules, FormationCompetences,
--                                  PositionGradeEligibilities, PositionMandatoryFormations,
--                                  PositionRequiredCompetences, SkillAliases,
--                                  SkillLevels, SkillRelations, SkillVersions
--   -- fabriquer des lignes plausibles pour des evenements RH (decisions,
--   audits, plans de succession, OKRs de vraies personnes) reviendrait a
--   inventer de faux faits metier, pas a peupler un referentiel.
--
-- IMPORTANT - fragilite assumee : stg est rechargee en TRUNCATE+INSERT par
-- stg.usp_Load_ReviewCycles / usp_Load_SkillCategories / usp_Load_SkillCriticalities.
-- Comme dbo est interdit de modification, ces lignes seedees sont
-- TEMPORAIRES : le prochain stg.usp_LoadAllStaging les effacera (dbo restant
-- vide pour ces 3 tables). Pour les rendre durables, il faudrait les saisir
-- via l'application (dbo), hors perimetre de ce script.
-- ============================================================================

USE SIRH_EY;
GO

-- ============================================================================
-- 1. stg.ReviewCycles
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM stg.ReviewCycles)
BEGIN
    INSERT INTO stg.ReviewCycles (Id, Nom, DateDebut, DateFin, Statut, Perimetre)
    VALUES
        (1, N'Cycle Annuel 2026',          '2026-01-01', '2026-12-31', 1, N'Global'),
        (2, N'Revue Semestrielle H1 2026', '2026-01-01', '2026-06-30', 2, N'Global'),
        (3, N'Revue Semestrielle H2 2026', '2026-07-01', '2026-12-31', 0, N'Global');
    PRINT N'Seed : stg.ReviewCycles (3 lignes)';
END
ELSE PRINT N'IGNORE (non vide) : stg.ReviewCycles';
GO

-- ============================================================================
-- 2. stg.SkillCategories
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM stg.SkillCategories)
BEGIN
    INSERT INTO stg.SkillCategories (Id, Nom, Description, ParentCategoryId)
    VALUES
        (1, N'Technique',                N'Competences techniques et outils',                          NULL),
        (2, N'Fonctionnel',               N'Connaissance metier et processus',                          NULL),
        (3, N'Leadership & Management',   N'Encadrement, gestion d''equipe, mentorat',                   NULL),
        (4, N'Certification',             N'Competences liees a une certification professionnelle',      NULL),
        (5, N'Savoir-etre',                N'Communication, relationnel, adaptabilite',                   NULL),
        (6, N'Outils & Plateformes',       N'Maitrise d''outils logiciels specifiques',                    1);
    PRINT N'Seed : stg.SkillCategories (6 lignes)';
END
ELSE PRINT N'IGNORE (non vide) : stg.SkillCategories';
GO

-- ============================================================================
-- 3. stg.SkillCriticalities (1 ligne par competence reellement presente dans
--    stg.Skills -- aucun Id invente)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM stg.SkillCriticalities) AND EXISTS (SELECT 1 FROM stg.Skills)
BEGIN
    INSERT INTO stg.SkillCriticalities (Id, SkillId, Niveau, Justification, Perimetre, DateEvaluation)
    SELECT
        ROW_NUMBER() OVER (ORDER BY sk.Id),
        sk.Id,
        sk.Id % 4,   -- 0=Faible, 1=Moyenne, 2=Elevee, 3=Strategique (rotation illustrative)
        N'Evaluation initiale provisoire - a valider par le comite competences',
        N'Global',
        CAST(GETDATE() AS DATE)
    FROM stg.Skills sk;
    PRINT N'Seed : stg.SkillCriticalities (' + CAST(@@ROWCOUNT AS VARCHAR) + N' lignes, 1 par competence existante)';
END
ELSE PRINT N'IGNORE (non vide, ou stg.Skills vide) : stg.SkillCriticalities';
GO

-- ============================================================================
-- 4. Propage le seed vers dim/fact
-- ============================================================================
EXEC fact.usp_ChargerEntrepotComplet;
GO

-- ============================================================================
-- 5. VALIDATION (PK / FK / index / row counts / relations)
-- ============================================================================

PRINT N'--- Nombre de lignes par table dim/fact ---';
SELECT s.name AS schema_bd, t.name AS table_bd, SUM(p.rows) AS nombre_lignes
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE s.name IN ('dim', 'fact')
GROUP BY s.name, t.name
ORDER BY s.name, t.name;

PRINT N'--- Contraintes PK/FK dim/fact ---';
SELECT
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS schema_table_enfant,
    OBJECT_NAME(fk.parent_object_id)        AS table_enfant,
    fk.name                                  AS nom_contrainte,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS schema_table_parent,
    OBJECT_NAME(fk.referenced_object_id)        AS table_parent
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) IN ('dim', 'fact')
ORDER BY schema_table_enfant, table_enfant;

PRINT N'--- Index par table dim/fact ---';
SELECT s.name AS schema_bd, t.name AS table_bd, i.name AS nom_index, i.type_desc AS type_index
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name IN ('dim', 'fact') AND i.name IS NOT NULL
ORDER BY s.name, t.name, i.name;

PRINT N'--- Orphelins potentiels (faits sans dimension correspondante -- 0 attendu) ---';
SELECT N'fact.EvaluationCompetences -> dim.Collaborateur' AS relation, COUNT(*) AS nb_orphelins
FROM fact.EvaluationCompetences f WHERE NOT EXISTS (SELECT 1 FROM dim.Collaborateur d WHERE d.CleCollaborateur = f.CleCollaborateur)
UNION ALL
SELECT N'fact.Formation -> dim.Collaborateur', COUNT(*)
FROM fact.Formation f WHERE NOT EXISTS (SELECT 1 FROM dim.Collaborateur d WHERE d.CleCollaborateur = f.CleCollaborateur)
UNION ALL
SELECT N'fact.EvaluationTalent -> dim.Collaborateur', COUNT(*)
FROM fact.EvaluationTalent f WHERE NOT EXISTS (SELECT 1 FROM dim.Collaborateur d WHERE d.CleCollaborateur = f.CleCollaborateur)
UNION ALL
SELECT N'fact.Promotion -> dim.Collaborateur', COUNT(*)
FROM fact.Promotion f WHERE NOT EXISTS (SELECT 1 FROM dim.Collaborateur d WHERE d.CleCollaborateur = f.CleCollaborateur);

PRINT N'--- dim.Collaborateur : integrite SCD2 (0 ligne attendue = pas de doublon de version courante) ---';
SELECT CollaborateurId, COUNT(*) AS nb_versions_courantes
FROM dim.Collaborateur
WHERE EstVersionCourante = 1
GROUP BY CollaborateurId
HAVING COUNT(*) > 1;
GO
