namespace SIRH.EY.Models;

public class FormationCompetence
{
    public int FormationId { get; set; }
    public int CompetenceId { get; set; }
    public virtual Formation Formation { get; set; }
    public virtual Competence Competence { get; set; }
}