# PROJECT_KNOWLEDGE.md — SIRH.EY

Factual knowledge base extracted directly from the codebase at `c:\Users\Bureau\Documents\SIRH.EY` (audit date: 2026-07-06). No inferred intent, no proposed solutions — only what is present in code, cited by file/class where possible. Cross-referenced against `docs\ARCHITECTURE_RH.md` and `docs\DOCUMENTATION_TECHNIQUE_COMPLETE.md`, which are existing (non-authoritative) project documents; anything sourced from them rather than verified in code is marked **[docs claim]**.

---

## 1. Project Purpose

- ASP.NET Core MVC (.NET 8) application named `SIRH.EY` (`SIRH.EY.csproj`).
- **[docs claim]** `docs\DOCUMENTATION_TECHNIQUE_COMPLETE.md` and the seed data describe it as an HR management system for "EY Tunisie" (Ernst & Young Tunisia office) — centralizing employee (`Collaborateur`) records, competencies, training/formations, and career development, with additional analytics for talent management, succession planning, promotion readiness, and workforce-impact simulation.
- Code evidence supporting an EY-branded HR context: seed data in `Program.cs` (e.g. admin email `admin@ey.tn`, employee emails `@ey.com`), `Data\EnterpriseDemoSeeder.cs` (EY Tunisia office hierarchy, matricules `EY-PTN-###`, `EY-DIR-###`, etc.), CSS files `wwwroot\css\ey-*.css`, and EY-sector competency catalogs hardcoded in `Controllers\CompetencesController.cs`.

---

## 2. Business Modules

Derived from controllers, view folders, and services:

