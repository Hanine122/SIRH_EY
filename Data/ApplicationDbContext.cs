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

    // ── Talent Governance (cycle, audit, decision rules) ──────────────────────
    public DbSet<ReviewCycle> ReviewCycles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<DecisionRule> DecisionRules { get; set; }

    // ── Succession Planning (persistant) ──────────────────────────────────────
    public DbSet<SuccessionPlan> SuccessionPlans { get; set; }
    public DbSet<SuccessorRankingSnapshot> SuccessorRankingSnapshots { get; set; }

    // ── Skill Ontology (référentiel compétence enterprise) ────────────────────
    public DbSet<Skill> Skills { get; set; }
    public DbSet<SkillCategory> SkillCategories { get; set; }
    public DbSet<SkillLevel> SkillLevels { get; set; }
    public DbSet<SkillRelation> SkillRelations { get; set; }
    public DbSet<SkillAlias> SkillAliases { get; set; }
    public DbSet<SkillCriticality> SkillCriticalities { get; set; }
    public DbSet<SkillVersion> SkillVersions { get; set; }

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

    // ── Phase 2 — Certifications ──────────────────────────────────────────────
    public DbSet<Certification> Certifications { get; set; }
    public DbSet<CollaborateurCertification> CollaborateurCertifications { get; set; }

    // ── Phase 4 — Grade Référentiel ───────────────────────────────────────────
    public DbSet<GradeReferentiel> GradeReferentiels { get; set; }

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

        // ── Phase 2 : CollaborateurCertification → Collaborateur ──────────────
        modelBuilder.Entity<CollaborateurCertification>()
            .HasOne(cc => cc.Collaborateur)
            .WithMany(c => c.CollaborateurCertifications)
            .HasForeignKey(cc => cc.CollaborateurId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CollaborateurCertification>()
            .HasOne(cc => cc.Certification)
            .WithMany(cert => cert.CollaborateurCertifications)
            .HasForeignKey(cc => cc.CertificationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Phase 3 : ModeDeploiement stocké en string lisible ────────────────
        modelBuilder.Entity<Collaborateur>()
            .Property(c => c.ModeDeploiement)
            .HasConversion<string>()
            .HasMaxLength(20);

        // ── Skill Ontology : SkillCategory self-référence (même pattern que Collaborateur.Manager) ──
        modelBuilder.Entity<SkillCategory>()
            .HasOne(sc => sc.ParentCategory)
            .WithMany(sc => sc.SousCategories)
            .HasForeignKey(sc => sc.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Skill Ontology : SkillRelation a deux FK vers Skill — Restrict sur les deux
        //    pour éviter l'erreur SQL Server "multiple cascade paths".
        modelBuilder.Entity<SkillRelation>()
            .HasOne(sr => sr.SourceSkill)
            .WithMany(s => s.RelationsSource)
            .HasForeignKey(sr => sr.SourceSkillId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SkillRelation>()
            .HasOne(sr => sr.TargetSkill)
            .WithMany(s => s.RelationsCible)
            .HasForeignKey(sr => sr.TargetSkillId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Skill Ontology : suppression d'un Skill entraîne celle de ses attributs (owning) ──
        modelBuilder.Entity<SkillAlias>()
            .HasOne(a => a.Skill)
            .WithMany(s => s.Aliases)
            .HasForeignKey(a => a.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkillLevel>()
            .HasOne(l => l.Skill)
            .WithMany(s => s.Levels)
            .HasForeignKey(l => l.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkillCriticality>()
            .HasOne(c => c.Skill)
            .WithMany(s => s.Criticalities)
            .HasForeignKey(c => c.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkillVersion>()
            .HasOne(v => v.Skill)
            .WithMany(s => s.Versions)
            .HasForeignKey(v => v.SkillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
