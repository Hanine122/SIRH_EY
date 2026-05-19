using System;
using System.Collections.Generic;
using SIRH.EY.Models;

namespace SIRH.EY.Models.InsightsAI
{
    public class RhInsightsViewModel
    {
        // Keep the original alerts for UI compatibility
        public List<Collaborateur> AlertesContinuite { get; set; } = new();
        
        public List<ExecutiveKpiCardViewModel> KpiCards { get; set; } = new();
        public List<SmartAlertViewModel> SmartAlerts { get; set; } = new();
        public List<HiddenTalentViewModel> HiddenTalents { get; set; } = new();
        public List<SkillHeatmapViewModel> SkillHeatmaps { get; set; } = new();
        public List<FormationInsightViewModel> FormationInsights { get; set; } = new();
        public PromotionReadinessSimulatorViewModel PromotionSimulator { get; set; } = new();
        public WorkforceImpactSimulatorViewModel WorkforceImpactSimulator { get; set; } = new();
    }

    public class ExecutiveKpiCardViewModel
    {
        public string Title { get; set; } = "";
        public string Value { get; set; } = "";
        public string Trend { get; set; } = "";
        public string IconClass { get; set; } = "";
        public string ColorClass { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Tone { get; set; } = "neutral";
        public string Insight { get; set; } = "";
        public double NumericValue { get; set; }
        public string ValueSuffix { get; set; } = "";
    }

    public class SmartAlertViewModel
    {
        public int CollaborateurId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string AlertType { get; set; } = ""; // e.g., "Succession", "SkillGap", "FlightRisk"
        public string Severity { get; set; } = ""; // "High", "Medium", "Low"
        public string Recommendation { get; set; } = "";
        public string AiBadge { get; set; } = "AI recommendation";
        public int ConfidenceScore { get; set; } = 82;
        public DateTime DateGenerated { get; set; } = DateTime.UtcNow;
    }

    public class HiddenTalentViewModel
    {
        public int CollaborateurId { get; set; }
        public string NomComplet { get; set; } = "";
        public string Departement { get; set; } = "";
        public string PosteActuel { get; set; } = "";
        public double ReadinessScore { get; set; }
        public string EvolutionPotentielle { get; set; } = "";
        public List<string> CompetencesCles { get; set; } = new();
        public string TalentType { get; set; } = "";
        public string Signal { get; set; } = "";
        public int FormationCount { get; set; }
    }

    public class SkillHeatmapViewModel
    {
        public string Competence { get; set; } = "";
        public int NbCollaborateursMaitrisant { get; set; }
        public int NbCollaborateursRequis { get; set; }
        public double Couverture { get; set; }
        public string Status { get; set; } = ""; // "Critical", "Warning", "Healthy"
        public string Category { get; set; } = "";
        public string Insight { get; set; } = "";
    }

    public class FormationInsightViewModel
    {
        public string FormationTitre { get; set; } = "";
        public int UrgencyScore { get; set; } // 0-100
        public string Cible { get; set; } = "";
        public int NbCollaborateursImpactes { get; set; }
        public string ExpectedImpact { get; set; } = "";
        public int ReadinessGain { get; set; }
        public List<string> TargetedCompetencies { get; set; } = new();
    }

    public class AiComparisonResponse
    {
        public double CompatibilityScore { get; set; }
        public List<string> SharedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public List<string> TransversalSkills { get; set; } = new();
        public double ReadinessScore { get; set; }
        public string AiSummary { get; set; } = "";
        public List<string> RecommendedFormations { get; set; } = new();
    }

    public class PromotionReadinessSimulatorViewModel
    {
        public List<PromotionCollaborateurOptionViewModel> Collaborateurs { get; set; } = new();
        public List<PromotionTargetOptionViewModel> TargetPositions { get; set; } = new();
        public PromotionReadinessResultViewModel? DefaultResult { get; set; }
    }