1. **Collaborateurs** (employee directory/CRUD, hierarchy, departure/replacement workflow) — `Controllers\CollaborateursController.cs`, `Views\Collaborateurs\`
2. **Compétences** (competency catalog, self-evaluation, manager validation, team matrix) — `Controllers\CompetencesController.cs`, `Views\Competences\`
3. **Formations** (training catalog, enrollment, exam scheduling, certificates, recommendations, career path) — `Controllers\FormationsController.cs`, `Views\Formations\`
4. **Inscriptions** (enrollment admin CRUD) — `Controllers\InscriptionsController.cs`, `Views\Inscriptions\`
5. **Certificats** (completed-training certificate list/PDF) — `Controllers\CertificatsController.cs`, `Views\Certificats\`
6. **Talent Management** (9-box grid, OKRs, evaluations) — `Controllers\TalentController.cs`, `Views\Talent\`
7. **Reporting** (executive dashboard, succession analytics, skill-gap charts) — `Controllers\ReportingController.cs`, `Views\Reporting\`
8. **RH Insights / AI Insights** (KPI cards, smart alerts, hidden talents, skill heatmaps, formation insights, promotion/workforce simulators) — `Controllers\RhInsightsController.cs`, `Views\RHInsights\`
9. **Chatbot / Copilot** (conversational HR assistant backed by n8n) — `Controllers\ChatbotController.cs`, `Controllers\CopilotController.cs`, `Views\Copilot\`, `Views\Shared\_ChatbotWidget.cshtml`
10. **Admin master-data management** (Departments, SubDepartments, Positions, Grades, Business Units, Locations, Contract Types, System Parameters) — `Areas\Admin\Controllers\*`
11. **Identity/Account** — `Areas\Identity\Pages\Account\*` (ASP.NET Core Identity scaffolded pages)
12. **Home / Portal** — `Controllers\HomeController.cs` (dashboard, modern portal, settings, privacy)

---

## 3. Features by Module

### Collaborateurs (`Controllers\CollaborateursController.cs`)
- List/search/sort/filter by department, with role-scoped visibility via `ITeamAccessService.ApplyAccessFilterAsync`.
- CRUD (Create/Edit/Delete) restricted to ITAdmin/RH.
- Manager assignment (bulk, `AssignerManager`).
- Successor selection (`ChoisirRemplacant`) using `SuccessionEngine`.
- Departure workflow (`Depart` → `ConfirmDepart`/`ConfirmerRemplacement`): finds replacement in same dept/poste, computes skill gaps, recommends formations, deactivates departing employee.
- Interview-request email to candidates (`EnvoyerDemandeEntretiens`) via `IEmailSender`.
- PDF comparison export of replacement candidates (`ExportComparaisonRemplacantsPdf`) via `Services\ComparaisonRemplacantsPdf.cs`.
- `GetPostesParDepartement`: hardcoded department→position mapping (not DB-driven).

### Compétences (`Controllers\CompetencesController.cs`)
- Competency list/index scoped by IDOR check (`ITeamAccessService.CanAccessCollaborateurAsync`).
- EY sector-based competency catalog (`Catalogue`/`AjouterCompetencesCatalogue`) — sectors and competencies are hardcoded in the controller (lines ~20–78): Assurance, Consulting, Strategy & Transactions, TAX, CBS Support.
- Team competency matrix (`MatriceEquipe`, roles ITAdmin/RH/Manager).
- Manual evaluation (`Evaluate`, ITAdmin/RH) — writes `EvaluationHistorique` with reason "Manuel".
- Self-evaluation (`AutoEvaluation`) with ownership check (`IOwnershipService.OwnsCompetenceAsync`).
- Manager validation with 4-eyes rule (`ValidationManager`) — a manager cannot validate their own competencies.
- Development-plan generation (`GenererPlanDeveloppement`) delegating to `IPlanDeveloppementService`.
- Grade-based competency selection at creation (`GetCompetencesParGrade` via `IReferentielRhService`).

### Formations (`Controllers\FormationsController.cs`)
- Dashboard of enrolled/available formations, filterable; IDOR-checked.
- Module progression (`ReprendreFormation`, `AvancerModule`) with ownership check.
- Exam scheduling (`PlanifierExamen`).
- Enrollment/cancellation (`Inscrire`, `AnnulerInscription`) respecting capacity (`CapaciteMax`/`PlacesPrises`).
- Completion workflow (`TerminerFormation`, ITAdmin/RH) delegating competency update logic to `Services\FormationCompletionEngine.cs`.
- Certificate download (`TelechargerCertificat`) via `Services\CertificatFormationPdf.cs`.
- Adequation score computed in `Details` action (base 45 + bonuses for position/métier/department/plan match).
- Recommendations (`Recommandations`) and career path (`ParcoursCarriere`) views computing gaps vs. next grade.

### Talent Management (`Controllers\TalentController.cs`)
- 9-box dashboard/matrix (`Index`, `Matrix9Box`), role-scoped (ITAdmin/RH/Manager).
- Manual evaluation (`EvaluateAjax`) creating `TalentEvaluation` records; category computed via `TalentScoringEngine.Calculate9BoxCategory`.
- OKR management (`MyOKRs`, `CreateOKR`, `UpdateKeyResult`, `ValidateOKR`) with ownership checks.

### Reporting (`Controllers\ReportingController.cs`)
- `ExecutiveDashboard`: 6 KPIs (actifs, % competencies validated, % formations, critical positions, senior replacements, gap rate), skill-gap evolution (partly mocked 6-month series), top-5 missing competencies, department repartition, heatmap (top 20 collaborators × 15 competencies).
- `SuccessionAnalytics`: filterable by department/grade; critical positions, coverage %, rare competencies (≤2 experts at level ≥4), high potentials (NineBox Star/EmergingTalent).
- `GetSkillGapData`: JSON chart data (top 10 average gaps).

### RH Insights (`Controllers\RhInsightsController.cs`)
- Main `Index`: continuity alerts (Vacant/EnPassation), executive KPI cards, smart alerts, hidden-talent discovery, skill heatmaps, formation insights, promotion & workforce simulators.
- `GetMatchingRemplacants`: detailed succession candidate matching with gap analysis, missing deliverables, transition-plan narrative.
- `GetAiComparison`: pairwise collaborator comparison (compatibility %, shared/missing/transversal skills, readiness score).
- `SimulatePromotionReadiness` / `SimulateWorkforceImpact`: POST endpoints delegating to `IPromotionReadinessService`/`IWorkforceImpactService`, triggering Power Automate notifications above score thresholds (80 and 75 respectively).

### Chatbot/Copilot
See §9 (dedicated section).

### Admin master data (`Areas\Admin\Controllers\*`)
- Each of Departments, SubDepartments, Positions, Grades, BusinessUnits, Locations, ContractTypes, SystemParameters has: Index (search/filter), Create, Edit, Delete/ToggleActive — all `[Authorize(Roles = "ITAdmin")]`.
- Delete operations check for referential usage (e.g., a Department cannot be deleted if it has SubDepartments or Collaborateurs) and block with a TempData error.
- `PositionsController` additionally manages junction records: `PositionRequiredCompetence`, `PositionMandatoryFormation`, `PositionGradeEligibility` (Add/Remove actions).
- `SystemParametersController` respects an `IsEditable` flag preventing edit/delete of protected parameters.

### Home/Portal (`Controllers\HomeController.cs`)
- `Index`: KPI calculation (active count, enrollments, completion rate), recent collaborators, current user's in-progress enrollments with estimated remaining days, certifications, monthly mandatory-formation count. Includes a hardcoded fallback list of mock collaborators if the DB query returns none (lines ~72–78).
- `TestDataverse`: diagnostic endpoint calling `IDataverseService.GetCollaborateursAsync()`.
- `PortailModerne`, `Settings`, `Privacy`: additional/placeholder views.

---

## 4. User Roles & Permissions

Defined in `Authorization\Roles.cs` and `Authorization\Policies.cs`; wired in `Program.cs`.

**Roles** (`Roles.cs`): `ITAdmin`, `RH`, `Manager`, `Collaborateur`, plus composite string constants `ITAdminOrRH`, `ITAdminOrRHOrManager`, `All`.

**Policies** (`Policies.cs` / `Program.cs`):
- `Policies.ITAdminOnly` → role `ITAdmin`
- `Policies.HrPrivileged` → roles `ITAdmin`, `RH`
- `Policies.ManagerOrAbove` → roles `ITAdmin`, `RH`, `Manager`
- Global default policy: `RequireAuthenticatedUser()` applied to every controller/action via an `AuthorizeFilter` registered in `Program.cs` (so every endpoint requires login unless explicitly `[AllowAnonymous]`).

**Observed access patterns across controllers:**
- **ITAdmin only:** all `Areas\Admin\Controllers\*` actions.
- **ITAdminOrRH:** `CollaborateursController` CRUD/departure/successor actions, `CompetencesController` manual evaluation/CRUD, `InscriptionsController` (all actions), `ReportingController` (all actions), `RhInsightsController` (all actions), `FormationsController` Create/Edit/Delete/TerminerFormation.
- **ITAdminOrRHOrManager:** `TalentController` (Index, Matrix9Box, GetCollaborateurDetails, EvaluateAjax, ValidateOKR), `CompetencesController.MatriceEquipe`/`ValidationManager`.
- **`[Authorize]` (any authenticated user):** `CopilotController.Index`.
- **`[AllowAnonymous]`** (but still requires the global authenticated-user filter to apply per-endpoint attributes — code marks every `ChatbotController` GET method `[AllowAnonymous]`): all `ChatbotController` GET endpoints under `/api/chatbot/*`.
- **No explicit role attribute (relies on global auth + internal IDOR checks):** `CollaborateursController.Index/Details`, `CompetencesController.Index/Catalogue/AutoEvaluation`, `FormationsController` (most self-service actions), `HomeController`.

**Row-level authorization (IDOR/ownership) services**, used inside controllers on top of role checks:
- `ITeamAccessService` (`Services\ITeamAccessService.cs` / `TeamAccessService.cs`): `IsPrivileged`, `CanAccessCollaborateurAsync`, `ApplyAccessFilterAsync`, `GetCurrentCollaborateurIdAsync`. Rules: ITAdmin/RH see everything; Manager sees self + direct reports (`ManagerId` match); Collaborateur sees only self.
- `IOwnershipService` (`Services\IOwnershipService.cs` / `OwnershipService.cs`): `OwnsCompetenceAsync`, `OwnsInscriptionAsync`, `OwnsOkrAsync` — all delegate to `ITeamAccessService.CanAccessCollaborateurAsync` on the record's owning collaborateur.
- `IUserContextService` (`Services\IUserContextService.cs` / `UserContextService.cs`): resolves current `ApplicationUser` → linked `Collaborateur`; `GetTeamMembersAsync` returns all actifs for RH, direct reports for Manager, self for standard employee.

**Startup role/user seeding** (`Program.cs`): roles `ITAdmin`, `RH`, `Manager`, `Collaborateur` created if missing; `admin@ey.tn` (ITAdmin) and `rh@ey.tn` (RH) system accounts created; 8 named demo collaborateurs seeded with role resolved from department/grade/poste via `ResolveIdentityRole` local function (RH dept → RH role; "Manager" in grade/poste → Manager role; else Collaborateur) — this role resolution is *idempotently re-applied on every app startup* ("role repair" logic, lines 199–207).

---

## 5. Controllers and Responsibilities

(Route = default unless noted; all require authentication globally per §4.)

| Controller | Area/Route | Responsibility |
|---|---|---|
| `HomeController` | `/` | Landing dashboard, KPI summary, portal/settings/privacy pages |
| `CollaborateursController` | `/Collaborateurs` | Employee CRUD, hierarchy, departure/succession workflow, IA prompt passthrough (`RecommendFormation`, `AskIA` via `FlowiseService`) |
| `CompetencesController` | `/Competences` | Competency CRUD, catalog, self/manager evaluation workflow, team matrix |
| `FormationsController` | `/Formations` | Training catalog, enrollment, progress, completion, certificates, recommendations |
| `InscriptionsController` | `/Inscriptions` | Raw admin CRUD over enrollments (no IDOR checks — ITAdmin/RH only) |
| `CertificatsController` | `/Certificats` | List of completed-training certificates, IDOR-guarded |
| `TalentController` | `/Talent` | 9-box evaluation, OKRs |
| `ReportingController` | `/Reporting` | Executive dashboard & succession analytics (KPI/heatmap computation embedded in controller) |
| `RhInsightsController` | `/RhInsights` and `/api/rhinsights/*` | AI-style HR insights dashboard, succession matching API, promotion/workforce simulators |
| `ChatbotController` | `/api/chatbot/*` | JSON API layer consumed by the n8n chatbot workflow and the Copilot/Widget UI (see §9) |
| `CopilotController` | `/Copilot` | Thin wrapper serving the Copilot chat UI page (`Index` only) |
| `AdminHomeController` | `/Admin/AdminHome` (Area Admin) | Admin dashboard: counts of all master-data entities |
| `BusinessUnitsController`, `ContractTypesController`, `DepartmentsController`, `GradesController`, `LocationsController`, `PositionsController`, `SubDepartmentsController`, `SystemParametersController` | `/Admin/*` (Area Admin) | CRUD over HR master/referential data, all ITAdmin-only |

Controller-embedded business logic flagged by the audit (i.e., logic not delegated to a service, found directly in controller action bodies):
- `ChatbotController`: promotion scoring formulas, succession scoring calls, competence-match helper `CalculerScoreMatch`, Power Automate trigger calls, n8n webhook bridging in `Ask`.
- `CollaborateursController`: hardcoded department→position mapping in `GetPostesParDepartement` (lines ~1053–1085); HTML email composition in `EnvoyerDemandeEntretiens` (lines ~709–715).
- `CompetencesController`: hardcoded EY sector/competency catalog (lines ~20–78); scoring/seuil lookup in `GetSeuilRhAsync`.
- `HomeController`: KPI math, formation-progress estimate, prenom parsing from email, fallback mock collaborateurs.
- `ReportingController`: KPI/heatmap aggregation, partly-mocked chart series.
- `RhInsightsController`: smart-alert generation, hidden-talent labeling, gap-analysis priority mapping, transition-plan narrative text generation (`GenerateTransitionPlanNotes`), a hardcoded `LivrablesObligatoiresRegistry` (deliverables per role).
- `FormationsController`: adequation-score formula in `Details`; hardcoded RGPD/Conformité keyword check in `IsFormationObligatoire`.

---

## 6. Services / Engines and Business Rules

All under `Services\`. The codebase distinguishes **Engines** (static, pure calculation, no EF/DB access, unit-tested) from **Services** (EF Core orchestration, DI-registered, calling Engines).

### Pure calculation engines (static classes)

- **`CompetenceRules.cs`**: `GetSeuilRequis(grade)` → Junior 1 / Senior 2 / Manager 4 (default 1); `GetNiveauCibleParGrade(grade)` → Junior 3 / Senior 4 / Manager 5 (default 3); `NiveauFromScore(score 0-100)` → `ceil(score/20)` clamped 1–5.
- **`CompetenceCatalogService.cs`**: static lookup lists (10 departments, 11 positions, 6 grades); `GetCompetenceType(categorie)` keyword classification (Technique/Fonctionnel/Transverse); `GetDefaultCategorie(nom)` keyword-based category inference (Tech/Audit/Fiscalité/Management/Méthodes/Métier).
- **`DecisionEngine.cs`**: centralized numeric→label classification, explicitly extracted to remove duplication that previously existed in `PromotionReadinessService` and `WorkforceImpactService`. Methods: `ClassifyGapSeverity` (gap≥3 High/=2 Medium/=1 Low/0 Ready), `ClassifyGapPriorityLabel` (French: Critique/Prioritaire/A renforcer/Couvert), `ClassifyGapProgressionImpact`, `ClassifySuccessorType` (≥75 Immediate/≥45 Partial/else High potential), `ClassifyRiskLevel` (≥75 Critical/≥55 Elevated/else Controlled), `ClassifyExposureSignal`, `ClassifyActionPriority` (Critical→High/Elevated→Medium/else Low).
- **`PromotionReadinessEngine.cs`**: `BuildCompetencyGaps`, `ComputeCompatibilityScore` (=100×covered/total), `ComputeReadiness` (floors at 35%), `ComputeScorePerformance`/`ComputeScorePotentiel`/`ComputeScoreAnciennete`, `ComputeMultiCriteriaScore` = **0.40×competencies + 0.25×performance + 0.20×potential + 0.15×seniority**, `ComputePromotionPotential` (caps at 98%), `EstimateMonths`, `BuildFormationRecommendations`.
- **`WorkforceImpactEngine.cs`**: `BuildSuccessors` (readiness = 62% skill match + 18% average level + transversal/seniority/training bonuses, clamped 0–100), `BuildDepartmentExposure`, `ComputeSkillExposure` (rarity-based), `ComputeContinuityRisk`, `ComputeOperationalImpact`, `ComputeDepartmentFragility`, `ComputeStrategicDependency` = **0.38×continuity + 0.32×operational + 0.30×skill exposure**.
- **`SuccessionEngine.cs`**: `BuildExigences` (required competencies from `CompetenceRequiseParPoste`, fallback to departing employee's own level-≥3 competencies), `Score` = **60% competency coverage + 15% seniority (capped at 10y) + 15% career potential + 10% transversal-profile bonus**; eligibility requires tenure ≥ 2 years and contract type not in {Stage, Alternance, Stagiaire, Intern}.
- **`RemplacantMatchingEngine.cs`**: four distinct matching helpers each tied to a specific caller — `BuildCompetencesManquantesSimple` (Depart flow, case-sensitive), `CompatibilitePourcent`/`CompetencesManquantesPourCandidat` (PDF export flow, case-insensitive), `CompetencesManquantesParNoms` (GetRemplacants, case-sensitive), `ScoreMatching` (chatbot API, case-insensitive, returns tuple).
- **`TalentScoringEngine.cs`**: `CalculatePerformanceScore`/`CalculatePotentielScore` (base 3, +bonuses, clamped 1–5), `Calculate9BoxCategory` (3×3 matrix → `NineBoxCategory` enum: Star, FutureLeader, HighProfessional, EmergingTalent, SolidProfessional, InPlace, RisingStar, NeedDevelopment, Underperformer).
- **`FormationCompletionEngine.cs`**: `ResolveCompetenceUpdate(existing?, competenceVisee, grade)` → outcome enum `NoCompetence`/`Created`/`Incremented`/`AlreadyAtTarget`, using `CompetenceRules.GetNiveauCibleParGrade`.

### EF Core orchestration services (interface + implementation, DI-registered in `Program.cs`)

- **`IPromotionReadinessService`/`PromotionReadinessService.cs`**: `BuildSimulatorAsync`, `SimulateAsync(collaborateurId, targetKey)` — loads collaborator + competencies + certifications + talent eval, resolves target grade/position (with fallback competency generation if no referential exists), delegates all scoring to `PromotionReadinessEngine`.
- **`IWorkforceImpactService`/`WorkforceImpactService.cs`**: `BuildSimulatorAsync`, `SimulateAsync(collaborateurId)` — loads target + all active collaborators, delegates scoring to `WorkforceImpactEngine`, builds recommendations and executive narrative text.
- **`IReferentielRhService`/`ReferentielRhService.cs`**: `GetDepartementsAsync`, `GetPostesByDepartementAsync`, `GetCompetencesDisponiblesParGradeAsync`, `GetCategoriesCompetencesAsync` — each queries DB first, with hardcoded fallback lists if empty.
- **`IPlanDeveloppementService`/`PlanDeveloppementService.cs`**: `GenererPourCollaborateurAsync` — finds competency gaps (NiveauActuel < NiveauCible), greedily matches each to a `Formation` (exact match on `CompetenceVisee`, else substring match on `Titre`), creates `PlanDeveloppement` records with `Statut = "A faire"`.
- **`IParametreService`/`ParametreService.cs`**: `GetValue<T>`/`SetValue` over the `Parametre` table, with 10-minute `IMemoryCache` caching.
- **`ITeamAccessService`/`TeamAccessService.cs`**, **`IOwnershipService`/`OwnershipService.cs`**, **`IUserContextService`/`UserContextService.cs`**: see §4.
- **`IPowerAutomateService`/`PowerAutomateService.cs`**: see §10.
- **`IDataverseService`/`DataverseService.cs`**: see §10.
- **`FlowiseService.cs`**: see §9 (no interface; injected as concrete type via `AddHttpClient<FlowiseService>()`).
- **`EmailSender.cs`**: implements ASP.NET Core Identity's `IEmailSender`; currently logs to console instead of sending (stub — no SMTP wiring despite `SystemParameter` seed rows for `SMTP.Host`/`SMTP.Port`).

### PDF generation (static utility classes)
- **`CertificatFormationPdf.cs`**: generates a QuestPDF-based A4 completion certificate (`Generer(Inscription)`).
- **`ComparaisonRemplacantsPdf.cs`**: generates a landscape QuestPDF comparison table of replacement candidates vs. required competencies.

### Business-rule constants observed (cross-service)
- Competency scale: 1–5.
- Grade ladder: Junior → Senior → Manager → Senior Manager → Director → Partner (`GradeEntity.Level` 1–6; also referenced via string-based `switch` in `FormationsController.GetProchainGrade`).
- Certification-expiration urgency: ≤30 days "Critique", ≤60 "Urgent", ≤90 "À planifier" (`ChatbotController.GetCertificationsExpirantes`, `PowerAutomateDtos.CertificationExpirationNotification`).
- Promotion-notification threshold: multi-criteria score > 80 triggers Power Automate `PromotionReady` flow (`RhInsightsController.SimulatePromotionReadiness`).
- Workforce-risk notification threshold: strategic dependency score > 75 triggers `SuccessionRisk` flow (`RhInsightsController.SimulateWorkforceImpact`).

---

## 7. Models and Relationships

Full inventory in `Models\` (58 files). Grouped by domain; relationships as configured in `Data\ApplicationDbContext.cs` `OnModelCreating`.

### Core HR
- **`Collaborateur`**: central employee entity. FKs: `UserId`→`ApplicationUser` (SetNull), `ManagerId`→self (Restrict, self-referential hierarchy via `Equipe` collection), `DepartmentId`, `SubDepartmentId`, `PositionId`, `GradeId`, `BusinessUnitId`, `LocationId`, `ContractTypeId` (all SetNull to referential entities). Legacy string fields (`Departement`, `Poste`, `Grade`, etc.) kept in sync with FK entities via `CollaborateursController.SyncLegacyStringFieldsAsync`. Phase 3 CRM fields: `NombreImplementations`, `ExperienceDomainAnnees`, `ModeDeploiement`. Collections: `Competences`, `Inscriptions`, `CollaborateurCertifications`.
- **`ApplicationUser`**: extends `IdentityUser`; adds `Nom`, `Prenom`.
- **`Competence`**: `NiveauActuel`/`NiveauCible` (1–5), FK `CollaborateurId`, FK `CategorieCompetenceId`; optional link to `EvaluationCompetence`.
- **`Formation`**: catalog entity with capacity tracking (`CapaciteMax`/`PlacesPrises`) and learning-enrichment fields (`Plateforme`, `ExternalUrl`, `Description`, `CertificationNom`, `EstStrategique`, `EstForteDemande`, etc.).
- **`Inscription`**: FK `CollaborateurId`, `FormationId`; `Progression`, `Terminee`, `DateExamen`, `DateCompletion`, `DateExpiration`, `SourceCertification`.
- **`CategorieCompetence`**, **`EvaluationCompetence`** (4-eyes: `AutoEvaluationCollaborateur`, `EvaluationManager`, `ValidationManager`), **`EvaluationHistorique`** (level-change audit trail), **`FormationCompetence`** (composite key), **`PlanDeveloppement`**, **`CompetenceRequiseParPoste`** (string-keyed referential, not FK-linked to `Competence`).

### Talent Management
- **`TalentEvaluation`**: `PerformanceScore`/`PotentielScore` (1–5), `Category` (`NineBoxCategory` enum), governance fields `Statut` (`EvaluationStatus`: Draft/Submitted/Calibrated/Approved/Locked), `ReviewCycleId`, `ApprouveParId`.
- **`OKR`** + nested **`KeyResult`**: `Trimestre`/`OKRStatut` enums, manager validation fields.
- **`ReviewCycle`**: `ReviewCycleStatus` enum (Open/Closed); has many `TalentEvaluation`.

### Succession Planning
- **`SuccessionPlan`**: `SuccessionPlanStatus` enum (Draft/ManagerValidated/HRApproved/Rejected/Archived); FKs to `Collaborateur` (titulaire), `ReviewCycle`, proposer/approver `ApplicationUser`.
- **`SuccessorRankingSnapshot`**: FK `SuccessionPlanId`, `CandidatId`; `ReadinessHorizon` enum (ReadyNow/Ready6To12Months/Ready12To24Months/NotReady).

### Skill Ontology (governance layer, added by migration `AddSkillOntologyAndGovernance`)
- **`Skill`** (canonical name) 1–N **`SkillAlias`**, **`SkillLevel`**, **`SkillCriticality`** (`CriticalityLevel` enum), **`SkillVersion`**; **`SkillRelation`** (source/target self-graph, `SkillRelationType` enum: Prerequisite/RelatedTo/PartOf/Supersedes); **`SkillCategory`** self-referential (parent/sub-category).

### HR Master Data (all inherit `AuditableEntity`: CreatedBy/CreatedAt/UpdatedBy/UpdatedAt)
- **`Department`** 1–N **`SubDepartment`** 1–N **`Position`**; **`GradeEntity`**, **`BusinessUnitEntity`**, **`LocationEntity`**, **`ContractType`** — each 1–N `Collaborateur`.
- **`GradeReferentiel`** (does not inherit `AuditableEntity`): promotion thresholds per grade (`NiveauMinCompetences`, `AncienneteMinAns`, `NombreImplementationsMin`, `ExperienceDomainMinAns`, `GradeSuivant`).
- **`SystemParameter`** (unique index on `Key`) and **`Parametre`** — two parallel key/value config tables.
- **`PositionRequiredCompetence`**, **`PositionMandatoryFormation`**, **`PositionGradeEligibility`**: junction tables under `Position` (Cascade delete from Position side).

### Certifications
- **`Certification`** 1–N **`CollaborateurCertification`** (FK `CollaborateurId` Cascade, FK `CertificationId` Restrict). **`CertificatPdfModel`** is a DTO, not persisted.

### Governance & Audit
- **`DecisionRule`**: versioned rule definitions (`Code`, `ParametresJson`, `Version`, `Actif`).
- **`AuditLog`**: polymorphic (`EntityType` string + `EntityId`, no typed FK), `AuditAction` enum (Created/Updated/StatusChanged/Approved/Locked/Deleted).
- **`AuditableEntity`**: abstract base for the master-data entities above.

### Enums/config models (non-DB or simple enums)
- **`StatutCollaborateur`** (Actif/EnConge/EnPassation/Vacant), **`ModeDeploiement`** (OnPremise/Cloud/Hybride), **`SecteurEYDefinition`** (static EY-sector/competency config, not a DB table), **`DemandeEntretienRequest`** (DTO).

### View Models (non-persisted, one per screen)
`AutoEvaluationCompetenceViewModel`, `ChoisirRemplacantViewModel`, `CompetenceCatalogViewModel`, `DepartViewModel`, `FormationDetailViewModel`, `MatriceEquipeViewModel`, `ParcoursCarriereViewModel`, `RecommandationFormationViewModel`, `RemplacantViewModel`, `ValidationManagerCompetenceViewModel`, `ErrorViewModel`, and the AI-insights view-model set in `Models\InsightsAI\RhInsightsViewModels.cs` (`RhInsightsViewModel`, `ExecutiveKpiCardViewModel`, `SmartAlertViewModel`, `HiddenTalentViewModel`, `SkillHeatmapViewModel`, `FormationInsightViewModel`, `AiComparisonResponse`, `PromotionReadinessSimulatorViewModel` family, `WorkforceImpactSimulatorViewModel` family).

### Deletion behavior summary
- **Cascade**: `Position`→(`PositionRequiredCompetence`, `PositionMandatoryFormation`, `PositionGradeEligibility`); `Skill`→(`SkillAlias`, `SkillLevel`, `SkillCriticality`, `SkillVersion`); `Collaborateur`→(`CollaborateurCertification`, `Competence`); `Formation`→`Inscription`.
- **Restrict**: `Collaborateur.Manager` (self-ref), `Department`→`SubDepartment`, `SubDepartment`→`Position`, `PositionMandatoryFormation`→`Formation`, `EvaluationCompetence`→`Inscription`, `CollaborateurCertification`→`Certification`, `SkillRelation` (both ends), `SkillCategory` (parent).
- **SetNull**: `Collaborateur.User`, and `Collaborateur`'s links to `Department`/`SubDepartment`/`Position`/`GradeEntity`/`BusinessUnitEntity`/`LocationEntity`/`ContractType`.

---

## 8. Existing APIs

All under `/api/...` route prefixes, JSON responses, ASP.NET Core MVC controllers (not a separate Web API project).

### `/api/chatbot/*` (`ChatbotController.cs`) — 25 GET endpoints + 1 POST, listed with method name:
`GetHighPotentials` (`/hr-talent`), `SimulateDeparts` (`/simulate-departs?ids=`), `GetRhStats` (`/stats`), `GetTalentSummary` (`/ai/talent-summary`), `GetHrCopilotData` (`/hr-copilot-data`), `GetPromotables` (`/promotables?dept=`), `GetPromotion` (`/promotion/{id}?gradeCible=`), `GetPostesSansSuccesseur` (`/postes-sans-successeur`), `GetCollaborateur` (`/collaborateur/{id}`), `FindCollaborateur` (`/find?nom=`), `GetPostesARisque` (`/postes-a-risque`), `GetSuccessionData` (`/succession/{collaborateurId}`), `SimulateDepartsByName` (`/simulate-departs-by-name?noms=`), `SimulateFormation` (`/simulate-formation?...`), `GetEvolution` (`/evolution/{collaborateurId}`), `GetTalentEvaluation` (`/talent-evaluation/{collaborateurId}`), `GetSelfManagerComparison` (`/self-manager-comparison/{collaborateurId}`), `GetTalentScoreEvolution` (`/talent-score-evolution/{collaborateurId}`), `GetPendingTalentReviews` (`/pending-talent-reviews?managerId=`), **`Ask`** (POST `/ask` — main chatbot entrypoint, bridges to n8n), `GetCertificationsExpirantes` (`/certifications-expirantes?jours=`), `GetStaffingCRM` (`/staffing-crm?role=`), `GetKpiCRM` (`/kpi-crm`), `GetPlanDeveloppement` (`/plan-developpement/{collaborateurId}?posteCible=`), `GetCriteresPromotion` (`/criteres-promotion?grade=`).
All are `[AllowAnonymous]` at the attribute level (i.e., exempt from the extra role checks other controllers apply, though still subject to the global `RequireAuthenticatedUser` filter registered in `Program.cs` — see §4).

### `/api/rhinsights/*` (`RhInsightsController.cs`), all `[Authorize(Roles = Roles.ITAdminOrRH)]`:
`GetMatchingRemplacants` (`/matching/{id}`), `GetAlertesContinuite` (`/alertes`), `GetAiComparison` (`/compare/{id1}/{id2}`), `SimulatePromotionReadiness` (POST `/promotion-readiness`), `SimulateWorkforceImpact` (POST `/workforce-impact`).

### Other JSON endpoints embedded in MVC controllers
- `CollaborateursController.GetProfilCandidat`, `GetRemplacants`, `GetPostesParDepartement` (JSON, ITAdminOrRH except the last which has no attribute).
- `CompetencesController.GetCompetencesParGrade` (JSON).
- `ReportingController.GetSkillGapData` (JSON, ITAdminOrRH).
- `HomeController.TestDataverse` (JSON diagnostic).

---

## 9. AI / Chatbot / Copilot Components

- **`Controllers\ChatbotController.cs`** (`[ApiController] [Route("api/[controller]")]`): the data/API layer described in §8. Its `Ask` action is the single entrypoint used by the chat UI; it resolves the caller's role server-side via `ITeamAccessService` (never trusts a client-supplied role), forwards `{ message, page, contextId, sessionMemory, sessionHistory }` to the n8n webhook at **`http://localhost:5678/webhook/hr-copilot`** (hardcoded URL in code), and returns the n8n JSON response (or a fallback reply on error) to the caller. Numerous scoring/matching computations for promotion, succession, staffing, and simulation live directly in this controller (see §5 embedded-logic list), calling into `SuccessionEngine`, `TalentScoringEngine`, `DecisionEngine`, `CompetenceRules` where applicable.
- **`Controllers\CopilotController.cs`**: `[Authorize]`-only, single `Index` action serving `Views\Copilot\Index.cshtml` — a full-screen "HR Copilot" chat workspace (header with n8n/model badge, hero welcome screen, conversation view, right-hand Insights/Pinned tabs, command palette on Ctrl/Cmd+K). All AI interaction happens client-side via `POST /api/chatbot/ask`.
- **`Views\Shared\_ChatbotWidget.cshtml`**: floating chat widget included site-wide; same `/api/chatbot/ask` contract; maintains `window.hrContext` (sessionMemory, executionHistory) client-side; renders typed "cards" (collaborateur, succession, promotion, analytics, alerte, simulation, impact, evolution).
- **`Services\FlowiseService.cs`**: injected via `AddHttpClient<FlowiseService>()`; its `GetPredictionAsync` method returns a hardcoded simulated string (`$"Réponse simulée pour: {userPrompt}"`) after an artificial `Task.Delay(100)` — no real HTTP call to a Flowise instance is made. Called from `CollaborateursController.RecommendFormation` and `AskIA`.
- **`Services\DataverseService.cs`/`IDataverseService.cs`**: wraps `Data8.PowerPlatform.Dataverse.Client.OnPremiseClient` for CRUD against a Dataverse `contact` table (`GetCollaborateursAsync`, `GetCollaborateurByIdAsync`, `CreateCollaborateurAsync`, `UpdateCollaborateurAsync`, `DeleteCollaborateurAsync`). Registered conditionally in `Program.cs` only if `Dataverse:EnvironmentUrl`/`Username`/`Password` config keys are all non-empty. Only consumer found in the audit is `HomeController.TestDataverse` (a diagnostic action) — not wired into the chatbot/copilot flows.
- **Configuration**: `appsettings.Development.json` contains a `Dataverse` section with `EnvironmentUrl: "org0a1bd032.crm4.dynamics.com"`, `Username: "hanine.hammami@esprit.tn"`, and a `Password` value present in plain text.
- **`Models\InsightsAI\RhInsightsViewModels.cs`**: view models backing the RH Insights AI dashboard (see §7).

### n8n workflow files (`n8n\*.json`)
Four exported n8n workflows, all triggered by a webhook `POST /webhook/hr-copilot`, sharing the pipeline: **Webhook → Detect Intent (JS) → Fetch Data (HTTP call(s) to `/api/chatbot/*`) → Format Reply (JS) → Respond to Webhook**.
- **`sirh-chatbot-uc1.json`** — baseline ("HR Copilot v2"). Keyword-based intent detection (SUCCESSION, TALENT, PROMOTION, FORMATION, POSTES_CRITIQUES, ANALYTICS) mapped to fixed API routes; follow-up detection reuses the last intent.
- **`sirh-chatbot-uc1-v3.json`** — adds a `FORMATION_SIMULATION` intent (regex-parsed "si je forme X sur Y niveau Z" pattern), a name-extraction function handling French name particles (ben, ibn, el, al, ould), and nominative resolution chains (`/find` → `/succession/{id}` or `/promotion/{id}` or `/collaborateur/{id}` → `/simulate-formation`).
- **`sirh-chatbot-uc1-v3-final.json`** — adds structured error branches (`__notFound`, `__networkError`, `__parseError`, `__httpError`, `__noName`, `__noPosteCible`) and persists `lastPoste`/`lastGrade`/`lastDept` in session memory across turns.
- **`sirh-chatbot-uc1-promotion-nominative.json`** — specialized variant focused on nominative promotion queries ("Is [name] ready for promotion?"), with a dedicated name-detection function and a detailed individual promotion-readiness reply card.

---

## 10. n8n & Power Automate Integrations

### n8n
- Integration point: `ChatbotController.Ask` POSTs to `http://localhost:5678/webhook/hr-copilot` (hardcoded, not read from `appsettings.json`).
- Workflow definitions live in `n8n\*.json` (see §9) — these are n8n's own export format, not part of the compiled application; they must be imported into a running n8n instance to function.

### Power Automate
- **`Services\PowerAutomateService.cs`** / **`IPowerAutomateService.cs`** / **`PowerAutomateSettings.cs`** / **`Services\PowerAutomate\PowerAutomateDtos.cs`**.
- Settings bound from `appsettings.json` section `PowerAutomate`: `IsEnabled` (bool, default true), `TimeoutSeconds` (default 30), `Flows.{PromotionReady, SuccessionRisk, CertificationExpiration, DevelopmentPlanCreated, TalentReviewCompleted}` (webhook URLs).
- **Current config values in `appsettings.json`: all five flow URLs are empty strings** (`""`). `IsEnabled: true` but with no URL configured the service returns a "NotConfigured" result and does not call out.
- Methods: `NotifyPromotionReadyAsync`, `NotifyTalentReviewCompletedAsync`, `NotifySuccessionRiskAsync`, `NotifyCertificationExpirationAsync`, `NotifyDevelopmentPlanCreatedAsync` — each POSTs a strongly-typed DTO as JSON, logs outcome via `ILogger`, and returns a `PowerAutomateResult` (Success/ErrorMessage/HttpStatusCode), handling timeout and network exceptions explicitly.
- Callers found: `RhInsightsController.SimulatePromotionReadiness` (score > 80), `RhInsightsController.SimulateWorkforceImpact` (score > 75), `ChatbotController.GetCertificationsExpirantes` (≤30-day expirations), `ChatbotController.GetPlanDeveloppement` (development-plan creation).

---

## 11. Database Structure and Key Entities

- ORM: Entity Framework Core, SQL Server (`UseSqlServer`), context class `Data\ApplicationDbContext.cs`.
- Connection string (`appsettings.json`): `Server=(localdb)\mssqllocaldb;Database=SIRH_EY;Trusted_Connection=True;MultipleActiveResultSets=true` — LocalDB, development-oriented.
- Identity: ASP.NET Core Identity (`ApplicationUser : IdentityUser`, `IdentityRole`) integrated via `AddIdentity<...>().AddEntityFrameworkStores<ApplicationDbContext>()`.
- DbSets grouped in `ApplicationDbContext.cs` under comment headers: Core HR, Talent Management, Talent Governance/Audit, Succession Planning, Skill Ontology, HR Master Data Referential, Position Relationships, Certifications (Phase 2), Grade Referential (Phase 4).
- Entity/relationship detail: see §7.
- **Migrations** (`Migrations\`, 36 real migrations + designer/snapshot files), chronological highlights:
  - `InitialCreate` (2026-04-08) through a long series of incremental `Ajout*` (French "add") migrations building out competencies, formations, plans, evaluations, référentiel tables (through 2026-04-28).
  - `AddIdentity`, `AddUserToCollaborateur`, `InitClean`, `InitialSeed` (2026-05-05 to 2026-05-07) — Identity integration and schema cleanup.
  - `FormationCompetence`, `AddDateFinReelleToInscription`, `AjoutInscriptionIdEvaluationCompetence`, `AddCategorieCompetenceRelation`, `AddCategorieCompetenceSystem`, `AddTalentManagementTables` (2026-05-07) — talent-management tables introduced.
  - `AddStatutCollaborateur` (2026-05-18), `EnrichCollaborateurEY` (2026-05-21), `HrProfileAndFormationCatalogColumns` (2026-05-26), `SyncCollaborateurSchema` (2026-05-28).
  - `AddHrMasterData` (2026-05-31) and `AddHrMasterDataV2` (2026-06-01) — introduces Department/SubDepartment/Position/Grade/BusinessUnit/Location/ContractType referential tables; V2 adds audit fields (CreatedAt/CreatedBy/UpdatedAt/UpdatedBy) to them.
  - `FormationLearningEnrichment` (2026-06-11) — adds `Plateforme`, `ExternalUrl`, `Description`, `CompetencesRequises`, `CertificationNom`, `SupportPdfUrl`, `MentorEmail`, `EstStrategique`, `EstForteDemande` to `Formations`; `DateCompletion`, `DateExpiration`, `SourceCertification` to `Inscriptions`.
  - `CheckChanges`, `SyncModel`, `TrainingPortalUpdate`, `TempCheck` (all 2026-06-11) — four migrations with names suggesting diagnostic/debugging use; `TrainingPortalUpdate`'s Up/Down are both empty; the other three are minimal/no-op-like. Flagged as technical debt in §14.
  - `AddCertifications` (2026-06-22) — adds `Certifications`, `GradeReferentiels`, `CollaborateurCertifications` tables and `ExperienceDomainAnnees`/`ModeDeploiement`/`NombreImplementations` to `Collaborateurs`.
  - `AddSkillOntologyAndGovernance` (2026-07-02, most recent) — adds `AuditLogs`, `DecisionRules`, `ReviewCycles`, `Skills`, `SkillCategories`, `SkillLevels`, `SkillRelations`, `SkillAliases`, `SkillCriticalities`, `SkillVersions`, `SuccessionPlans`, `SuccessorRankingSnapshots`; augments `TalentEvaluations` with `ApprouveParId`, `DateApprobation`, `ReviewCycleId`, `Statut`.

### Seed data (`Data\*Seeder.cs`, executed in `Program.cs` at startup, in this order)
1. `EvaluationHistoriqueSeeder` — generates 3 historical evaluation snapshots (9 months back) per competency, seeded random (42) for reproducibility.
2. Inline `SeedHrMasterData` (in `Program.cs`) — 10 departments, 8 sub-departments, 14 positions, 6 grades, 5 business units, 5 locations, 6 system parameters, 6 contract types (only runs if `Departments` table is empty).
3. 8 named demo `Collaborateur` records + Identity users (in `Program.cs`), with manager-hierarchy assignment for Audit and Risk teams.
4. `DemoDataSeeder` — 10 formations, 10+ competency categories, ~40 competences, 5 inscriptions, 3 evaluations (version-gated by parameter `DEMO_SEED_VERSION_2026_04_28`).
5. `EnterpriseDemoSeeder` (1125 lines) — a full 30-person EY Tunisia office hierarchy (3 Partners, 5 Directors, 10 Senior Managers/Managers, 12 Consultants), 8 departments, 16 sub-departments, 10 positions, 8 formations, matricules `EY-PTN/DIR/SMG/MGR/SRC/CNS-###` (version `ENTERPRISE_DEMO_V1_2026_06`).
6. `FormationEnrichmentSeeder` — back-fills 17 formations with platform/URL/description/certification metadata (only fills empty fields; version `FORMATION_ENRICHMENT_V1_2026_06`).
7. `PostesReferentielSeeder` — 12 `CompetenceRequiseParPoste` rows for Partner/Director/Senior Manager (version `POSTES_REFERENTIEL_V1_2026_06`).
8. `CompetencesITCRMSeeder` — 20 `CompetenceRequiseParPoste` rows for 7 CRM/D365 positions (version `COMPETENCES_IT_CRM_V1_2026_06`).
9. `FormationsCRMSeeder` — 11 D365/Power Platform/Azure formations (version `FORMATIONS_CRM_V1_2026_06`).
10. `CertificationsSeeder` — 15-certification catalog (PL-900, MB-210/230, AZ-204, PMP, AWS SAA-C03, Tableau, ISO 27001, CNIL DPO, etc.) + 14 collaborator↔certification links (version `CERTIFICATIONS_V1_2026_06`).
11. `GradeReferentielSeeder` — 6-grade promotion-threshold referential (version `GRADE_REFERENTIEL_V1_2026_06`).
12. `SuccessionEnrichmentSeeder` — adds 26 additional competency records across Partners/Directors/Senior Managers/Managers/Senior Consultants so that succession-pool queries return ≥3 candidates (version `SUCCESSION_ENRICHMENT_V1_2026_06`). **Explicitly a post-hoc correction for gaps left by `EnterpriseDemoSeeder`.**
13. `HrDataCorrectionSeeder` — corrects (a) `PotentielCarriere` values that were in French ("Haut potentiel"/"Potentiel solide"/"Développement requis") to the English values ("High"/"Medium"/"Low") expected by `SuccessionEngine`/`PromotionReadinessEngine` scoring, (b) 3 Senior Managers mis-tagged with `RoleRH = "Manager"`, (c) missing Leadership competency records for 2 individuals (version `HR_DATA_CORRECTION_V1_2026_06`). **Explicitly a bug-fix seeder for defects introduced by `EnterpriseDemoSeeder`.**
14. `MissingReferentielSeeder` — adds 23 `CompetenceRequiseParPoste` rows for 7 positions omitted by `PostesReferentielSeeder`/`CompetencesITCRMSeeder` (version `POSTES_REFERENTIEL_COMPLETION_V1_2026_06`).

All seeders are idempotent via a version key stored in the `Parametre`/`SystemParameter` table, checked before inserting.

---

## 12. Current Architecture

- **Pattern**: traditional ASP.NET Core MVC (Razor views + server-rendered HTML) with an embedded set of JSON API controllers/actions for the chatbot and insights layers — not a separate front-end SPA and not a formal Web API project.
- **Layering** (per `docs\ARCHITECTURE_RH.md`, a project-authored target-architecture note, and largely matched by the code): Controllers (HTTP orchestration) → Services (application/business logic) → Models (EF Core entities + view models) → Views (Razor + Tailwind, minimal logic) → `wwwroot\js` (AJAX behaviors). The audit found this pattern followed inconsistently — several controllers (notably `ChatbotController`, `RhInsightsController`, `ReportingController`, `HomeController`) embed non-trivial business logic directly rather than delegating (see §5, §14).
- **Engine vs. Service split**: a deliberate pattern in `Services\` separates pure, unit-tested calculation logic ("Engines": `DecisionEngine`, `PromotionReadinessEngine`, `WorkforceImpactEngine`, `SuccessionEngine`, `RemplacantMatchingEngine`, `TalentScoringEngine`, `FormationCompletionEngine`, `CompetenceRules`) from EF-Core-dependent orchestration ("Services": `PromotionReadinessService`, `WorkforceImpactService`, `ReferentielRhService`, `PlanDeveloppementService`, `ParametreService`, `TeamAccessService`, `OwnershipService`, `UserContextService`, `PowerAutomateService`).
- **Authentication/Authorization**: ASP.NET Core Identity (cookie-based, login path `/Identity/Account/Login`), a global `RequireAuthenticatedUser` filter, role-based `[Authorize(Roles=...)]` attributes, and three named policies (`ITAdminOnly`, `HrPrivileged`, `ManagerOrAbove`), plus custom row-level services (`ITeamAccessService`, `IOwnershipService`) for IDOR protection.
- **Areas**: `Areas\Admin` (master-data CRUD controllers/views) and `Areas\Identity` (scaffolded Identity UI Razor Pages).
- **Routing** (`Program.cs`): area route registered before default `{controller=Home}/{action=Index}/{id?}` route.
- **Front-end tooling**: Tailwind CSS v3.4.0 compiled via npm script (`package.json`: `build:css` → `npx tailwindcss -i ./wwwroot/css/site.css -o ./wwwroot/css/tailwind.css --watch`; `tailwind.config.js` scans `Views/**/*.cshtml` and `wwwroot/**/*.js`). No JS bundler; 6 hand-written JS files and 15 CSS files in `wwwroot\` (custom + Tailwind hybrid). Chart.js is used for charts **[docs claim]**.
- **`SIRH_Frontend\`** directory exists at repo root but contains only a `node_modules` folder — no source or config files. Not a functioning separate frontend project.
- **PDF generation**: QuestPDF (`Services\CertificatFormationPdf.cs`, `Services\ComparaisonRemplacantsPdf.cs`).
- **External integrations**: n8n (chatbot workflow engine, §9/§10), Power Automate (notification flows, §10), Microsoft Dataverse (conditional, §9), Flowise (placeholder only, §9).
- **Testing**: xUnit test project `SIRH.EY.Tests` with 8 test classes, all targeting `Services\` engines/business-rule classes: `CompetenceCatalogServiceTests`, `CompetenceRulesTests`, `DecisionEngineTests`, `FormationCompletionEngineTests`, `PromotionReadinessEngineTests`, `RemplacantMatchingEngineTests`, `SuccessionEngineTests`, `WorkforceImpactEngineTests`. No test files target `Controllers\`, `Data\`, `Areas\Admin\`, or any integration/end-to-end path.

---

## 13. Reused Business Logic

Cases where the same engine/service is deliberately shared across multiple controllers to guarantee consistent results:
- **`SuccessionEngine`** is called from both `CollaborateursController` (UI: `ChoisirRemplacant`, `Depart`, `ExportComparaisonRemplacantsPdf`) and `ChatbotController`/`RhInsightsController` (API: `GetSuccessionData`, `GetMatchingRemplacants`) — same scoring formula used by UI screens and the chatbot/API layer.
- **`TalentScoringEngine`** is called from both `TalentController` (9-box UI) and `ChatbotController` (`GetTalentEvaluation`) for identical performance/potential/9-box computation.
- **`DecisionEngine`** is called from both `PromotionReadinessEngine` and `WorkforceImpactEngine` for all severity/risk/priority label classification, replacing what were previously duplicated inline classifications in `PromotionReadinessService` and `WorkforceImpactService` (per the services-audit finding).
- **`CompetenceRules.GetNiveauCibleParGrade`** is used by both `CompetencesController` (competency creation) and `FormationCompletionEngine` (formation-completion competency creation).
- **`FormationCompletionEngine.ResolveCompetenceUpdate`** is invoked from both `FormationsController.TerminerFormation` and `InscriptionsController.Terminer`, ensuring the two different completion entry points (self-service vs. admin) update competencies identically.
- **`ITeamAccessService`/`IOwnershipService`** are used consistently across `CollaborateursController`, `CompetencesController`, `FormationsController`, `TalentController`, `CertificatsController` for IDOR checks, rather than each controller re-implementing access logic.

---

## 14. Duplicated Logic

- **`RemplacantMatchingEngine`** contains four separate, non-unified competency-matching formulas (`BuildCompetencesManquantesSimple`, `CompatibilitePourcent`/`CompetencesManquantesPourCandidat`, `CompetencesManquantesParNoms`, `ScoreMatching`), each tied to a different caller with different case-sensitivity behavior (some ordinal/case-sensitive, some case-insensitive) doing conceptually the same "which required competencies is this candidate missing" calculation. The services-audit agent noted test comments in `RemplacantMatchingEngineTests.cs` treat this as intentional per-caller behavior rather than an oversight, but it remains four code paths for one underlying question.
- **Two parallel key/value configuration tables** exist: `SystemParameter` (used by `Areas\Admin\SystemParametersController`) and `Parametre` (used by `IParametreService`/`ParametreService` and by seeder version-gating). Both store `Key`/`Value`(or `Code`/`Valeur`)-style configuration independently.
- **Two parallel notions of "target competency level by grade"** exist: `CompetenceRules.GetNiveauCibleParGrade` (static hardcoded switch) and the DB-driven `GradeReferentiel.NiveauMinCompetences` referential populated by `GradeReferentielSeeder`. Both are read by different code paths (`CompetenceRules` by `CompetencesController`/`FormationCompletionEngine`; `GradeReferentiel` by `PromotionReadinessService` for seniority-threshold lookups).
- **Grade-ladder "next grade" logic** is duplicated as a hardcoded switch in at least two places: `FormationsController.GetProchainGrade` and the `ResolveIdentityRole`/grade-comparison logic elsewhere; `GradeReferentiel.GradeSuivant` also encodes the same progression in the database.
- **Department→Position mapping** is hardcoded in `CollaborateursController.GetPostesParDepartement` as a switch/case, while `IReferentielRhService.GetPostesByDepartementAsync` independently queries the DB for the same relationship (with its own separate hardcoded fallback list).
- **EY sector/competency catalog** is hardcoded directly inside `CompetencesController` (lines ~20–78) as well as represented by the non-DB `SecteurEYDefinition`/`SecteurCompetenceDefinition` models — two representations of the same reference data.

---

## 15. Technical Debt

- **Four migrations with diagnostic-sounding names and empty/near-empty bodies**, all dated 2026-06-11: `CheckChanges`, `SyncModel`, `TrainingPortalUpdate` (confirmed empty `Up`/`Down`), `TempCheck`.
- **Seeder execution order is a hard dependency, not just idempotent**: `HrDataCorrectionSeeder`, `SuccessionEnrichmentSeeder`, and `MissingReferentielSeeder` all exist specifically to patch defects/gaps left by `EnterpriseDemoSeeder` (wrong `PotentielCarriere` language, wrong `RoleRH` values, missing succession-pool competencies, missing referential rows). If `EnterpriseDemoSeeder` were re-seeded without also re-running the three correction seeders (or if their order changed), the original defects would reappear — each seeder's idempotency check only guards against re-insertion of its own rows, not against the correctness of upstream data.
- **`EmailSender.cs`** does not send real email — it logs to console. `SystemParameter` seed rows for `SMTP.Host`/`SMTP.Port` exist but are not wired to any actual SMTP client in the code reviewed.
- **`FlowiseService.cs`** performs no real HTTP call; it is a hardcoded placeholder response.
- **Power Automate flow URLs are empty** in `appsettings.json`; the notification code path exists and is called from `RhInsightsController`/`ChatbotController` but silently no-ops (`NotConfigured` result) in the current configuration.
- **Dataverse credentials stored in plain text** in `appsettings.Development.json` (`EnvironmentUrl`, `Username`, `Password` values present in the file).
- **No controller-level, integration, or end-to-end tests** — the entire `SIRH.EY.Tests` project (8 classes) targets only pure `Services\` engine classes; `Controllers\`, `Areas\Admin\`, `Data\`, and all Razor views are untested.
- **Business logic embedded in controllers** rather than delegated to services — see the per-controller list in §5 (notably `ChatbotController`, `RhInsightsController`, `ReportingController`, `HomeController`, parts of `CollaborateursController` and `CompetencesController`).
- **Hardcoded fallback/mock data** used when the database is empty or a query returns no rows, e.g. `HomeController.Index` fallback mock collaborateurs (lines ~72–78), various hardcoded referential fallback lists in `ReferentielRhService`, and partly-mocked chart series in `ReportingController.ExecutiveDashboard` (6-month skill-gap evolution, monthly progression).
- **`SIRH_Frontend\`** directory is an empty stub (only `node_modules`), present in the repo without any active source content.
- **`docs\DOCUMENTATION_TECHNIQUE_COMPLETE.md`** is dated "15 juin 2026," which is after several code changes observed in later migrations/seeders (e.g. `AddSkillOntologyAndGovernance`, 2026-07-02) — the document's claims about total migration/seeder counts should not be assumed current without re-verification against the live code.
- **n8n webhook URL is hardcoded** (`http://localhost:5678/webhook/hr-copilot` in `ChatbotController.Ask`) rather than sourced from `appsettings.json`, unlike the Power Automate flow URLs which are configuration-driven.

