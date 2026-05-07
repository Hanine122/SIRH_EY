using Microsoft.AspNetCore.Identity;

namespace SIRH.EY.Models;

public class ApplicationUser : IdentityUser
{
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
}