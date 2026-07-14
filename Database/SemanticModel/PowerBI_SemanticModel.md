# SIRH.EY — Power BI Semantic Model

Built on top of `analytics.*` (Database/Staging/003_AnalyticsViews.sql) and `stg.*`
(Database/Staging/001_CreateStagingSchema.sql). Two changes from the raw views,
both driven by the audit findings:

1. **`analytics.vw_Executive` is not imported.** A single precomputed row can't be
   sliced by anything. Its 13 metrics are rebuilt below as real measures on the
   fact tables they summarize — same numbers, but now filterable by department,
   grade, date, etc. from any page.
2. **Descriptive text columns are trimmed from the fact views on import**
   (`CollaborateurNom`, `Prenom`, `DepartmentName`, …) — they're now supplied by
   `Dim Collaborateur` / `Dim Organisation` through relationships instead of being
   repeated in five different fact tables.

---

## Dimensions

| Dimension | Source | Key | Notes |
|---|---|---|---|
| **Dim Date** | generated calendar (not sourced from `stg`) | `Date` | Year/Quarter/Month/Day. No calendar table exists in the OLTP schema — this is new. |
| **Dim Collaborateur** | `stg.Collaborateurs` | `Id` | Id, Nom, Prenom, Actif, Statut, DateEmbauche, PotentielCarriere, NiveauPreparationSuccession, ManagerId, PositionId, GradeId, BusinessUnitId, LocationId. No department/position/grade *names* — those come from the related dimensions below, avoiding the duplication the raw views had. |
| **Dim Collaborateur (Candidat)** | role-playing copy of Dim Collaborateur | `Id` | A second import of the same table, renamed. Needed because `Fact Succession` has two collaborator roles (titulaire and candidate) and Power BI allows only one *active* relationship between a given pair of tables — see Relationships. |
| **Dim Organisation** | `stg.Positions` merged with `stg.SubDepartments` and `stg.Departments` (Power Query merge at load, keyed on `SubDepartmentId`→`DepartmentId`) | `PositionId` | One flattened table exposing `DepartmentName`, `SubDepartmentName`, `PositionName` plus their source ids. Chosen over a 3-table snowflake because the source tables are small (15/23/16 rows) and a single flat dimension gives a cleaner drill hierarchy (see Hierarchies). |
| **Dim Grade** | `stg.Grades` merged with `stg.GradeReferentiels` (matched on `Grades.Name = GradeReferentiels.Grade` — the only link available; there is no FK between them in the OLTP schema) | `Id` | Name, Level, NiveauMinCompetences, AncienneteMinAns, NombreImplementationsMin, ExperienceDomainMinAns, GradeSuivant. Flag: 8/44 active collaborators have no `GradeId` and 4/44 have a `Grade` text that matches no `GradeReferentiel` row (from the prior audit) — those rows will show `(blank)` Grade attributes until the source data is corrected. |
| **Dim BusinessUnit** | `stg.BusinessUnits` | `Id` | |
| **Dim Location** | `stg.Locations` | `Id` | |
| **Dim Skill** | `stg.Skills` merged with `stg.SkillCategories` and the latest `stg.SkillCriticalities` row per skill | `Id` | Nom, CategoryName, CriticalityLabel, CriticalityWeight (calculated column, see Measures). `SkillCriticalities` is currently empty — every skill will show "Unclassified" until that table is populated. |
| **Dim Formation** | `stg.Formations` | `Id` | Titre, Categorie, Plateforme, EstCertifiante, CapaciteMax. |
| **Dim ReviewCycle** | `stg.ReviewCycles` | `Id` | Currently 0 rows — `Fact Talent` will show `(blank)` cycle until this is populated. |

---

## Fact tables

| Fact | Source | Grain |
|---|---|---|
| **Fact Skills** | `analytics.vw_Skills`, trimmed to `CompetenceId, CollaborateurId, SkillId, CategorieCompetenceId, NiveauActuel, NiveauCible, Gap, IsAtTarget, GapSeverity, DateEvaluation` | Collaborateur × Competence assessment |
| **Fact Training** | `analytics.vw_Training`, trimmed to `InscriptionId, CollaborateurId, FormationId, Terminee, Progression, DateInscription, DateCompletion, DateExamen, HotNoteGlobale, HotNoteContenu, HotNoteFormateur, HotRecommande, ColdNoteApplication, ColdNoteImpactBusiness, CapacityUtilizationRatio` | Enrollment |
| **Fact Talent** | `analytics.vw_Talent`, trimmed to `TalentEvaluationId, CollaborateurId, ReviewCycleId, PerformanceScore, PotentielScore, NineBoxCategoryCode, EvaluationStatusCode, DateEvaluation, Actif, TotalOKRs, CompletedOKRs` | Collaborateur × Review |
| **Fact Promotion** | `analytics.vw_Promotion`, trimmed to `CollaborateurId, GradeId, AncienneteAnnees, AvgNiveauActuel, AvgNiveauCible, CompetenceAttainmentRatio, LatestPerformanceScore, LatestPotentielScore, ReadinessBand` | Active Collaborateur (current-state snapshot — no transaction date, not related to Dim Date) |
| **Fact Succession** | `analytics.vw_Succession`, trimmed to `SuccessionPlanId, CollaborateurTitulaireId, PlanStatusCode, DateCreation, SnapshotId, CandidatId, Rang, ScoreSuccession, ScoreCouverture, ReadinessHorizonCode, DateSnapshot` | Plan × ranked candidate (0 rows today — see prior audit) |

