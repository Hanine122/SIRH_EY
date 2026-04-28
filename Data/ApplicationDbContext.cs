using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
public DbSet<CompetenceRequiseParPoste> CompetencesRequisesParPoste { get; set; }
    public DbSet<Collaborateur> Collaborateurs { get; set; }
    public DbSet<Competence> Competences { get; set; }
    public DbSet<EvaluationCompetence> EvaluationsCompetences { get; set; }
    public DbSet<Formation> Formations { get; set; }
    public DbSet<Inscription> Inscriptions { get; set; }
    public DbSet<PlanDeveloppement> PlansDeveloppement { get; set; }
    public DbSet<EvaluationHistorique> EvaluationHistorique { get; set; }
    public DbSet<Parametre> Parametres { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration supplémentaire (optionnelle)
        // Par exemple, pour éviter la suppression en cascade sur Inscription
        modelBuilder.Entity<Inscription>()
            .HasOne(i => i.Collaborateur)
            .WithMany(c => c.Inscriptions)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inscription>()
            .HasOne(i => i.Formation)
            .WithMany(f => f.Inscriptions)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.Manager)
            .WithMany(m => m.Equipe)
            .HasForeignKey(c => c.ManagerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EvaluationCompetence>()
            .HasOne(e => e.Competence)
            .WithOne(c => c.EvaluationCompetence)
            .HasForeignKey<EvaluationCompetence>(e => e.CompetenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}