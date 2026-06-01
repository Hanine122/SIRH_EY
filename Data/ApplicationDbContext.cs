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

    // ── Core HR ──────────────────────────────────────────────────────────────
    public DbSet<Collaborateur> Collaborateurs { get; set; }
    public DbSet<Competence> Competences { get; set; }
    public DbSet<CategorieCompetence> CategoriesCompetences { get; set; }
    public DbSet<Formation> Formations { get; set; }
    public DbSet<Inscription> Inscriptions { get; set; }
    public DbSet<FormationCompetence> FormationCompetences { get; set; }
    public DbSet<CompetenceRequiseParPoste> CompetencesRequisesParPoste { get; set; }
    public DbSet<EvaluationCompetence> EvaluationsCompetences { get; set; }
    public DbSet<EvaluationHistorique> EvaluationsHistoriques { get; set; }
    public DbSet<PlanDeveloppement> PlansDeveloppement { get; set; }
    public DbSet<Parametre> Parametres { get; set; }

    // ── Talent Management ─────────────────────────────────────────────────────
    public DbSet<TalentEvaluation> TalentEvaluations { get; set; }
    public DbSet<OKR> OKRs { get; set; }
    public DbSet<KeyResult> KeyResults { get; set; }

    // ── HR Master Data (referential) ──────────────────────────────────────────
    public DbSet<Department> Departments { get; set; }
    public DbSet<SubDepartment> SubDepartments { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<GradeEntity> Grades { get; set; }
    public DbSet<BusinessUnitEntity> BusinessUnits { get; set; }
    public DbSet<LocationEntity> Locations { get; set; }
    public DbSet<ContractType> ContractTypes { get; set; }
    public DbSet<SystemParameter> SystemParameters { get; set; }

    // ── Position relationships ─────────────────────────────────────────────────
    public DbSet<PositionRequiredCompetence> PositionRequiredCompetences { get; set; }
    public DbSet<PositionMandatoryFormation> PositionMandatoryFormations { get; set; }
    public DbSet<PositionGradeEligibility> PositionGradeEligibilities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Collaborateur → ApplicationUser ───────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Self-referential: Collaborateur → Manager ─────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.Manager)
            .WithMany(c => c.Equipe)
            .HasForeignKey(c => c.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Department → SubDepartments ───────────────────────────────────────
        modelBuilder.Entity<SubDepartment>()
            .HasOne(s => s.Department)
            .WithMany(d => d.SubDepartments)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── SubDepartment → Positions ─────────────────────────────────────────
        modelBuilder.Entity<Position>()
            .HasOne(p => p.SubDepartment)
            .WithMany(s => s.Positions)
            .HasForeignKey(p => p.SubDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Department → Collaborateurs ───────────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.DepartmentRef)
            .WithMany(d => d.Collaborateurs)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── SubDepartment → Collaborateurs ────────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.SubDepartmentRef)
            .WithMany(s => s.Collaborateurs)
            .HasForeignKey(c => c.SubDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Position → Collaborateurs ─────────────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.PositionRef)
            .WithMany(p => p.Collaborateurs)
            .HasForeignKey(c => c.PositionId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── GradeEntity → Collaborateurs ──────────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.GradeRef)
            .WithMany(g => g.Collaborateurs)
            .HasForeignKey(c => c.GradeId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── BusinessUnitEntity → Collaborateurs ───────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.BusinessUnitRef)
            .WithMany(b => b.Collaborateurs)
            .HasForeignKey(c => c.BusinessUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── LocationEntity → Collaborateurs ───────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.LocationRef)
            .WithMany(l => l.Collaborateurs)
            .HasForeignKey(c => c.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── ContractType → Collaborateurs ─────────────────────────────────────
        modelBuilder.Entity<Collaborateur>()
            .HasOne(c => c.ContractTypeRef)
            .WithMany(ct => ct.Collaborateurs)
            .HasForeignKey(c => c.ContractTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Position → RequiredCompetences ────────────────────────────────────
        modelBuilder.Entity<PositionRequiredCompetence>()
            .HasOne(prc => prc.Position)
            .WithMany(p => p.RequiredCompetences)
            .HasForeignKey(prc => prc.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Position → MandatoryFormations ────────────────────────────────────
        modelBuilder.Entity<PositionMandatoryFormation>()
            .HasOne(pmf => pmf.Position)
            .WithMany(p => p.MandatoryFormations)
            .HasForeignKey(pmf => pmf.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PositionMandatoryFormation>()
            .HasOne(pmf => pmf.Formation)
            .WithMany()
            .HasForeignKey(pmf => pmf.FormationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Position → GradeEligibilities ─────────────────────────────────────
        modelBuilder.Entity<PositionGradeEligibility>()
            .HasOne(pge => pge.Position)
            .WithMany(p => p.GradeEligibilities)
            .HasForeignKey(pge => pge.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PositionGradeEligibility>()
            .HasOne(pge => pge.GradeEntity)
            .WithMany(g => g.PositionEligibilities)
            .HasForeignKey(pge => pge.GradeEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Audit fields: default CreatedAt for existing rows ─────────────────
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(AuditableEntity.CreatedAt))
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }

        // ── FormationCompetence composite key ─────────────────────────────────
        modelBuilder.Entity<FormationCompetence>()
            .HasKey(fc => new { fc.FormationId, fc.CompetenceId });

        // ── EvaluationCompetence → Inscription ────────────────────────────────
        modelBuilder.Entity<EvaluationCompetence>()
            .HasOne(e => e.Inscription)
            .WithMany(i => i.EvaluationsFormation)
            .HasForeignKey(e => e.InscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── SystemParameter unique key ────────────────────────────────────────
        modelBuilder.Entity<SystemParameter>()
            .HasIndex(p => p.Key)
            .IsUnique();
    }
}