    public class PromotionCollaborateurOptionViewModel
    {
        public int Id { get; set; }
        public string NomComplet { get; set; } = "";
        public string Poste { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Departement { get; set; } = "";
    }

    public class PromotionTargetOptionViewModel
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Poste { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Departement { get; set; } = "";
    }

    public class PromotionReadinessRequest
    {
        public int CollaborateurId { get; set; }
        public string TargetKey { get; set; } = "";
    }

    public class PromotionReadinessResultViewModel
    {
        public int CollaborateurId { get; set; }
        public string CollaborateurNom { get; set; } = "";
        public string CurrentRole { get; set; } = "";
        public string TargetRole { get; set; } = "";
        public double ReadinessPercentage { get; set; }
        public double CompatibilityScore { get; set; }
        public double PromotionPotential { get; set; }
        public int EstimatedMonthsMin { get; set; }
        public int EstimatedMonthsMax { get; set; }
        public List<string> TransversalSkills { get; set; } = new();
        public List<string> LeadershipIndicators { get; set; } = new();
        public List<PromotionCompetencyGapViewModel> MissingCompetencies { get; set; } = new();
        public List<PromotionFormationRecommendationViewModel> RecommendedFormations { get; set; } = new();
        public string ExecutiveSummary { get; set; } = "";
    }

    public class PromotionCompetencyGapViewModel
    {
        public string Competence { get; set; } = "";
        public int CurrentLevel { get; set; }
        public int RequiredLevel { get; set; }
        public int Gap { get; set; }
        public string Severity { get; set; } = "";
        public string PriorityLabel { get; set; } = "";
    }

    public class PromotionFormationRecommendationViewModel
    {
        public string FormationTitre { get; set; } = "";
        public string TargetCompetence { get; set; } = "";
        public int ReadinessGain { get; set; }
        public string ProgressionImpact { get; set; } = "";
        public int EstimatedWeeks { get; set; }
    }

    public class WorkforceImpactSimulatorViewModel
    {
        public List<PromotionCollaborateurOptionViewModel> Collaborateurs { get; set; } = new();
        public WorkforceImpactResultViewModel? DefaultResult { get; set; }
    }

    public class WorkforceImpactRequest
    {
        public int CollaborateurId { get; set; }
    }

    public class WorkforceImpactResultViewModel
    {
        public int CollaborateurId { get; set; }
        public string CollaborateurNom { get; set; } = "";
        public string Role { get; set; } = "";
        public string Departement { get; set; } = "";
        public double ContinuityRisk { get; set; }
        public double OperationalImpact { get; set; }
        public double DepartmentFragility { get; set; }
        public double StrategicDependencyScore { get; set; }
        public string RiskLevel { get; set; } = "";
        public string ExecutiveInsight { get; set; } = "";
        public List<string> CompetenciesLost { get; set; } = new();
        public List<WorkforceDepartmentExposureViewModel> DepartmentExposure { get; set; } = new();
        public List<WorkforceSuccessorViewModel> ImmediateSuccessors { get; set; } = new();
        public List<WorkforceSuccessorViewModel> PartialSuccessors { get; set; } = new();
        public List<WorkforceSuccessorViewModel> HighPotentialAlternatives { get; set; } = new();
        public List<WorkforceActionRecommendationViewModel> RecommendedActions { get; set; } = new();
    }

    public class WorkforceDepartmentExposureViewModel
    {
        public string Department { get; set; } = "";
        public int ImpactedCollaborators { get; set; }
        public double ExposureScore { get; set; }
        public string Signal { get; set; } = "";
    }

    public class WorkforceSuccessorViewModel
    {
        public int CollaborateurId { get; set; }
        public string NomComplet { get; set; } = "";
        public string Poste { get; set; } = "";
        public string Departement { get; set; } = "";
        public double ReadinessScore { get; set; }
        public int SharedCompetencies { get; set; }
        public string SuccessorType { get; set; } = "";
    }

    public class WorkforceActionRecommendationViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Priority { get; set; } = "";
    }
}
