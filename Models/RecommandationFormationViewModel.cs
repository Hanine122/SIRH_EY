namespace SIRH.EY.Models;

public class RecommandationFormationViewModel
{
    public Formation Formation { get; set; } = null!;

    /// <summary>Raison lisible : "Améliore la compétence Power BI", "Requis pour le grade Director"</summary>
    public string Raison { get; set; } = string.Empty;

    /// <summary>"competence" | "grade" | "plan" | "mandatory"</summary>
    public string Type { get; set; } = "competence";

    public int ScoreAdequation { get; set; }

    public string? CompetenceCiblee { get; set; }

    public bool EstInscrit { get; set; }

    public bool EstDansPlan { get; set; }

    public string TypeIconClass => Type switch
    {
        "grade"     => "fas fa-level-up-alt text-primary",
        "plan"      => "fas fa-clipboard-list text-info",
        "mandatory" => "fas fa-exclamation-circle text-danger",
        _           => "fas fa-bullseye text-success"
    };

    public string TypeLabel => Type switch
    {
        "grade"     => "Évolution de grade",
        "plan"      => "Plan de développement",
        "mandatory" => "Obligatoire",
        _           => "Compétence manquante"
    };

    public string TypeBadgeClass => Type switch
    {
        "grade"     => "badge-strategic",
        "plan"      => "badge-recommended",
        "mandatory" => "badge-mandatory",
        _           => "badge-recommended"
    };
}

public class RecommandationsPageViewModel
{
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public List<RecommandationFormationViewModel> ParCompetence { get; set; } = new();
    public List<RecommandationFormationViewModel> ParGrade      { get; set; } = new();
    public List<RecommandationFormationViewModel> ParPlan       { get; set; } = new();
    public List<RecommandationFormationViewModel> Obligatoires  { get; set; } = new();

    public string? ProchainGrade { get; set; }

    public int TotalRecommandations =>
        ParCompetence.Count + ParGrade.Count + ParPlan.Count + Obligatoires.Count;
}