---

## Relationships

All relationships are **single-direction (Dim → Fact)** unless noted, to keep filter propagation predictable across a model where one dimension (`Dim Collaborateur`) feeds five fact tables.

| From | To | Cardinality | Active | Note |
|---|---|---|---|---|
| Dim Collaborateur[Id] | Fact Skills[CollaborateurId] | 1:* | Yes | |
| Dim Collaborateur[Id] | Fact Training[CollaborateurId] | 1:* | Yes | |
| Dim Collaborateur[Id] | Fact Talent[CollaborateurId] | 1:* | Yes | |
| Dim Collaborateur[Id] | Fact Promotion[CollaborateurId] | 1:1 | Yes | Fact Promotion is one row per active collaborator. |
| Dim Collaborateur[Id] | Fact Succession[CollaborateurTitulaireId] | 1:* | Yes | The position-holder side. |
| Dim Collaborateur (Candidat)[Id] | Fact Succession[CandidatId] | 1:* | Yes | Separate role-playing import rather than a second (inactive) relationship on the same table — lets both "filter by titulaire" and "filter by candidate" slicers work at once without `USERELATIONSHIP` in every measure. |
| Dim Collaborateur[ManagerId] | Dim Collaborateur[Id] | *:1 | **No** (self-referencing) | Power BI auto-deactivates a second relationship between the same table. Only needed if org-chart depth analysis is added later, via a Parent-Child DAX pattern (`PATH()`) — not built here, out of scope until requested. |
| Dim Organisation[PositionId] | Dim Collaborateur[PositionId] | 1:* | Yes | |
| Dim Grade[Id] | Dim Collaborateur[GradeId] | 1:* | Yes | |
| Dim BusinessUnit[Id] | Dim Collaborateur[BusinessUnitId] | 1:* | Yes | |
| Dim Location[Id] | Dim Collaborateur[LocationId] | 1:* | Yes | |
| Dim Skill[Id] | Fact Skills[SkillId] | 1:* | Yes | |
| Dim Formation[Id] | Fact Training[FormationId] | 1:* | Yes | |
| Dim ReviewCycle[Id] | Fact Talent[ReviewCycleId] | 1:* | Yes | |
| Dim Date[Date] | Fact Skills[DateEvaluation] | 1:* | Yes | |
| Dim Date[Date] | Fact Training[DateInscription] | 1:* | Yes | Primary training date. |
| Dim Date[Date] | Fact Training[DateCompletion] | 1:* | **No** | Inactive — activate per-measure with `USERELATIONSHIP` when a "by completion date" view is needed. |
| Dim Date[Date] | Fact Talent[DateEvaluation] | 1:* | Yes | |
| Dim Date[Date] | Fact Succession[DateSnapshot] | 1:* | Yes | |
| Dim Date[Date] | Fact Succession[DateCreation] | 1:* | **No** | Inactive, same pattern as Fact Training above. |

**Not related:** `Fact Succession.Poste` (free text) vs. `Dim Organisation.PositionName` — `SuccessionPlans.Poste` is a text column, not an FK, so it cannot be joined reliably; this is the same legacy-text-vs-FK split the original project audit flagged for `Collaborateurs.Poste`. Do not build this relationship until `SuccessionPlans` gets a real `PositionId` FK.

---

## Hierarchies

| Hierarchy | Table | Levels | Notes |
|---|---|---|---|
| **Calendar** | Dim Date | Year → Quarter → Month → Day | Standard drill hierarchy for every trend visual. |
| **Organisation** | Dim Organisation | Department → SubDepartment → Position | Natural drill path for every department-level breakdown across all five fact tables. |
| **Skill Taxonomy** | Dim Skill | SkillCategory → Skill | `SkillCategories` is self-referential (`ParentCategoryId`), so a true unlimited-depth tree would need a Parent-Child DAX pattern; the data today is shallow enough that a fixed two-level hierarchy (Category → Skill) is sufficient. Revisit if category nesting grows beyond one level. |

**Not modeled as a hierarchy:**
- **Grade** — `Level` (1–6) is a single sortable attribute, not a multi-level tree. Use *Sort Column* (sort `Grade Name` by `Level`) rather than building a hierarchy object.
- **GradeSuivant** — a linked-list "next grade" pointer, not a parent/child tree; used directly in the Promotion measures below rather than as a drillable hierarchy.

---

## Measures

