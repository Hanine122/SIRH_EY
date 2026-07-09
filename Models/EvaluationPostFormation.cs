using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class EvaluationPostFormation
{
    public int Id { get; set; }

    [Required]
    public int InscriptionId { get; set; }
    public Inscription? Inscription { get; set; }

    [Range(1, 5)]
    public int NoteGlobale { get; set; }

    [Range(1, 5)]
    public int? NoteContenu { get; set; }

    [Range(1, 5)]
    public int? NoteFormateur { get; set; }

    public bool Recommande { get; set; }

    [MaxLength(1000)]
    public string? Commentaire { get; set; }

    public DateTime DateEvaluation { get; set; } = DateTime.Now;
}
