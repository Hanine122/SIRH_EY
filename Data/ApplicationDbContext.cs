using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    public DbSet<Collaborateur> Collaborateurs { get; set; }

public DbSet<FormationCompetence> FormationCompetences { get; set; }

    public DbSet<Competence> Competences { get; set; }
    public DbSet<CategorieCompetence> CategoriesCompetences { get; set; }
    public DbSet<Formation> Formations { get; set; }
    public DbSet<Inscription> Inscriptions { get; set; }
    public DbSet<CompetenceRequiseParPoste> CompetencesRequisesParPoste { get; set; }

    public DbSet<Parametre> Parametres { get; set; }
    public DbSet<EvaluationCompetence> EvaluationsCompetences { get; set; }
    public DbSet<PlanDeveloppement> PlansDeveloppement { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Relation Collaborateur -> User (existant)
    modelBuilder.Entity<Collaborateur>()
        .HasOne(c => c.User)
        .WithMany()
        .HasForeignKey(c => c.UserId)
        .OnDelete(DeleteBehavior.SetNull);
// Clé composite pour FormationCompetence
    modelBuilder.Entity<FormationCompetence>()
        .HasKey(fc => new { fc.FormationId, fc.CompetenceId });

    // Relations pour EvaluationFormation
   modelBuilder.Entity<EvaluationCompetence>()
    .HasOne(e => e.Inscription)
    .WithMany(i => i.EvaluationsFormation) 
    .HasForeignKey(e => e.InscriptionId)
    .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
    
}