Hosted on a dedicated disconnected `_Measures` table (standard Power BI hygiene — keeps DAX off the fact tables' own column list) unless noted.

### Skills

```
Total Competence Assessments := COUNTROWS ( 'Fact Skills' )

Competencies At Target := CALCULATE ( COUNTROWS ( 'Fact Skills' ), 'Fact Skills'[IsAtTarget] = 1 )

Skill Coverage % := DIVIDE ( [Competencies At Target], [Total Competence Assessments] )

Critical Skill Gaps := CALCULATE ( COUNTROWS ( 'Fact Skills' ), 'Fact Skills'[GapSeverity] = "Critical" )

Average Gap := AVERAGE ( 'Fact Skills'[Gap] )
```

`Dim Skill` calculated column (weight used below):
```
CriticalityWeight =
SWITCH (
    'Dim Skill'[CriticalityCode],
    3, 4,   -- Strategic
    2, 3,   -- High
    1, 2,   -- Medium
    0, 1,   -- Low
    1       -- Unclassified — treated as Medium until SkillCriticalities is populated
)
```
```
Criticality-Weighted Gap Score :=
DIVIDE (
    SUMX ( 'Fact Skills', 'Fact Skills'[Gap] * RELATED ( 'Dim Skill'[CriticalityWeight] ) ),
    SUMX ( 'Fact Skills', RELATED ( 'Dim Skill'[CriticalityWeight] ) )
)
```

### Training

```
Total Enrollments := COUNTROWS ( 'Fact Training' )

Completed Enrollments := CALCULATE ( COUNTROWS ( 'Fact Training' ), 'Fact Training'[Terminee] = 1 )

Training Completion % := DIVIDE ( [Completed Enrollments], [Total Enrollments] )

Avg Satisfaction Score := AVERAGE ( 'Fact Training'[HotNoteGlobale] )

Recommend Rate % :=
DIVIDE (
    CALCULATE ( COUNTROWS ( 'Fact Training' ), 'Fact Training'[HotRecommande] = TRUE ),
    CALCULATE ( COUNTROWS ( 'Fact Training' ), NOT ISBLANK ( 'Fact Training'[HotRecommande] ) )
)

Avg Business Impact := AVERAGE ( 'Fact Training'[ColdNoteImpactBusiness] )

Capacity Utilization % :=
AVERAGEX (
    SUMMARIZE ( 'Fact Training', 'Fact Training'[FormationId] ),
    CALCULATE ( AVERAGE ( 'Fact Training'[CapacityUtilizationRatio] ) )
)
```
`Capacity Utilization %` is averaged per distinct formation first, not per enrollment row — `CapacityUtilizationRatio` is a formation-level attribute repeated on every enrollment row, so a plain row-level `AVERAGE` would over-weight formations with more enrollments.

### Talent

```
Collaborators Reviewed := DISTINCTCOUNT ( 'Fact Talent'[CollaborateurId] )

Talent Review Coverage % :=
DIVIDE ( [Collaborators Reviewed], CALCULATE ( COUNTROWS ( 'Dim Collaborateur' ), 'Dim Collaborateur'[Actif] = TRUE ) )

High Potential Rate % :=
DIVIDE (
    CALCULATE ( COUNTROWS ( 'Fact Talent' ), 'Fact Talent'[NineBoxCategoryCode] IN { 1, 4 } ),  -- Star, Emerging Talent
    COUNTROWS ( 'Fact Talent' )
)

OKR Success Rate % := DIVIDE ( SUM ( 'Fact Talent'[CompletedOKRs] ), SUM ( 'Fact Talent'[TotalOKRs] ) )

Calibration Completion % :=
DIVIDE (
    CALCULATE ( COUNTROWS ( 'Fact Talent' ), 'Fact Talent'[EvaluationStatusCode] >= 2 ),  -- Calibrated, Approved, Locked
    COUNTROWS ( 'Fact Talent' )
)
```

### Promotion

```
Promotion Ready Count := CALCULATE ( COUNTROWS ( 'Fact Promotion' ), 'Fact Promotion'[ReadinessBand] = "Ready" )

Promotion Ready % := DIVIDE ( [Promotion Ready Count], COUNTROWS ( 'Fact Promotion' ) )

Avg Competence Attainment := AVERAGE ( 'Fact Promotion'[CompetenceAttainmentRatio] )
```

### Succession

```
Succession Plan Count := DISTINCTCOUNT ( 'Fact Succession'[SuccessionPlanId] )

Positions Without Successor :=
CALCULATE (
    DISTINCTCOUNT ( 'Fact Succession'[SuccessionPlanId] ),
    ISBLANK ( 'Fact Succession'[SnapshotId] )
)

Succession Coverage % := DIVIDE ( [Succession Plan Count] - [Positions Without Successor], [Succession Plan Count] )
```

### Cross-subject (replace `vw_Executive`)

```
Active Headcount := CALCULATE ( COUNTROWS ( 'Dim Collaborateur' ), 'Dim Collaborateur'[Actif] = TRUE )
```

Every other Executive-page number is one of the measures above, evaluated with no extra filter context — e.g. the Executive page's "Skill Coverage %" card is literally `[Skill Coverage %]`, but because it's a measure (not a baked-in view column) it also works correctly when a user adds a Department slicer, which `vw_Executive`'s single precomputed row could never do.
