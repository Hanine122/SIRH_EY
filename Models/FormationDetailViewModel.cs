namespace SIRH.EY.Models;

public class FormationDetailViewModel
{
    public Formation Formation { get; set; } = null!;
    public Inscription? InscriptionActuelle { get; set; }
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }
    public int ScoreAdequation { get; set; }
    public string? ProchainGrade { get; set; }
    public bool EstObligatoire { get; set; }
    public bool EstDansPlan { get; set; }

    public List<string> CompetencesDeveloppees { get; set; } = new();
    public List<string> CompetencesPrerequisListe { get; set; } = new();

    // Platform display helpers
    public string PlatformeCouleur => Formation.Plateforme switch
    {
        "Udemy"             => "#A435F0",
        "Microsoft Learn"   => "#0078D4",
        "Coursera"          => "#0056D3",
        "AWS Skill Builder" => "#FF9900",
        "LinkedIn Learning" => "#0A66C2",
        "Google Cloud"      => "#4285F4",
        _                   => "#2E2E38"
    };

    public string PlateformeIconClass => Formation.Plateforme switch
    {
        "Udemy"             => "fab fa-udemy",
        "Microsoft Learn"   => "fab fa-microsoft",
        "Coursera"          => "fas fa-graduation-cap",
        "AWS Skill Builder" => "fab fa-aws",
        "LinkedIn Learning" => "fab fa-linkedin",
        "Google Cloud"      => "fab fa-google",
        _                   => "fas fa-building"
    };

    public string BoutonOuvrirLabel => Formation.Plateforme switch
    {
        "Udemy"             => "Ouvrir sur Udemy",
        "Microsoft Learn"   => "Ouvrir sur Microsoft Learn",
        "Coursera"          => "Ouvrir sur Coursera",
        "AWS Skill Builder" => "Ouvrir sur AWS Skill Builder",
        "LinkedIn Learning" => "Ouvrir sur LinkedIn Learning",
        "Google Cloud"      => "Ouvrir sur Google Cloud",
        _                   => "Accéder à la formation"
    };
}
