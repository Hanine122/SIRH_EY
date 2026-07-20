-- ============================================================================
-- SIRH.EY - Architecture BI - 004_AnalyticsViews.sql
-- analytics.vw_* construites sur dim/fact UNIQUEMENT. Noms de vues, alias et
-- colonnes en francais (import direct dans Power BI).
--
-- AUDIT : analytics.vw_Skills / vw_Training / vw_Talent / vw_Promotion /
-- vw_Succession / vw_Executive existent DEJA (construites sur stg, pas sur
-- dim/fact) -- non touchees, conformement a la regle "jamais recreer
-- l'existant". Les 5 vues ci-dessous portent des noms francais distincts
-- (pluriel ou synonyme), donc aucune collision : vw_Competences, vw_Formations,
-- vw_EvaluationsTalent, vw_Promotions, vw_Successions.
-- ============================================================================

USE SIRH_EY;
GO

DROP VIEW IF EXISTS analytics.vw_DW_Skills, analytics.vw_DW_Training, analytics.vw_DW_Talent,
    analytics.vw_DW_Promotion, analytics.vw_DW_Succession;   -- ancienne version anglaise, avant francisation
GO

CREATE OR ALTER VIEW analytics.vw_Competences AS
SELECT
    f.CompetenceEvalueeId                                                          AS IdEvaluationCompetence,
    c.CollaborateurId,
    c.Nom                                                                          AS NomCollaborateur,
    c.Prenom                                                                       AS PrenomCollaborateur,
    o.NomDepartement,
    g.Nom                                                                          AS NomGrade,
    cp.Nom                                                                         AS NomCompetence,
    cp.NomCategorie,
    cp.LibelleCriticite,
    cp.PoidsCriticite,
    f.NiveauActuel,
    f.NiveauCible,
    f.Ecart,
    f.AtteintCible,
    f.SeveriteEcart,
    cal.[Date]                                                                     AS DateEvaluation
FROM fact.EvaluationCompetences f
JOIN dim.Collaborateur c        ON c.CleCollaborateur = f.CleCollaborateur
LEFT JOIN dim.Organisation o     ON o.PositionId = c.PositionId
LEFT JOIN dim.Grade g            ON g.GradeId = c.GradeId
LEFT JOIN dim.Competence cp       ON cp.CleCompetence = f.CleCompetence
JOIN dim.Calendrier cal ON cal.CleDate = f.CleDate;
GO

CREATE OR ALTER VIEW analytics.vw_Formations AS
SELECT
    f.InscriptionId,
    c.CollaborateurId,
    c.Nom                    AS NomCollaborateur,
    c.Prenom                 AS PrenomCollaborateur,
    fo.Titre                 AS TitreFormation,
    fo.Categorie             AS CategorieFormation,
    fo.Plateforme,
    fo.EstCertifiante,
    f.Terminee,
    f.Progression,
    cali.[Date]              AS DateInscription,
    calc.[Date]              AS DateCompletion,
    f.NoteGlobaleChaud,
    f.NoteContenuChaud,
    f.NoteFormateurChaud,
    f.RecommandeChaud,
    f.NoteApplicationFroid,
    f.NoteImpactBusinessFroid,
    f.TauxUtilisationCapacite
FROM fact.Formation f
JOIN dim.Collaborateur c ON c.CleCollaborateur = f.CleCollaborateur
JOIN dim.Formation fo    ON fo.CleFormation = f.CleFormation
JOIN dim.Calendrier cali ON cali.CleDate = f.CleDateInscription
LEFT JOIN dim.Calendrier calc ON calc.CleDate = f.CleDateCompletion;
GO

CREATE OR ALTER VIEW analytics.vw_EvaluationsTalent AS
SELECT
    f.EvaluationTalentId,
    c.CollaborateurId,
    c.Nom                AS NomCollaborateur,
    c.Prenom             AS PrenomCollaborateur,
    o.NomDepartement,
    g.Nom                AS NomGrade,
    ce.Nom               AS NomCycleEvaluation,
    f.ScorePerformance,
    f.ScorePotentiel,
    f.Code9Boites,
    f.CodeStatutEvaluation,
    f.Actif,
    f.TotalOKR,
    f.OKRTermines,
    CASE WHEN f.TotalOKR > 0 THEN CAST(f.OKRTermines AS FLOAT) / f.TotalOKR END AS TauxReussiteOKR,
    cal.[Date]           AS DateEvaluation
FROM fact.EvaluationTalent f
JOIN dim.Collaborateur c     ON c.CleCollaborateur = f.CleCollaborateur
LEFT JOIN dim.Organisation o ON o.PositionId = c.PositionId
LEFT JOIN dim.Grade g        ON g.GradeId = c.GradeId
LEFT JOIN dim.CycleEvaluation ce ON ce.CleCycleEvaluation = f.CleCycleEvaluation
JOIN dim.Calendrier cal ON cal.CleDate = f.CleDate;
GO

CREATE OR ALTER VIEW analytics.vw_Promotions AS
SELECT
    c.CollaborateurId,
    c.Nom               AS NomCollaborateur,
    c.Prenom             AS PrenomCollaborateur,
    o.NomDepartement,
    g.Nom                AS NomGrade,
    g.GradeSuivant,
    g.AncienneteMinAns,
    g.NiveauMinCompetences,
    f.AncienneteAnnees,
    f.MoyenneNiveauActuel,
    f.MoyenneNiveauCible,
    f.TauxAtteinteCompetences,
    f.DernierScorePerformance,
    f.DernierScorePotentiel,
    c.PotentielCarriere,
    f.BandeEligibilite,
    cal.[Date]           AS DateSnapshot
FROM fact.Promotion f
JOIN dim.Collaborateur c     ON c.CleCollaborateur = f.CleCollaborateur
LEFT JOIN dim.Organisation o ON o.PositionId = c.PositionId
LEFT JOIN dim.Grade g        ON g.CleGrade = f.CleGrade
JOIN dim.Calendrier cal ON cal.CleDate = f.CleDateSnapshot;
GO

CREATE OR ALTER VIEW analytics.vw_Successions AS
SELECT
    f.SuccessionPlanId,
    tit.CollaborateurId AS IdCollaborateurTitulaire,
    tit.Nom             AS NomTitulaire,
    tit.Prenom          AS PrenomTitulaire,
    f.CodeStatutPlan,
    dcre.[Date]         AS DateCreation,
    f.IdSnapshot,
    cand.CollaborateurId AS IdCollaborateurCandidat,
    cand.Nom            AS NomCandidat,
    cand.Prenom         AS PrenomCandidat,
    f.Rang,
    f.ScoreSuccession,
    f.ScoreCouverture,
    f.CodeHorizonPreparation,
    dsnap.[Date]        AS DateSnapshot
FROM fact.Succession f
LEFT JOIN dim.Collaborateur tit  ON tit.CleCollaborateur = f.CleCollaborateurTitulaire
LEFT JOIN dim.Collaborateur cand ON cand.CleCollaborateur = f.CleCollaborateurCandidat
LEFT JOIN dim.Calendrier dcre  ON dcre.CleDate = f.CleDateCreation
LEFT JOIN dim.Calendrier dsnap ON dsnap.CleDate = f.CleDateSnapshot;
GO

PRINT N'analytics : 5 vues francaises creees sur dim/fact (vw_Competences, vw_Formations, vw_EvaluationsTalent, vw_Promotions, vw_Successions).';
GO
