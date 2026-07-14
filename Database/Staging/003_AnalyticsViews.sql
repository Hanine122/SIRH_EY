/* ============================================================================
   SIRH.EY — Analytics Views
   Schema: analytics
   One view per subject: Skills, Training, Talent, Promotion, Succession,
   Executive. All read from stg.* only (never dbo.*), per the staging design.

   Enum decodes below are taken directly from the C# enum definitions
   (Models\TalentEvaluation.cs, OKR.cs, SuccessionPlan.cs,
   SuccessorRankingSnapshot.cs, SkillCriticality.cs) rather than guessed.

   vw_Executive is a single-row global snapshot built by reading the other
   five views rather than re-deriving their logic, so there is exactly one
   place each metric is computed.

   vw_Promotion's ReadinessBand is a transparent, DB-only proxy (ancienneté +
   competence attainment vs. GradeReferentiel thresholds). It intentionally
   does NOT reimplement PromotionReadinessEngine / SuccessionEngine /
   ChatbotController's weighted scoring formulas — those are the application's
   canonical (if currently divergent) logic, live in C#, and are not
   duplicated here. Treat ReadinessBand as a coarse screening signal, not a
   replacement for the app's promotion decision.
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'analytics')
    EXEC('CREATE SCHEMA analytics');
GO

-- ============================================================================
-- 1. Skills — grain: one row per Competence assessment (Collaborateur x Skill)
-- ============================================================================

CREATE OR ALTER VIEW analytics.vw_Skills AS
SELECT
    c.Id                                AS CompetenceId,
    c.CollaborateurId,
    col.Nom                             AS CollaborateurNom,
    col.Prenom                          AS CollaborateurPrenom,
    col.DepartmentId,
    dep.Name                            AS DepartmentName,
    col.GradeId,
    c.SkillId,
    sk.Nom                              AS SkillNom,
    COALESCE(sk.Nom, c.Nom)             AS CompetenceLabel,
    c.CategorieCompetenceId,
    cat.Nom                             AS CategorieNom,
    c.NiveauActuel,
    c.NiveauCible,
    (c.NiveauCible - c.NiveauActuel)    AS Gap,
    CASE WHEN c.NiveauActuel >= c.NiveauCible THEN 1 ELSE 0 END AS IsAtTarget,
    CASE
        WHEN (c.NiveauCible - c.NiveauActuel) >= 2 THEN 'Critical'
        WHEN (c.NiveauCible - c.NiveauActuel) = 1 THEN 'Warning'
        ELSE 'OK'
    END                                  AS GapSeverity,
    crit.Niveau                         AS SkillCriticalityCode,
    CASE crit.Niveau
        WHEN 3 THEN 'Strategic' WHEN 2 THEN 'High' WHEN 1 THEN 'Medium' WHEN 0 THEN 'Low'
    END                                  AS SkillCriticalityLabel,
    -- Numeric weight for gap-weighting measures, computed here so the DAX
    -- layer never needs a calculated column to derive it. Unclassified
    -- skills (SkillCriticalities not yet populated) default to weight 2
    -- (Medium) rather than 0, so an unweighted gap isn't silently dropped
    -- from a weighted average.
    CASE crit.Niveau
        WHEN 3 THEN 4 WHEN 2 THEN 3 WHEN 1 THEN 2 WHEN 0 THEN 1 ELSE 2
    END                                  AS SkillCriticalityWeight,
    c.DateEvaluation
FROM stg.Competences c
JOIN stg.Collaborateurs col            ON col.Id = c.CollaborateurId
LEFT JOIN stg.Departments dep          ON dep.Id = col.DepartmentId
LEFT JOIN stg.Skills sk                ON sk.Id = c.SkillId
LEFT JOIN stg.CategoriesCompetences cat ON cat.Id = c.CategorieCompetenceId
OUTER APPLY (
    SELECT TOP 1 sc.Niveau
    FROM stg.SkillCriticalities sc
    WHERE sc.SkillId = c.SkillId
    ORDER BY sc.DateEvaluation DESC
) crit;
GO

-- ============================================================================
-- 2. Training — grain: one row per Inscription (enrollment), enriched with
--    hot/cold post-training evaluation and formation capacity.
-- ============================================================================

CREATE OR ALTER VIEW analytics.vw_Training AS
SELECT
    i.Id                                AS InscriptionId,
    i.CollaborateurId,
    col.Nom                             AS CollaborateurNom,
    col.Prenom                          AS CollaborateurPrenom,
    col.DepartmentId,
    i.FormationId,
    f.Titre                             AS FormationTitre,
    f.Categorie                         AS FormationCategorie,
    f.Plateforme,
    f.EstCertifiante,
    f.CapaciteMax,
    f.PlacesPrises,
    CASE WHEN f.CapaciteMax > 0
         THEN CAST(f.PlacesPrises AS FLOAT) / f.CapaciteMax
    END                                  AS CapacityUtilizationRatio,
    i.DateInscription,
    i.Terminee,
    i.Progression,
    i.DateCompletion,
    i.DateExamen,
    i.DateExpiration,
    pf.NoteGlobale                      AS HotNoteGlobale,
    pf.NoteContenu                      AS HotNoteContenu,
    pf.NoteFormateur                    AS HotNoteFormateur,
    pf.Recommande                       AS HotRecommande,
    sf.NoteApplicationCompetences       AS ColdNoteApplication,
    sf.NoteImpactBusiness               AS ColdNoteImpactBusiness
FROM stg.Inscriptions i
JOIN stg.Collaborateurs col             ON col.Id = i.CollaborateurId
JOIN stg.Formations f                   ON f.Id = i.FormationId
LEFT JOIN stg.EvaluationsPostFormation pf  ON pf.InscriptionId = i.Id
LEFT JOIN stg.EvaluationsSuiviFormation sf ON sf.InscriptionId = i.Id;
GO

-- ============================================================================
-- 3. Talent — grain: one row per TalentEvaluation (Collaborateur x review),
--    enriched with a per-collaborator OKR rollup.
-- ============================================================================

CREATE OR ALTER VIEW analytics.vw_Talent AS
SELECT
    te.Id                                AS TalentEvaluationId,
    te.CollaborateurId,
    col.Nom                              AS CollaborateurNom,
    col.Prenom                           AS CollaborateurPrenom,
    col.DepartmentId,
    col.GradeId,
    te.ReviewCycleId,
    rc.Nom                               AS ReviewCycleNom,
    te.PerformanceScore,
    te.PotentielScore,
    te.Category                          AS NineBoxCategoryCode,
    CASE te.Category
        WHEN 1 THEN 'Star'
        WHEN 2 THEN 'Future Leader'
        WHEN 3 THEN 'High Professional'
        WHEN 4 THEN 'Emerging Talent'
        WHEN 5 THEN 'Solid Professional'
        WHEN 6 THEN 'In Place'
        WHEN 7 THEN 'Rising Star'
        WHEN 8 THEN 'Need Development'
        WHEN 9 THEN 'Underperformer'
    END                                   AS NineBoxCategoryLabel,
    te.Statut                            AS EvaluationStatusCode,
    CASE te.Statut
        WHEN 0 THEN 'Draft' WHEN 1 THEN 'Submitted' WHEN 2 THEN 'Calibrated'
        WHEN 3 THEN 'Approved' WHEN 4 THEN 'Locked'
    END                                   AS EvaluationStatusLabel,
    te.DateEvaluation,
    te.Actif,
    okr.TotalOKRs,
    okr.CompletedOKRs,
    CASE WHEN okr.TotalOKRs > 0
         THEN CAST(okr.CompletedOKRs AS FLOAT) / okr.TotalOKRs
    END                                   AS OkrSuccessRateRatio
FROM stg.TalentEvaluations te
JOIN stg.Collaborateurs col              ON col.Id = te.CollaborateurId
LEFT JOIN stg.ReviewCycles rc            ON rc.Id = te.ReviewCycleId
OUTER APPLY (
    SELECT
        COUNT(*)                                        AS TotalOKRs,
        SUM(CASE WHEN o.Statut = 4 THEN 1 ELSE 0 END)   AS CompletedOKRs   -- 4 = Completed
    FROM stg.OKRs o
    WHERE o.CollaborateurId = te.CollaborateurId
) okr;
GO

-- ============================================================================
-- 4. Promotion — grain: one row per active Collaborateur.
--    ReadinessBand is a transparent DB-only proxy — see header note.
-- ============================================================================

CREATE OR ALTER VIEW analytics.vw_Promotion AS
SELECT
    col.Id                               AS CollaborateurId,
    col.Nom,
    col.Prenom,
    col.DepartmentId,
    col.GradeId,
    g.Name                               AS GradeName,
    gr.GradeSuivant,
    gr.AncienneteMinAns,
    gr.NiveauMinCompetences,
    CAST(DATEDIFF(DAY, col.DateEmbauche, GETDATE()) / 365.25 AS FLOAT) AS AncienneteAnnees,
    comp.AvgNiveauActuel,
    comp.AvgNiveauCible,
    CASE WHEN comp.AvgNiveauCible > 0
         THEN comp.AvgNiveauActuel / comp.AvgNiveauCible
    END                                   AS CompetenceAttainmentRatio,
    te.PerformanceScore                  AS LatestPerformanceScore,
    te.PotentielScore                    AS LatestPotentielScore,
    col.PotentielCarriere,
    CASE
        WHEN comp.AvgNiveauActuel IS NULL THEN 'Insufficient data'
        WHEN (DATEDIFF(DAY, col.DateEmbauche, GETDATE()) / 365.25) >= ISNULL(gr.AncienneteMinAns, 0)
             AND comp.AvgNiveauActuel >= ISNULL(gr.NiveauMinCompetences, 0)
            THEN 'Ready'
        WHEN (DATEDIFF(DAY, col.DateEmbauche, GETDATE()) / 365.25) >= ISNULL(gr.AncienneteMinAns, 0) * 0.75
             OR comp.AvgNiveauActuel >= ISNULL(gr.NiveauMinCompetences, 0) * 0.85
            THEN 'Developing'
        ELSE 'Not ready'
    END                                   AS ReadinessBand
FROM stg.Collaborateurs col
LEFT JOIN stg.Grades g                   ON g.Id = col.GradeId
LEFT JOIN stg.GradeReferentiels gr       ON gr.Grade = col.Grade
OUTER APPLY (
    SELECT
        AVG(CAST(c.NiveauActuel AS FLOAT)) AS AvgNiveauActuel,
        AVG(CAST(c.NiveauCible AS FLOAT))  AS AvgNiveauCible
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
GO

-- ============================================================================
-- 5. Succession — grain: one row per SuccessionPlan x ranked candidate
--    (LEFT JOIN, so a plan with no ranked successor yet still appears —
--    surfaces "single point of failure" positions once plans are populated).
-- ============================================================================

CREATE OR ALTER VIEW analytics.vw_Succession AS
SELECT
    sp.Id                                AS SuccessionPlanId,
    sp.Poste,
    sp.Departement,
    sp.CollaborateurTitulaireId,
    tit.Nom                              AS TitulaireNom,
    tit.Prenom                           AS TitulairePrenom,
    sp.Statut                            AS PlanStatusCode,
    CASE sp.Statut
        WHEN 0 THEN 'Draft' WHEN 1 THEN 'Manager Validated' WHEN 2 THEN 'HR Approved'
        WHEN 3 THEN 'Rejected' WHEN 4 THEN 'Archived'
    END                                   AS PlanStatusLabel,
    sp.DateCreation,
    srs.Id                                AS SnapshotId,
    srs.CandidatId,
    cand.Nom                             AS CandidatNom,
    cand.Prenom                          AS CandidatPrenom,
    srs.Rang,
    srs.ScoreSuccession,
    srs.ScoreCouverture,
    srs.Horizon                          AS ReadinessHorizonCode,
    CASE srs.Horizon
        WHEN 0 THEN 'Ready now' WHEN 1 THEN 'Ready 6-12 months'
        WHEN 2 THEN 'Ready 12-24 months' WHEN 3 THEN 'Not ready'
    END                                   AS ReadinessHorizonLabel,
    srs.DateSnapshot
FROM stg.SuccessionPlans sp
LEFT JOIN stg.Collaborateurs tit               ON tit.Id = sp.CollaborateurTitulaireId
LEFT JOIN stg.SuccessorRankingSnapshots srs    ON srs.SuccessionPlanId = sp.Id
LEFT JOIN stg.Collaborateurs cand              ON cand.Id = srs.CandidatId;
GO

-- ============================================================================
-- 6. Executive — grain: single row, global snapshot. Reads the five views
--    above rather than re-deriving their logic, so each metric has exactly
--    one place it's computed.
-- ============================================================================

CREATE OR ALTER VIEW analytics.vw_Executive AS
SELECT
    (SELECT COUNT(*) FROM stg.Collaborateurs WHERE Actif = 1)                          AS ActiveHeadcount,

    (SELECT COUNT(*) FROM analytics.vw_Skills)                                         AS TotalCompetenceAssessments,
    (SELECT COUNT(*) FROM analytics.vw_Skills WHERE IsAtTarget = 1)                     AS CompetenceAtTargetCount,
    CAST((SELECT COUNT(*) FROM analytics.vw_Skills WHERE IsAtTarget = 1) AS FLOAT)
        / NULLIF((SELECT COUNT(*) FROM analytics.vw_Skills), 0)                         AS SkillCoverageRatio,
    (SELECT COUNT(*) FROM analytics.vw_Skills WHERE GapSeverity = 'Critical')           AS CriticalSkillGapCount,

    (SELECT COUNT(*) FROM analytics.vw_Training)                                        AS TotalEnrollments,
    (SELECT COUNT(*) FROM analytics.vw_Training WHERE Terminee = 1)                     AS CompletedEnrollments,
    CAST((SELECT COUNT(*) FROM analytics.vw_Training WHERE Terminee = 1) AS FLOAT)
        / NULLIF((SELECT COUNT(*) FROM analytics.vw_Training), 0)                       AS TrainingCompletionRatio,

    (SELECT COUNT(DISTINCT CollaborateurId) FROM analytics.vw_Talent)                   AS CollaborateursReviewedCount,
    CAST((SELECT COUNT(DISTINCT CollaborateurId) FROM analytics.vw_Talent) AS FLOAT)
        / NULLIF((SELECT COUNT(*) FROM stg.Collaborateurs WHERE Actif = 1), 0)          AS TalentReviewCoverageRatio,

    (SELECT COUNT(*) FROM analytics.vw_Promotion WHERE ReadinessBand = 'Ready')         AS PromotionReadyCount,

    (SELECT COUNT(DISTINCT SuccessionPlanId) FROM analytics.vw_Succession)              AS SuccessionPlanCount,
    (SELECT COUNT(DISTINCT SuccessionPlanId) FROM analytics.vw_Succession
        WHERE SnapshotId IS NULL)                                                       AS PositionsWithoutSuccessorCount;
GO