---

## 16. Missing Features

Purely factual gaps observed relative to what surrounding code/data implies should exist, with no proposed remediation:
- No real outbound email sending implementation (see §15) despite an `IEmailSender` interface, a controller action (`EnvoyerDemandeEntretiens`) that composes interview-request HTML emails, and seeded SMTP configuration parameters.
- No real LLM/AI backend behind `FlowiseService` — the `RecommendFormation`/`AskIA` actions in `CollaborateursController` that call it will always return the same simulated string regardless of input.
- No controller/integration/UI test coverage (only service-layer unit tests exist).
- `Views\InsightsAI\` folder exists but is empty (no `.cshtml` files) — the AI-insights UI is implemented instead under `Views\RHInsights\`.
- `CertificatsController` has a PDF-download action referenced as commented-out (`DownloadCertificat`) per the controllers-audit note — no active PDF download route in that controller (formation certificates are instead downloaded via `FormationsController.TelechargerCertificat`).
- Power Automate flows are code-complete but have no configured destination URLs in `appsettings.json` (all five flow keys are empty strings), so notifications for promotion-readiness, workforce/succession risk, certification expiration, and development-plan creation cannot currently reach an external Power Automate flow without configuration.

---

## 17. Opportunities for Improvement

(Observations of contrast/inconsistency in the existing code, stated as facts about the current state — not recommendations.)
- The project's own architecture note (`docs\ARCHITECTURE_RH.md`) states a target of keeping business rules out of controllers ("Controllers: actions MVC... Services: cas d'usage RH et regles applicatives"), while the controllers audit found multiple controllers (`ChatbotController`, `RhInsightsController`, `ReportingController`, `HomeController`) with substantial scoring/aggregation logic embedded directly in action methods rather than in a service — a direct contrast between the stated target architecture and the current implementation.
- Two parallel parameter tables (`SystemParameter`, `Parametre`) and two parallel "next grade"/"target competency level" computations (hardcoded `CompetenceRules`/switch statements vs. DB-driven `GradeReferentiel`) currently coexist (§14).
- Seeder correction chain (`HrDataCorrectionSeeder`, `MissingReferentielSeeder`, `SuccessionEnrichmentSeeder`) indicates the base `EnterpriseDemoSeeder` dataset required three follow-up passes to reach a state usable by `SuccessionEngine`/`PromotionReadinessEngine` scoring (§11, §15).
- The n8n integration point uses a hardcoded localhost URL while the comparable Power Automate integration uses configuration-bound URLs — the two external-automation integrations are wired inconsistently with respect to configurability.

---

*End of factual audit. All statements above are grounded in the files and classes cited; claims sourced only from `docs\` are explicitly marked **[docs claim]** and were not independently re-verified line-by-line against every corresponding code path.*
