using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class SystemParameter
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Clé")]
    public string Key { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Valeur")]
    public string Value { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Catégorie")]
    public string? Category { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Visible")]
    public bool IsVisible { get; set; } = true;

    [Display(Name = "Modifiable")]
    public bool IsEditable { get; set; } = true;

    [MaxLength(256)]
    [Display(Name = "Modifié par")]
    public string? ModifiedBy { get; set; }

    [Display(Name = "Date de modification")]
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
