# CHATBOT_API_SHAPES.md — SIRH.EY

JSON contracts for the 25 endpoints of `Controllers\ChatbotController.cs` (`[ApiController] [Route("api/[controller]")]`, base path `/api/chatbot`), extracted directly from the controller source, `Services\SuccessionEngine.cs`, `Services\TalentScoringEngine.cs`, and the relevant `Models\*.cs` files. No content taken from `docs\` or from prior summaries.

## Serialization ground rules (verified, apply to every endpoint below)

- No `AddJsonOptions`/`PropertyNamingPolicy`/`JsonSerializerOptions`/Newtonsoft configuration exists anywhere in the project (`Program.cs` or elsewhere) — confirmed by full-repo grep. ASP.NET Core's `AddControllersWithViews()` therefore uses its **default** `System.Text.Json` settings, whose default `PropertyNamingPolicy` is **camelCase**. Every anonymous object returned via `Ok(...)`/`NotFound(...)`/`BadRequest(...)` is serialized camelCase regardless of the C# property casing used in the controller (e.g. a literal `Nom = ...` is written to the wire as `"nom"`). All field names below are given in their **actual wire casing** (post camelCase-policy), not the C# source casing.
- The project has `<Nullable>enable</Nullable>` (`SIRH.EY.csproj`) and targets `net10.0`. Because Nullable Reference Types are enabled, ASP.NET Core's `[ApiController]` convention treats **non-nullable `string`/reference-type action parameters with no default value** as implicitly required. If such a query/route parameter is entirely absent from the request, the framework short-circuits **before the action body runs** and returns an automatic `400` with a `ValidationProblemDetails` body — this is distinct from, and takes priority over, any `BadRequest(new { error = "..." })` the action code itself would otherwise return for a *present-but-blank* value. Both cases are documented per-endpoint below. The generic shape is:

```jsonc
// Automatic 400 — required parameter missing entirely from the request
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "<paramName>": ["The <paramName> field is required."] },
  "traceId": "00-....-....-00"
}
```
- `null` value C# properties are **not omitted** by the serializer (no `DefaultIgnoreCondition` configured) — nullable fields appear in the JSON as `"field": null`, never dropped.
- All 25 endpoints except `Ask` (POST `/ask`) carry `[AllowAnonymous]` at the attribute level (still subject to the app's global `RequireAuthenticatedUser` filter registered in `Program.cs`, per `PROJECT_KNOWLEDGE.md` §4).
- Where an endpoint has no engine/service call, "Backend source" reads "Inline in `ChatbotController`" — this reflects the actual code, not an omission.

---

## GET /api/chatbot/find

**Params**: `nom`: `string` (required — no `[FromQuery]` attribute, but bound from query since it's a simple type on a GET action with no route placeholder; non-nullable with no default ⇒ implicitly required by NRT convention, source: query)

**Response 200 shape**:
```json
{
  "id": 12,
  "nom": "Sami Trabelsi"
}
```
`id`: `int`. `nom`: `string` (built as `collaborateur.Prenom + " " + collaborateur.Nom`, never null).

**Error shapes**:
- Missing `nom` entirely → automatic `400 ValidationProblemDetails` (see ground rules).
- `nom` present but matches nobody (including `nom=""`, which matches every name via `.Contains("")` — **so an empty `nom` value actually always matches the first collaborator in table order**, not a 404) → if truly no match: **`404` with an empty body** — the code calls bare `return NotFound();` (no error object), unlike every other endpoint in this controller.
- No explicit invalid-format case (any string is valid input; matching is a case-insensitive substring `Contains`).

**Backend source**: Inline in `ChatbotController.FindCollaborateur` — single EF `FirstOrDefaultAsync` with client-evaluated `.ToLower().Contains(...)`. No engine/service.

---

## GET /api/chatbot/collaborateur/{id}

**Params**: `id`: `int` (required, route, no `:int` constraint — a non-numeric segment fails model binding)

**Response 200 shape**:
```json
{
  "id": 12,
  "nom": "Sami Trabelsi",
  "poste": "Partner",
  "grade": "Partner",
  "departement": "Assurance",
  "competences": [
    { "nom": "Leadership stratégique", "niveauActuel": 5 }
  ],
  "certifications": [
    {
      "nom": "PMP",
      "organisme": "PMI",
      "domaine": "Project Management",
      "codeExamen": null,
      "dateObtention": "2024-03-15",
      "dateExpiration": "2027-03-15",
      "statut": "Active"
    }
  ],
  "nombreImplementations": 4,
  "experienceDomainAnnees": 6,
  "modeDeploiement": "Cloud"
}
```
- `poste`, `grade`, `departement`: `string?` (`Collaborateur.Poste`/`Grade`/`Departement` are nullable legacy string fields).
- `competences`: array of `{ nom: string, niveauActuel: int }` — note these two field names are **not** camelCase-transformed because they're taken straight from `x.Nom`/`x.NiveauActuel` via an anonymous projection `new { x.Nom, x.NiveauActuel }`; policy still lowercases first letter → `nom`, `niveauActuel`. `null` if `Competences` navigation is null (rendered as `competences: null`), `[]` if empty.
- `certifications`: `[]` if none (code explicitly falls back to `Enumerable.Empty<object>()` rather than `null`). Each item's `nom`/`organisme`/`domaine`/`codeExamen` are `string?` (from `Certification`), `dateObtention`/`dateExpiration` are `string?` formatted `"yyyy-MM-dd"` (`null` if the underlying `DateTime?` is null), `statut` is `string?` (`CollaborateurCertification.Statut`, free-text "Active"|"Expirée"|"En cours" per model comment, not an enum).
- `nombreImplementations`: `int?` (`Collaborateur.NombreImplementations`, passed through raw, can be `null`).
- `experienceDomainAnnees`: `int?` (same, can be `null`).
- `modeDeploiement`: `string?` — `ModeDeploiement` enum converted with `.ToString()`; possible values: `"OnPremise"`, `"Cloud"`, `"Hybride"`, or `null` if unset.

**Error shapes**:
- `id` not found → `404 { "error": "Collaborateur non trouvé." }`.
- `id` non-numeric in the URL → binding failure → automatic `400` (empty/ValidationProblemDetails-style; framework default since no explicit `:int` constraint routes it to this action but the parameter fails to bind).

**Backend source**: Inline in `ChatbotController.GetCollaborateur`. No engine/service.

---

## GET /api/chatbot/talent-evaluation/{id}

Route parameter name in code is `collaborateurId` with an `:int` constraint: `talent-evaluation/{collaborateurId:int}`.

**Params**: `collaborateurId`: `int` (required, route, constrained to `:int` — a non-numeric segment means the route simply doesn't match this action; falls through to a plain routing 404)

**Response 200 shape**:
```json
{
  "employee": {
    "id": 12,
    "nom": "Sami Trabelsi",
    "poste": "Partner",
    "grade": "Partner",
    "departement": "Assurance"
  },
  "evaluation": {
    "source": "manual",
    "performanceScore": 4,
    "potentielScore": 5,
    "category": "Star",
    "categoryLabel": "⭐ Talent stratégique",
    "managerComments": "Excellent leadership visible sur le dernier mandat.",
    "employeeComments": "Ambitionne un rôle de Senior Partner.",
    "dateEvaluation": "2026-05-10"
  },
  "latestOkrs": [
    {
      "id": 7,
      "objectif": "Développer 2 nouveaux comptes stratégiques",
      "annee": 2026,
      "trimestre": "Q3",
      "statut": "Active",
      "progressionGlobale": 45,
      "dateFinCible": "2026-09-30",
      "keyResults": [
        { "description": "Signer 1 nouveau client Tier-1", "progression": 30, "statut": "InProgress" }
      ]
    }
  ],
  "recommendation": "⭐ Talent stratégique — performance 4/5, potentiel 5/5."
}
```
- `evaluation.source`: `string`, one of `"manual"` (a `TalentEvaluation` row exists) or `"computed"` (no manual eval — scores computed live by `TalentScoringEngine`).
- `evaluation.performanceScore`/`potentielScore`: `int` (1–5).
- `evaluation.category`: `string` — `NineBoxCategory` enum name via `.ToString()`. Possible values: `"Star"`, `"FutureLeader"`, `"HighProfessional"`, `"EmergingTalent"`, `"SolidProfessional"`, `"InPlace"`, `"RisingStar"`, `"NeedDevelopment"`, `"Underperformer"`.
- `evaluation.categoryLabel`: `string` — emoji-prefixed French label from `NineBoxExtensions.GetDisplayName()`: `"⭐ Talent stratégique"` (Star), `"🚀 Leader stratégique"` (FutureLeader), `"💎 Expert métier"` (HighProfessional), `"🌱 Potentiel émergent"` (EmergingTalent), `"✅ Collaborateur clé"` (SolidProfessional), `"📍 Stable dans le poste"` (InPlace), `"⭐ Haut potentiel"` (RisingStar), `"📈 Besoin d'accompagnement"` (NeedDevelopment), `"⚠️ Performance insuffisante"` (Underperformer).
- `evaluation.managerComments`/`employeeComments`: `string?` — `null` whenever `source == "computed"` (no manual eval to source comments from); when `source == "manual"`, mapped from `TalentEvaluation.CommentairesPerformance`/`CommentairesPotentiel` respectively (both nullable in the model), so can still be `null` even for a manual record.
- `evaluation.dateEvaluation`: `string`, `"yyyy-MM-dd"`. `DateTime.Today` when computed.
- `latestOkrs`: `[]` if the collaborator has no OKR cycles (`dernierCycle == null`). Otherwise contains **only the most recent (year, trimestre) cycle's** OKRs, not full history.
  - `trimestre`: `string`, `Trimestre` enum name → `"Q1"`, `"Q2"`, `"Q3"`, `"Q4"`.
  - `statut` (OKR-level): `string`, `OKRStatut` enum name → `"Draft"`, `"Active"`, `"OnTrack"`, `"AtRisk"`, `"Completed"`, `"Cancelled"`.
  - `progressionGlobale`: `int` (0–100).
  - `keyResults[].statut`: `string`, `KeyResultStatut` enum name → `"NotStarted"`, `"InProgress"`, `"AtRisk"`, `"Completed"`, `"Cancelled"`.
  - `keyResults[].progression`: `int` (0–100, computed property `KeyResult.Progression`).
- `recommendation`: `string`, narrative built as `"{categoryLabel} — performance {p}/5, potentiel {q}/5"` + `" (score calculé automatiquement, aucune évaluation manuelle enregistrée)."` **(présent si `source == "computed"`)**, else just `"."`.

**Error shapes**:
- `collaborateurId` not found → `404 { "error": "Collaborateur introuvable." }`.
- `collaborateurId` non-numeric → route doesn't match `:int` constraint → plain routing `404` (no JSON body — this is ASP.NET Core's built-in "no endpoint matched" response, not a controller-authored one).

**Backend source**: `TalentScoringEngine.CalculatePerformanceScore`/`CalculatePotentielScore`/`Calculate9BoxCategory` — **only invoked when no manual `TalentEvaluation` exists** (`source == "computed"`); otherwise the stored `TalentEvaluation` row is used as-is. `NineBoxExtensions.GetDisplayName()` for the label.

---

## GET /api/chatbot/self-manager-comparison/{id}

Route parameter name is `collaborateurId:int`.

**Params**: `collaborateurId`: `int` (required, route, `:int` constrained)

**Response 200 shape**:
```json
{
  "employee": { "id": 12, "nom": "Sami Trabelsi", "poste": "Partner", "grade": "Partner" },
  "totalCompetences": 6,
  "competencesValideesParManager": 4,
  "ecartMoyen": -3.5,
  "comparisons": [
    {
      "competenceId": 41,
      "competence": "Business Development",
      "categorie": "Management",
      "selfEvaluation": { "score": 80, "comment": "Solide en négociation.", "date": "2026-06-01" },
      "managerEvaluation": { "score": 75, "comment": "À nuancer sur le closing.", "date": "2026-06-05", "validated": true },
      "gap": -5,
      "alignment": "Aligné"
    }
  ]
}
```
- Only competencies that already have an `EvaluationCompetence` row are included (`Where(c => c.EvaluationCompetence != null)`) — competencies never self-evaluated are absent from `comparisons` entirely, not shown with nulls.
- `ecartMoyen`: `double?` — average of `gap` across only the entries where `gap` is non-null (i.e. manager has evaluated); `null` if none have been manager-evaluated yet.
- `comparisons[].categorie`: `string?` (`CategorieCompetence.Nom`, `null` if `CategorieCompetenceId` unset).
- `comparisons[].selfEvaluation.score`: `int` (`EvaluationCompetence.AutoEvaluationCollaborateur`, 0–100).
- `comparisons[].selfEvaluation.comment`: `string?`.
- `comparisons[].selfEvaluation.date`: `string?` `"yyyy-MM-dd"`, `null` if `DateAutoEvaluation` unset.
- `comparisons[].managerEvaluation.score`: `int?` (`EvaluationManager`, `null` until manager evaluates).
- `comparisons[].managerEvaluation.validated`: `bool` (`ValidationManager`).
- `comparisons[].gap`: `int?` — `EvaluationManager - AutoEvaluationCollaborateur`; `null` if manager hasn't scored yet.
- `comparisons[].alignment`: `string`, one of exactly four values: `"En attente de validation manager"` (`gap == null`), `"Aligné"` (`|gap| <= 10`), `"Manager plus indulgent"` (`gap > 10`… actually `gap > 0` beyond the 10 threshold), `"Manager plus sévère"` (`gap < 0` beyond the 10 threshold).

**Error shapes**:
- Not found → `404 { "error": "Collaborateur introuvable." }`.
- Non-numeric id → routing `404` (no body), per the `:int` constraint.

**Backend source**: Inline in `ChatbotController.GetSelfManagerComparison`. No engine/service.

---

## GET /api/chatbot/talent-score-evolution/{id}

Route parameter is `collaborateurId:int`.

**Params**: `collaborateurId`: `int` (required, route, `:int` constrained)

**Response 200 shape**:
```json
{
  "employee": { "id": 12, "nom": "Sami Trabelsi", "poste": "Partner", "grade": "Partner" },
  "previousEvaluation": {
    "source": "manual",
    "performanceScore": 3,
    "potentielScore": 4,
    "category": "EmergingTalent",
    "categoryLabel": "🌱 Potentiel émergent",
    "dateEvaluation": "2026-01-15"
  },
  "currentEvaluation": {
    "source": "manual",
    "performanceScore": 4,
    "potentielScore": 5,
    "category": "Star",
    "categoryLabel": "⭐ Talent stratégique",
    "dateEvaluation": "2026-05-10"
  },
  "delta": { "performance": 1, "potentiel": 1, "categoryChanged": true },
  "changes": [
    "Performance : 3/5 → 4/5 (+1)",
    "Potentiel : 4/5 → 5/5 (+1)",
    "Catégorie 9-box : 🌱 Potentiel émergent → ⭐ Talent stratégique"
  ],
  "summary": "Sami Trabelsi : progression entre le 15/01/2026 et le 10/05/2026. Performance : 3/5 → 4/5 (+1) Potentiel : 4/5 → 5/5 (+1) Catégorie 9-box : 🌱 Potentiel émergent → ⭐ Talent stratégique"
}
```
- `previousEvaluation`: **object or `null`** — `null` if fewer than 2 `TalentEvaluation` rows exist for the collaborator (`previousSnapshot` stays unset). When present, `source` is always `"manual"` (the code never computes a synthetic "previous" snapshot).
- `currentEvaluation.source`: `"manual"` if at least 1 `TalentEvaluation` row exists, else `"computed"` (via `TalentScoringEngine`, same as the `talent-evaluation` endpoint).
- `delta`: **object or `null`** — `null` if `previousEvaluation` is `null` (i.e. only 1 or 0 prior evaluations). When present: `{ performance: int, potentiel: int, categoryChanged: bool }` (signed deltas, current − previous).
- `changes`: `string[]` — free-text lines, **not an enum**; possible entries follow the patterns `"Performance : {a}/5 → {b}/5 ({+/-}{delta})"`, `"Potentiel : {a}/5 → {b}/5 ({+/-}{delta})"`, `"Catégorie 9-box : {oldLabel} → {newLabel}"`, or (if no numeric/category change at all) the single line `"Aucun changement détecté depuis la dernière évaluation."`, or (if no previous evaluation exists at all) the single line `"Aucune évaluation précédente disponible pour comparaison."`.
- `summary`: `string` — one paragraph combining collaborator name, `"progression"`/`"régression"`/`"stabilité"` trend word (computed but **not exposed as its own field** — only embedded in this text), and the `changes` lines joined by spaces; different phrasing entirely when there's no previous evaluation (mentions `"première évaluation enregistrée"` or `"aucun historique d'évaluation manuelle"`).

**Error shapes**:
- Not found → `404 { "error": "Collaborateur introuvable." }`.
- Non-numeric id → routing `404` (no body).

**Backend source**: `TalentScoringEngine` (only for the current snapshot, only when no manual eval exists — identical rule to `talent-evaluation/{id}`). Previous-snapshot logic is always inline (never computed, only ever a real prior `TalentEvaluation` row).

---

## GET /api/chatbot/evolution/{id}

Route parameter is unconstrained `collaborateurId` (`evolution/{collaborateurId}`).

**Params**: `collaborateurId`: `int` (required, route, no `:int` constraint)

**Response 200 shape (with history)**:
```json
{
  "collaborateurNom": "Sami Trabelsi",
  "periodeAnalysee": "Jan 2026 → Jun 2026",
  "tendance": "Progression de +2 niveau(x)",
  "timeline": [
    {
      "periode": "2026-01",
      "evolutions": [
        { "competence": "Leadership stratégique", "avant": 3, "apres": 4, "raison": "Formation" }
      ]
    }
  ]
}
```
**Response 200 shape (no history at all)**:
```json
{
  "collaborateurNom": "Sami Trabelsi",
  "message": "Aucun historique disponible pour ce collaborateur.",
  "timeline": []
}
```
- `periodeAnalysee`/`message` are **mutually exclusive** — only one of the two shapes above is ever returned (the empty-history variant has no `periodeAnalysee` or `tendance` keys at all, and vice versa).
- `tendance`: `string`, free text — either `"Stable"` (if `historique.Last().NiveauNouveau - historique.First().NiveauAncien <= 0`) or `"Progression de +{n} niveau(x)"` (positive delta). Not a fixed enum.
- `timeline[].periode`: `string`, `"yyyy-MM"` grouping key.
- `timeline[].evolutions[].competence`: `string?` (`EvaluationHistorique.Competence?.Nom`, `null` if the navigation failed to load — unlikely given the `Include`).
- `timeline[].evolutions[].avant`/`apres`: `int` (`NiveauAncien`/`NiveauNouveau`).
- `timeline[].evolutions[].raison`: `string?` (`EvaluationHistorique.Raison`, free text set by seeders/manual evaluation, e.g. `"Manuel"`, `"Formation"`, `"Évaluation initiale"`).

**Error shapes**:
- Not found → `404 { "error": "Collaborateur introuvable." }`.
- Non-numeric id → binding failure → automatic `400` (no `:int` constraint on this route, so it reaches model binding rather than a routing 404).

**Backend source**: Inline in `ChatbotController.GetEvolution`, reading `EvaluationHistorique` directly. No engine/service.

---

## GET /api/chatbot/pending-talent-reviews

**Params**: `managerId`: `int?` (optional, query, default `null` — filters to that manager's direct reports if supplied; if omitted, all active collaborators are scanned)

**Response 200 shape**:
```json
{
  "employeesAwaitingValidation": [
    {
      "id": 34,
      "name": "Aziz Belhadj",
      "status": "En attente de validation manager",
      "progress": "2/5",
      "lastEvaluationDate": "2026-06-20"
    }
  ]
}
```
- `status`: `string`, always the single fixed literal `"En attente de validation manager"` (only employees with at least one pending item are included, so this is a constant, not a variable status field).
- `progress`: `string` — free-form `"{validatedCount}/{totalEvaluatedCount}"` (**note**: this is validated-so-far over total-ever-self-evaluated, not "pending/total"; not a numeric field).
- `lastEvaluationDate`: `string`, `"yyyy-MM-dd"` — the **latest** among that employee's still-pending self-evaluation dates.
- Array is `[]` if nobody is pending (or if `managerId` matches nobody / a manager with no reports).

**Error shapes**: none beyond the generic — `managerId` supplied but non-numeric (e.g. `?managerId=abc`) → automatic `400` (binding failure on a nullable int still fails when a non-empty, non-numeric value is supplied). No 404 path exists; an unmatched `managerId` simply yields `{ "employeesAwaitingValidation": [] }` with a `200`.

**Backend source**: Inline in `ChatbotController.GetPendingTalentReviews`. No engine/service.

---

## GET /api/chatbot/succession/{id}

Route parameter is unconstrained `collaborateurId` (`succession/{collaborateurId}`). **Maximum-effort section per instructions.**

**Params**: `collaborateurId`: `int` (required, route, no `:int` constraint, source: route)

**Response 200 shape**:
```json
{
  "collaborateurNom": "Karim Ben Youssef",
  "poste": "Partner",
  "grade": "Partner",
  "competencesRequises": [
    { "nom": "Business Development", "niveauRequis": 5 },
    { "nom": "Leadership stratégique", "niveauRequis": 5 }
  ],
  "top3": [
    {
      "id": 41,
      "nom": "Hatem Gharbi",
      "poste": "Partner",
      "grade": "Partner",
      "departement": "Assurance",
      "scoreSuccession": 78,
      "scoreCouverture": 83,
      "competencesCommunes": 5,
      "competencesManquantes": ["Stakeholder management"],
      "profilTransversal": false,
      "ancienneteAns": 6,
      "estEligible": true
    }
  ],
  "candidatsEnAttente": [
    {
      "id": 55,
      "nom": "Yosra Hammami",
      "poste": "Director",
      "grade": "Director",
      "departement": "Assurance",
      "scoreSuccession": 61,
      "scoreCouverture": 70,
      "competencesCommunes": 4,
      "competencesManquantes": ["Business Development"],
      "profilTransversal": true,
      "ancienneteAns": 3,
      "estEligible": true
    }
  ],
  "avertissement": "1 candidat(s) non éligible(s) au remplacement direct — grade différent ou ancienneté < 2 ans."
}
```
- `poste`/`grade`: `string?` — `partant.Poste`/`partant.Grade` passed through **without** the `?? ""` fallback used inside `top3`/`candidatsEnAttente` items, so these two top-level fields can genuinely be `null`, unlike the per-candidate `poste`/`grade`/`departement` below.
- `competencesRequises`: from `SuccessionEngine.BuildExigences` — array of `{ nom: string, niveauRequis: int }` (record `CompetenceExigee(string Nom, int NiveauRequis)`, camelCased). **Source priority**: rows from `CompetenceRequiseParPoste` where `Poste == partant.Poste` (exact `==` match, case-sensitive at the SQL/LINQ level); **if none exist**, falls back to the departing employee's *own* competencies with `NiveauActuel >= 3` (i.e. the "requirements" become a copy of what the departing person already has at a meaningful level). Deduplicated by name (case-insensitive), keeping the highest `NiveauRequis`/`NiveauActuel` per name.
- `top3`/`candidatsEnAttente`: built via the same `ToApiItem` mapping (`SuccessionEngine.ResultatScore` → JSON), only the selection filter differs:
  - `top3`: same grade as `partant` (case-insensitive) **AND** `EstEligible` **AND** `NbCommunes > 0`, top 3 by `ScoreSuccession` descending.
  - `candidatsEnAttente`: (different grade **OR** not eligible) **AND** `NbCommunes > 0`, top 3 by `ScoreSuccession` descending.
  - A candidate with `NbCommunes == 0` appears in **neither** list.
  - Field types per item: `id: int`, `nom: string` (interpolated, never null), `poste`/`grade`/`departement`: `string` (note: **coalesced to `""` if null** via `?? ""` inside `ToApiItem`, unlike the top-level `poste`/`grade` above — so these are never JSON `null`, only possibly `""`), `scoreSuccession: int` (0–100, the weighted 60/15/15/10 score — see `SuccessionEngine.Score`), `scoreCouverture: int` (0–100, the 60%-weight competency-coverage sub-score alone), `competencesCommunes: int` (`NbCommunes` — competencies at full or "acceptable gap" coverage), `competencesManquantes: string[]` (names with a gap, including the "acceptable gap" ones flagged for reinforcement), `profilTransversal: bool` (different department **and** ≥1 shared competency), `ancienneteAns: int` (whole years, from `Collaborateur.Anciennete` computed property), `estEligible: bool` (tenure ≥ 2 years **and** contract type not in `{Stage, Alternance, Stagiaire, Intern}`).
- `avertissement`: `string?` — `null` if `candidatsEnAttente` is empty, else `"{count} candidat(s) non éligible(s) au remplacement direct — grade différent ou ancienneté < 2 ans."`.

**Error shapes**:
- `collaborateurId` not found → `404 { "error": "Collaborateur non trouvé." }`.
- `collaborateurId` non-numeric → binding failure → automatic `400`.
- No explicit error if `competencesRequises` ends up empty (e.g. departing employee also has no competencies at all) — the endpoint still returns `200` with `"competencesRequises": []`, `"top3": []`, `"candidatsEnAttente": []` (every candidate necessarily has `NbCommunes == 0` against an empty requirement list, so both lists are empty but no error is raised).

**Backend source**: `Services\SuccessionEngine.cs` — `SuccessionEngine.BuildExigences(referentiel, partant.Competences)` then `SuccessionEngine.Score(candidate, exigences, deptPartant)` per candidate. This is the **same engine** used by `CollaborateursController.ChoisirRemplacant`/`Depart` (UI) — the code comment explicitly states this endpoint and the Razor view are guaranteed identical scores.

---

## GET /api/chatbot/promotion/{id}?gradeCible=

Route: `promotion/{id:int}`. **Maximum-effort section per instructions.**

**Params**: `id`: `int` (required, route, `:int` constrained) · `gradeCible`: `string?` (optional, query, default `null` — if omitted, defaults to the current grade's `GradeSuivant` from `GradeReferentiel`, or `"Senior"` if that lookup also fails)

**Response 200 shape**:
```json
{
  "scorePromotion": 74,
  "gradeActuel": "Senior Manager",
  "gradeCible": "Director",
  "competencesAcquises": ["Gestion de projet", "Analyse"],
  "competencesManquantes": ["Conseil stratégique"],
  "delaiEstime": "3-6 mois",
  "justificationRH": "Sarra Ben Ali — ancienneté 4.2 an(s), performance 4/5, potentiel solide. Couverture compétences Director : 2/3. À développer : Conseil stratégique. Score promotion global : 74/100.",
  "scoreDetail": {
    "competences": 66.7,
    "performance": 80.0,
    "potentiel": 60.0,
    "anciennete": 100.0,
    "poids": "40% comp + 25% perf + 20% pot + 15% anc"
  }
}
```
- `scorePromotion`: `int` (rounded), formula **inline in this action** (not `PromotionReadinessEngine`): `round(0.40×scoreComp + 0.25×scorePerformance + 0.20×scorePotentiel + 0.15×scoreAnciennete)`. **This is a separate, independently-implemented copy of the same 40/25/20/15 weighting scheme found in `Services\PromotionReadinessEngine.ComputeMultiCriteriaScore` and again (a third time) in this same controller's `GetPromotables` — three parallel implementations of nominally the same formula, not a shared call.**
- `gradeActuel`: `string` — `collaborateur.Grade ?? "Junior"` (never null on the wire).
- `gradeCible`: `string` — the resolved/defaulted target grade name (never null).
- `competencesAcquises`/`competencesManquantes`: `string[]` — computed by comparing each `CompetenceRequiseParPoste.NiveauRequis` (looked up first by `collaborateur.Poste`, falling back to `gradeCible` name if the poste yields nothing) against `NiveauActuel`, using threshold `NiveauActuel >= (int)(NiveauRequis * 0.6)` for "acquise" (a looser 60%-of-required threshold than `SuccessionEngine`'s per-level rule).
- `delaiEstime`: `string` — `"Prêt maintenant"` if `competencesManquantes` is empty, else `"{min}-{min+3} mois"` where `min = max(3, gapCount*3)`. Free text, not an enum.
- `justificationRH`: `string` — narrative paragraph assembled from: seniority (`Math.Round(anciennete,1)` years), a performance clause (`"performance {n}/5"` if a `TalentEvaluation` exists, else `"performance non évaluée"`), a potential clause (`"potentiel {n}/5"` if evaluated, else one of `"haut potentiel"`/`"potentiel solide"`/`"développement requis"`/`"potentiel non évalué"` derived from `Collaborateur.PotentielCarriere`), a coverage sentence, a gap sentence (`"À développer : {top 3 missing}…"` or `"Toutes les compétences requises sont couvertes."`), and the final score sentence.
- `scoreDetail`: object with the four component sub-scores (`competences`, `performance`, `potentiel`, `anciennete`, all `double`, rounded to 1 decimal, **0–100 scale each** — not the same scale as the final 0–100 `scorePromotion`, these are the pre-weight inputs) plus a fixed descriptive string `poids` (`"40% comp + 25% perf + 20% pot + 15% anc"`).

**Error shapes**:
- `id` not found (or found but not `Actif`, since the query filters `c.Actif`) → `404 { "error": "Collaborateur introuvable." }`.
- `id` non-numeric → route doesn't match `:int` → plain routing `404` (no body).
- No validation on `gradeCible` — an unrecognized target grade name silently falls through (no matching `GradeReferentiel` row → `scoreAnciennete` uses the flat `anciennete*25` fallback formula instead of a threshold-relative one); no error is raised.

**Backend source**: Entirely inline in `ChatbotController.GetPromotion` — reads `GradeReferentiel` and `CompetenceRequiseParPoste` directly via EF, computes its own weighted score. Does **not** call `Services\PromotionReadinessEngine.cs` or `IPromotionReadinessService` (those are used only by `RhInsightsController.SimulatePromotionReadiness`).

---

## GET /api/chatbot/criteres-promotion?grade=

**Params**: `grade`: `string?` (optional, query, default `null`)

**Response 200 shape — no `grade` supplied (full referentiel)**:
```json
{
  "referentiel": [
    {
      "grade": "Junior",
      "niveau": 1,
      "niveauMinCompetences": 1.5,
      "ancienneteMinAns": 0,
      "nombreImplementationsMin": 0,
      "experienceDomainMinAns": 0,
      "gradeSuivant": "Senior",
      "description": "Consultant en phase d'apprentissage."
    }
  ]
}
```
**Response 200 shape — `grade` supplied**:
```json
{
  "grade": "Senior",
  "niveau": 2,
  "criteresPourMaintenir": {
    "niveauMinCompetences": 2.8,
    "ancienneteMinAns": 2,
    "nombreImplementationsMin": 2,
    "experienceDomainMinAns": 1,
    "description": "Autonomie confirmée sur les livrables."
  },
  "gradeSuivant": {
    "grade": "Manager",
    "niveauMinCompetences": 3.5,
    "ancienneteMinAns": 3,
    "nombreImplementationsMin": 4,
    "experienceDomainMinAns": 3,
    "description": "Encadrement d'équipe et responsabilité de livrables."
  }
}
```
- **Note the type conflict**: `gradeSuivant` is a plain `string` (grade name) in each `referentiel[]` item of the no-`grade` shape, but a full **nested object** (or `null`) in the single-`grade` shape — same field name, different type depending on which of the two response variants is returned.
- `niveauMinCompetences`: `double` (e.g. `1.5`–`4.8` per `GradeReferentiel` seed data). All other numeric fields (`niveau`, `ancienneteMinAns`, `nombreImplementationsMin`, `experienceDomainMinAns`) are `int`.
- `gradeSuivant` (single-grade shape): `object?` — `null` if `actuel.GradeSuivant` is empty/null (top of the ladder, e.g. Partner) or if that name doesn't resolve to another `GradeReferentiel` row.

**Error shapes**:
- Referentiel table empty entirely → `404 { "error": "Référentiel de grades non initialisé." }` (applies regardless of whether `grade` was supplied).
- `grade` supplied but not found among seeded grades → `404 { "error": "Grade '{grade}' non trouvé dans le référentiel." }` (message interpolates the exact input string, not a generic message).

**Backend source**: Inline in `ChatbotController.GetCriteresPromotion`, reading `GradeReferentiel` directly. No engine/service.

---

## GET /api/chatbot/promotables?dept=

**Params**: `dept`: `string?` (optional, query, default `null` — substring filter on `departement`, case-insensitive)

**Response 200 shape**:
```json
{
  "total": 3,
  "collaborateurs": [
    {
      "id": 41,
      "nom": "Hatem Gharbi",
      "poste": "Partner",
      "gradeActuel": "Partner",
      "gradeCible": null,
      "departement": "Assurance",
      "scorePromotion": 82.4,
      "ancienneteAns": 8.3
    }
  ]
}
```
- `total`: `int` — count of the **already-filtered** (`scorePromotion >= 60`, and `dept`-matched if given) result set, i.e. equals `collaborateurs.length`, capped at 10 (see below).
- Every item has `scorePromotion >= 60` — this is a hard `Where` filter before pagination, then `OrderByDescending(scorePromotion).Take(10)`.
- `scorePromotion`: `double`, rounded to 1 decimal. **Independently computed formula, same 40/25/20/15 weighting as `GetPromotion` and `PromotionReadinessEngine`, but a third separate inline implementation** — inputs here use `NiveauPreparationSuccession`/competency average directly (not the per-required-competency gap analysis `GetPromotion` uses).
- `gradeActuel`: `string?` (`Collaborateur.Grade`, nullable).
- `gradeCible`: `string?` — `GradeReferentiel.GradeSuivant` for the collaborator's current grade; `null` if no matching `GradeReferentiel` row or already at the top grade.
- `ancienneteAns`: `double`, rounded to 1 decimal (note: **not an `int`** here, unlike `succession/{id}`'s `ancienneteAns` which is `int`).

**Error shapes**: none beyond the generic — an unmatched `dept` substring simply yields `{ "total": 0, "collaborateurs": [] }` with `200`. `dept` has no required/blank validation (fully optional, default `null`).

**Backend source**: Entirely inline in `ChatbotController.GetPromotables`. Does not call `Services\PromotionReadinessEngine.cs`.

---

## GET /api/chatbot/plan-developpement/{id}?posteCible=

Route: `plan-developpement/{collaborateurId}` (no `:int` constraint).

**Params**: `collaborateurId`: `int` (required, route) · `posteCible`: `string` (required, query, no default ⇒ implicitly required by NRT convention)

**Response 200 shape**:
```json
{
  "collaborateurNom": "Aziz Belhadj",
  "gradeActuel": "Consultant",
  "posteCible": "Architect CRM",
  "scoreCouverte": 40.0,
  "totalCompetences": 5,
  "competencesCouvertes": 2,
  "competencesADevelopper": 3,
  "certificationsDejaPossedees": ["PL-900"],
  "nombreImplementations": 1,
  "planDeveloppement": [
    {
      "competence": "Architecture solution",
      "gap": 3,
      "statut": "Manquant",
      "priorite": "Critique",
      "formation": {
        "id": 22,
        "titre": "Architecting Microsoft Power Platform Solutions",
        "dureeHeures": 24,
        "plateforme": "Microsoft Learn",
        "certificationVisee": "PL-600",
        "estCertifiante": true,
        "niveauDifficulte": "Avancé"
      }
    }
  ],
  "dureeEstimeeSemaines": 9
}
```
- **Note the field-name typo preserved from source**: `scoreCouverte` (missing the "r" from "couverture") — this is the literal wire name, not `scoreCouverture`.
- `gradeActuel`: `string?` (`collab.Grade`, nullable, passed through raw — not defaulted to `"Junior"` unlike `GetPromotion`).
- `scoreCouverte`: `double` — `% of reqs.Count with gap == 0`, rounded to 1 decimal; `0.0` if `reqs.Count == 0` (though that path returns 404 first, see below).
- `certificationsDejaPossedees`: `string[]` — names of currently-`Active`, non-expired certifications (nulls filtered out with `.Where(n => n != null)`).
- `nombreImplementations`: `int` — `collab.NombreImplementations ?? 0` (never `null` on the wire, unlike the raw nullable field returned by `/collaborateur/{id}`).
- `planDeveloppement`: only entries with `gap > 0` (fully-covered competencies are excluded from the array, though they're still counted in `competencesCouvertes`).
  - `statut`: `string`, one of `"Couvert"` (unreachable here since gap>0 items only, kept for completeness of the enum), `"À renforcer"` (`niveau > 0`), `"Manquant"` (`niveau == 0`).
  - `priorite`: `string`, one of `"Critique"` (`gap >= 3`), `"Haute"` (`gap == 2`), `"Normale"` (else).
  - `formation`: `object?` — `null` if no `Formation` row matches the competency name (via `CompetenceVisee` contains-match, falling back to `Titre` contains-match); when present, `dureeHeures: int`, `plateforme`/`certificationVisee`/`niveauDifficulte`: `string?`, `estCertifiante: bool`.
- `dureeEstimeeSemaines`: `int` — `sum(max(2, gap*3))` over the (already gap>0-filtered) `plan` list.
- **Side effect (not in the response body)**: fires `IPowerAutomateService.NotifyDevelopmentPlanCreatedAsync` unconditionally on every successful call (not gated by a score threshold, unlike the promotion/workforce simulators in `RhInsightsController`); failure to notify is only logged (`_logger.LogWarning`), never surfaced in the HTTP response.

**Error shapes**:
- `collaborateurId` not found → `404 { "error": "Collaborateur introuvable." }`.
- `posteCible` missing entirely from the query string → automatic `400 ValidationProblemDetails` (implicit-required, non-nullable `string` parameter).
- `posteCible` present but blank/whitespace → **this specific check is unreachable in practice**: the method signature has no default and no nullable annotation, so the implicit-required rule already rejects a *missing* key before the body runs; a *present-but-empty* value (`?posteCible=`) **does** reach the body and is caught by `if (string.IsNullOrWhiteSpace(posteCible)) return BadRequest(new { error = "Paramètre posteCible requis, ex: ?posteCible=Architect CRM" });`.
- `posteCible` has no matching `CompetenceRequiseParPoste` rows → `404 { "error": "Rôle cible '{posteCible}' introuvable dans le référentiel." }`.

**Backend source**: Inline in `ChatbotController.GetPlanDeveloppement`; triggers `Services\PowerAutomateService.NotifyDevelopmentPlanCreatedAsync` (`Services\PowerAutomate\PowerAutomateDtos.cs` → `DevelopmentPlanNotification`) as a side effect. No scoring engine involved.

---

## GET /api/chatbot/simulate-formation?collaborateurId=&posteCible=&competence=&niveauSimule=

**Maximum-effort section per instructions.**

**Params** (all four bound from query, none has a route placeholder, none has a default value ⇒ **all four are implicitly required**):
- `collaborateurId`: `int` (required, query)
- `posteCible`: `string` (required, query)
- `competence`: `string` (required, query)
- `niveauSimule`: `int` (required, query)

**Response 200 shape**:
```json
{
  "collaborateurNom": "Aziz Belhadj",
  "posteCible": "Architect CRM",
  "competenceSimulee": "Architecture solution",
  "niveauSimule": 4,
  "scoreAvant": 40,
  "scoreApres": 60,
  "gain": 20,
  "impact": "Impact significatif — accélère fortement la succession",
  "recommandation": "Prioriser cette formation : elle seule rapproche Aziz du seuil de succession."
}
```
- `posteCible`: echoed verbatim (not trimmed, not validated against the referentiel beyond the existence check below).
- `competenceSimulee`: echoed verbatim from the raw `competence` query value (**not** trimmed in the echo, even though the internal matching logic trims it).
- `niveauSimule`: echoed verbatim. **Per an inline code comment, this value does not actually affect the score calculation** — `scoreAvant`/`scoreApres` are a binary "does the candidate have this named competency at all" coverage check, not level-weighted; `niveauSimule` is documented as "purely informational".
- `scoreAvant`/`scoreApres`: `int` (0–100) — computed by the private helper `CalculerScoreMatch(competencesRequises, nomsCandidat)`: if `competencesRequises` is non-empty, `round(100 × possédées / total)`; if the referentiel for `posteCible` were empty the whole request would already have 404'd (see below), so the `competencesRequises.Count == 0` branch inside `CalculerScoreMatch` (`min(100, communes*25)`) is dead code for this endpoint specifically.
- `gain`: `int`, `scoreApres - scoreAvant` — **can be negative** in principle (though in practice adding a competency can't reduce coverage, so it is effectively always `>= 0` here).
- `impact`: `string`, exactly one of four fixed values: `"Impact significatif — accélère fortement la succession"` (`gain >= 20`), `"Impact modéré"` (`gain >= 5`), `"Aucun impact — compétence déjà couverte ou non requise"` (`gain <= 0`), `"Impact limité — d'autres compétences manquent davantage"` (`0 < gain < 5`).
- `recommandation`: `string`, exactly one of two fixed patterns: `"Prioriser cette formation : elle seule rapproche {prenom} du seuil de succession."` (`gain >= 20`) or the fixed literal `"Considérer cette formation en complément d'autres axes de développement."` (otherwise).

**Error shapes**:
- Any of the four params missing entirely → automatic `400 ValidationProblemDetails` (can list multiple missing fields at once if several are omitted together).
- `collaborateurId` not found → `404 { "error": "Collaborateur introuvable." }`.
- `posteCible` has no rows in `CompetenceRequiseParPoste` → `404 { "error": "Aucune compétence requise définie pour le poste '{posteCible}'." }`.
- No validation on `competence` (any string is accepted, even one that doesn't correspond to a real `Competence` row anywhere) and none on `niveauSimule` beyond being a valid `int`.

**Backend source**: Private static helper `ChatbotController.CalculerScoreMatch` (declared at the top of the controller, lines 60–76). **Note**: an inline code comment above this helper (`"Utilisé par GetSuccessionData ET SimulateFormation"`) claims it is shared with `GetSuccessionData`, but the current `GetSuccessionData` implementation calls `SuccessionEngine.Score` instead — the comment is stale relative to the code; `CalculerScoreMatch` is in fact only called from within `SimulateFormation` itself (twice: before/after).

---

## GET /api/chatbot/certifications-expirantes?jours=

**Params**: `jours`: `int` (optional, query, **default `90`** — has a default value in the method signature, so never triggers a "required" error; a non-numeric value still fails binding)

**Response 200 shape**:
```json
{
  "periode": "Dans les 90 prochains jours",
  "total": 2,
  "certifications": [
    {
      "collaborateurId": 34,
      "collaborateur": "Aziz Belhadj",
      "poste": "Consultant",
      "grade": "Junior",
      "certification": "PL-900",
      "organisme": "Microsoft",
      "domaine": "Power Platform",
      "dateExpiration": "2026-08-01",
      "joursRestants": 26,
      "urgence": "Critique"
    }
  ]
}
```
- Filter: `Statut == "Active"` **and** `DateExpiration` between today and `today + jours` (inclusive both ends) — already-expired or non-Active-status certifications never appear regardless of `jours`.
- `collaborateur`: `string` — `"?"` fallback literal if the `Collaborateur` navigation is somehow null (should not occur given the `Include`).
- `poste`/`grade`/`certification`/`organisme`/`domaine`: all `string?`.
- `dateExpiration`: `string`, `"yyyy-MM-dd"`.
- `joursRestants`: `int` — can be `0` (expires today) up to `jours`.
- `urgence`: `string`, one of exactly three values: `"Critique"` (`joursRestants <= 30`), `"Urgent"` (`<= 60`), `"À planifier"` (`<= 90`/otherwise within the window).
- **Side effect (not in the response body)**: for every item with `urgence == "Critique"` (≤30 days), fires `IPowerAutomateService.NotifyCertificationExpirationAsync` once per certification; failures are only logged, never surfaced in the response.

**Error shapes**: none beyond a possible automatic `400` if `jours` is supplied as a non-integer string (e.g. `?jours=abc`). No 404 path — an empty result set returns `200` with `"total": 0, "certifications": []`.

**Backend source**: Inline in `ChatbotController.GetCertificationsExpirantes`; side-effect call to `Services\PowerAutomateService.cs` → `CertificationExpirationNotification.Create(...)` (`Services\PowerAutomate\PowerAutomateDtos.cs`).

---

## GET /api/chatbot/ai/talent-summary

**Maximum-effort section per instructions.**

**Params**: none.

**Response 200 shape**:
```json
{
  "totalCollaborateurs": 30,
  "successionReady": 14,
  "topTalents": [
    { "id": 12, "nom": "Sami Trabelsi", "grade": "Partner", "departement": "Assurance", "score": 4.7 }
  ],
  "atRisk": [
    { "id": 55, "nom": "Nadia Ben Hassen", "score": 1.5 }
  ],
  "departmentDistribution": [
    { "departement": "Assurance", "total": 9 }
  ]
}
```
- `totalCollaborateurs`: `int` — count of **all active** collaborators (`Actif == true`), not filtered further.
- `successionReady`: `int` — count where `Grade == "Senior"` **or** `Grade == "Manager"` (exact string equality, case-sensitive, no `OrdinalIgnoreCase`) — this is a crude proxy, not a call into `SuccessionEngine`/succession-plan data at all.
- `topTalents`: **note the field set differs from `atRisk`** — `topTalents` items have `id`, `nom`, `grade`, `departement`, `score`; `atRisk` items have only `id`, `nom`, `score` (no `grade`/`departement`). Both use `Math.Round(avg(NiveauActuel), 1)` for `score`, defaulting to `0` if the collaborator has no competencies at all.
- `topTalents`: filtered to `score >= 4`, ordered descending, capped at 10.
- `atRisk`: filtered to `0 < score < 2` (a collaborator with **zero** competencies, `score == 0`, is explicitly **excluded** from `atRisk` despite having the lowest possible score — the `> 0` guard excludes them) — **not capped**, can return more than 10 items.
- `departmentDistribution`: **every** active collaborator's department (including those already counted in `topTalents`/`atRisk`), grouped and counted, ordered descending by `total`; `departement`: `string?` (can appear as a group with `"departement": null` if some collaborators have a null `Departement`).

**Error shapes**: none — always `200`, worst case all arrays empty (e.g. `totalCollaborateurs: 0` if there are no active collaborators, all others `[]`).

**Backend source**: Inline in `ChatbotController.GetTalentSummary`. No engine/service (does not call `TalentScoringEngine` or `SuccessionEngine` despite the "talent"/"succession" naming).

---

## GET /api/chatbot/postes-a-risque

**Maximum-effort section per instructions.**

**Params**: none.

**Response 200 shape**:
```json
{
  "total": 4,
  "collaborateurs": [
    {
      "id": 55,
      "nom": "Nadia Ben Hassen",
      "poste": "Senior Data Analyst",
      "grade": "Senior",
      "departement": "Consulting",
      "scoreActuel": 2.1,
      "seuilAttendu": 3.0,
      "anciennete": 3.4,
      "niveauRisque": "Élevé",
      "ecart": 0.9
    }
  ]
}
```
- `grade`: `string` — `c.Grade ?? "Junior"` (never null in the item; note this differs from the top-level pattern elsewhere where `Grade` is passed through raw-nullable).
- `scoreActuel`: `double`, rounded to 1 decimal — average `NiveauActuel` across all the collaborator's competencies, or `0` if none.
- `seuilAttendu`: `double` — looked up from a **hardcoded in-controller dictionary** (`seuilsParGrade`, case-insensitive keys): `Junior=2.0`, `Senior=3.0`, `Manager=3.5`, `Senior Manager=4.0`, `Director=4.0`, `Partner=4.5`; **default `3.0`** for any grade string not in this dictionary (this is a separate hardcoded table from `GradeReferentiel.NiveauMinCompetences`, not sourced from the database).
- `anciennete`: `double`, rounded to 1 decimal (years, computed from `DateEmbauche` using `DateTime.Now`, not `DateTime.Today` — differs subtly from other endpoints that use `.Today`).
- `niveauRisque`: `string`, one of exactly three non-null values (a fourth, implicit `null` branch is filtered out before the response is built, so it **never** appears in the JSON): `"Élevé"` (`scoreActuel < seuilAttendu && anciennete > 2`), `"Moyen"` (`scoreActuel < seuilAttendu` and `anciennete <= 2`), `"Faible"` (`scoreActuel >= seuilAttendu` **and** `anciennete < 1`). A collaborator meeting/exceeding their threshold with `anciennete >= 1` is entirely excluded from the array (implicit `null` case, filtered by `.Where(x => x.niveauRisque != null)`).
- `ecart`: `double`, rounded to 1 decimal — `seuilAttendu - scoreActuel` (can be **negative** for a `"Faible"`-risk item, since that branch only requires `anciennete < 1` and doesn't require `scoreActuel < seuilAttendu`).
- Sort order: `niveauRisque` severity descending (`Élevé` > `Moyen` > `Faible`), then `ecart` descending within the same severity; capped at 10.

**Error shapes**: none — always `200`; empty array if no active collaborator falls into any of the three risk buckets.

**Backend source**: Entirely inline in `ChatbotController.GetPostesARisque`, including the hardcoded per-grade threshold dictionary. No engine/service, no read of `GradeReferentiel`.

---

## GET /api/chatbot/postes-sans-successeur

**Params**: none.

**Response 200 shape**:
```json
{
  "total": 2,
  "collaborateurs": [
    {
      "id": 12,
      "nom": "Sami Trabelsi",
      "poste": "Partner",
      "grade": "Partner",
      "departement": "Assurance",
      "nbCompetencesRequises": 6
    }
  ]
}
```
- A collaborator appears here only if: they have ≥1 competency at all (`competencesRequises.Any()`, using **their own** competency names as the "requirement" list — this endpoint does **not** read `CompetenceRequiseParPoste`), and **no other** active collaborator reaches a ≥50% overlap score against that list (`candidats` empty after the `score >= 50` filter).
- `nbCompetencesRequises`: `int` — count of the collaborator's own distinct competency names (used as the improvised "requirements").
- Sort: by `grade` ascending via a `dynamic` cast (`((dynamic)x).grade`) — alphabetic string sort on the grade name, **not** a hierarchical Junior→Partner ordering; capped at 10.

**Error shapes**: none — always `200`; `total: 0` if every position has a plausible internal successor (≥50% skill overlap) or if no active collaborator has any competencies recorded at all.

**Backend source**: Inline in `ChatbotController.GetPostesSansSuccesseur`. No engine/service — deliberately does not use `SuccessionEngine`/`CompetenceRequiseParPoste`, unlike `succession/{id}`.

---

## GET /api/chatbot/simulate-departs-by-name?noms=

**Params**: `noms`: `string` (required, `[FromQuery]` explicit, comma-separated names, no default ⇒ implicitly required)

**Response 200 shape**: identical to `simulate-departs` (below) **plus** an extra top-level key `nomsIntrouvables: string[]` — **but only when at least one name failed to resolve** (the key is injected via a runtime JSON round-trip: `JsonSerializer.Serialize` the `simulate-departs` result, `Deserialize` into a `Dictionary<string, object>`, add the key, `Ok(dict)` again). If every name resolves, the response is the plain `simulate-departs` shape with **no** `nomsIntrouvables` key at all (not even an empty array).

**Error shapes**:
- `noms` missing entirely → automatic `400 ValidationProblemDetails`.
- `noms` present but blank/whitespace → `400 { "error": "Paramètre noms requis, ex: ?noms=Karim,Sami" }`.
- **None** of the comma-separated names resolve to a real collaborator → `404 { "error": "Aucun des collaborateurs mentionnés n'a été trouvé.", "nomsIntrouvables": ["Foo", "Bar"] }` — note this 404 shape has **two** keys, `error` and `nomsIntrouvables`, unlike the single-key `{ "error": "..." }` pattern used elsewhere.
- If the underlying `SimulateDeparts` call itself returns a non-`200` (e.g. its own internal `NotFound`), that result is passed through **unmodified** — `nomsIntrouvables` is only merged in when the wrapped call succeeded (`OkObjectResult`).

**Backend source**: Resolves each name via the same inline `FindCollaborateur`-style query, then makes a **direct in-process C# method call** to `ChatbotController.SimulateDeparts(idsString)` (not an HTTP call) and post-processes its `OkObjectResult`.

---

## GET /api/chatbot/stats

**Params**: none.

**Response 200 shape**:
```json
{
  "collaborateursActifs": 30,
  "formationsEnCours": 12,
  "totalInscriptions": 45,
  "terminees": 33,
  "tauxCompletion": 73.3,
  "repartitionDept": [
    { "departement": "Assurance", "total": 9 }
  ],
  "topCompetences": [
    { "nom": "Leadership stratégique", "nbCollaborateurs": 8, "niveauMoyen": 3.9 }
  ]
}
```
- `tauxCompletion`: `double`, rounded to 1 decimal; `0.0` (not `0`) if `totalInscriptions == 0`.
- `repartitionDept`: `departement`: `string?` (the query already excludes `null` departments via `Where(c.Departement != null)`, so in practice never `null` here — but the field itself is nullable-typed).
- `topCompetences`: top 5 by `nbCollaborateurs` descending; `niveauMoyen`: `double`, rounded to 1 decimal, computed only over competencies belonging to **active** collaborators (`c.Collaborateur.Actif`).

**Error shapes**: none — always `200`.

**Backend source**: Inline EF aggregation in `ChatbotController.GetRhStats`. No engine/service.

---

## GET /api/chatbot/hr-copilot-data

**Params**: none.

**Response 200 shape**:
```json
{
  "totalTalents": 30,
  "topTalents": [
    { "nom": "Sami Trabelsi", "poste": "Partner", "id": 12, "grade": "Partner", "score": 4.7 }
  ],
  "promotionReady": [
    { "nom": "Sami Trabelsi", "poste": "Partner", "id": 12, "grade": "Partner", "score": 4.7 }
  ],
  "atRisk": []
}
```
- `topTalents`: filtered `score >= 4`, ordered descending, capped at 10; field order in source is `Nom, Poste, id, Grade, Score` (irrelevant for JSON objects, but note `poste` **is** present here unlike `ai/talent-summary`'s `atRisk`).
- `promotionReady`: **always exactly `topTalents.Take(5)`** — not an independently computed "promotion readiness" list, just the first 5 entries of the same top-talents array.
- `atRisk`: **hardcoded to always be an empty array** (`new List<object>()`) — this field is a static placeholder in the current code, never populated regardless of actual data.

**Error shapes**: none — always `200`.

**Backend source**: Inline in `ChatbotController.GetHrCopilotData`. No engine/service.

---

## GET /api/chatbot/kpi-crm

**Params**: none.

**Response 200 shape**:
```json
{
  "totalCollaborateurs": 30,
  "tauxCertificationCRM": 23.3,
  "nombreCollabsCertifiesCRM": 7,
  "moyenneImplementations": 2.4,
  "distributionModeDeploiement": [
    { "mode": "Cloud", "count": 18 }
  ],
  "couvertureRolesCRM": [
    { "role": "Architect CRM", "eligible": 2, "couverturePct": 6.7 }
  ],
  "niveauxCompetencesCRMCles": [
    { "competence": "Microsoft Dynamics 365 CRM", "nCollaborateurs": 9, "niveauMoyen": 3.2 }
  ]
}
```
- `rolesCRM` (used to build `couvertureRolesCRM`) is a **hardcoded array** of exactly 7 role names: `"Technical Consultant CRM"`, `"Functional Consultant CRM"`, `"Techno Functional CRM"`, `"Business Analyst CRM"`, `"Quality Analyst CRM"`, `"Project Manager CRM"`, `"Architect CRM"` — `couvertureRolesCRM` always has exactly 7 entries in this fixed order, regardless of what's actually in `CompetenceRequiseParPoste`.
- `competencesCRMCles` is similarly hardcoded to exactly 5 names: `"Microsoft Dynamics 365 CRM"`, `"Power Platform"`, `"Intégration ERP/CRM"`, `"D365 Sales & Customer Service"`, `"Architecture solution"` — `niveauxCompetencesCRMCles` always has exactly 5 entries.
- `tauxCertificationCRM`: `double`, `0.0` if `totalCollabs == 0`; based on certifications whose `Certification.Domaine` **contains** `"CRM"` (substring, case-insensitive), not an exact domain match.
- `distributionModeDeploiement`: `mode`: `string` — `ModeDeploiement` enum `.ToString()` value (`"OnPremise"`/`"Cloud"`/`"Hybride"`); collaborators with `ModeDeploiement == null` are excluded from this grouping entirely (not shown as a `null`-keyed group).
- `couvertureRolesCRM[].eligible`: `int` — count of active collaborators with ≥50% name-overlap against that role's `CompetenceRequiseParPoste` rows (a different, coarser check than `staffing-crm`'s per-competency level-aware scoring). `couverturePct`: `double`, `0.0` if `totalCollabs == 0`.
- `niveauxCompetencesCRMCles[].niveauMoyen`: `double`, `0.0` if nobody has that exact competency name recorded.

**Error shapes**: none — always `200`.

**Backend source**: Inline in `ChatbotController.GetKpiCRM`, including the two hardcoded reference lists (`rolesCRM`, `competencesCRMCles`) local to this method. No engine/service.

---

## GET /api/chatbot/staffing-crm?role=

**Params**: `role`: `string` (required, `[FromQuery]` explicit, no default ⇒ implicitly required)

**Response 200 shape**:
```json
{
  "role": "Architect CRM",
  "competencesRequises": ["Architecture solution", "Power Platform"],
  "totalCandidats": 3,
  "top": [
    {
      "id": 41,
      "nom": "Hatem Gharbi",
      "poste": "Partner",
      "grade": "Partner",
      "departement": "Assurance",
      "scoreTotal": 84.5,
      "scoreCompetences": 100.0,
      "scoreCertifications": 50.0,
      "nombreImplementations": 5,
      "nombreCertifications": 2,
      "competencesCouvertes": 2,
      "competencesRequises": 2,
      "readiness": "Prêt"
    }
  ]
}
```
- **Note**: `competencesRequises` appears **twice** with different shapes — once at the top level as `string[]` (the raw list of required competency names for `role`), and once per-candidate inside `top[]` as an `int` (the count of requirements for that role, always equal for every candidate since it's the same role).
- `scoreTotal`: `double`, rounded to 1 decimal — `55%×scoreCompetences + 25%×scoreCertifications + 20%×scoreImpl` (a distinct weighting from `SuccessionEngine`'s 60/15/15/10 and from `PromotionReadinessEngine`'s 40/25/20/15).
- `scoreCompetences`: `double` — `%` of required competencies where `gap == 0` (`NiveauActuel >= NiveauRequis` for that competency); `0` if the role has 0 requirements (guarded, though `staffing-crm` already 404s before this if `competencesRequises` is empty at the referentiel level).
- `scoreCertifications`: `double`, rounded to 1 decimal — `min(100, activeCertCount × 25)`; "active" = `Statut == "Active"` and (`DateExpiration == null` or in the future).
- `scoreImpl`: (not directly exposed, only via `scoreTotal`) — `min(100, (NombreImplementations ?? 0) × 20)`.
- `nombreImplementations`: `int` — `c.NombreImplementations ?? 0` (never `null`).
- `readiness`: `string`, one of exactly four values based on `scoreTotal`: `"Prêt"` (`>= 80`), `"Quasi-prêt"` (`>= 60`), `"En développement"` (`>= 40`), `"Non éligible"` (`< 40`).
- `top`: candidates with `scoreTotal > 0` only, ordered descending, capped at 8 (**not** 10, unlike most other list endpoints in this controller).
- `totalCandidats`: `int` — the count of `top` (i.e., already capped at 8), **not** the total pool size before the cap/filter.

**Error shapes**:
- `role` missing entirely → automatic `400 ValidationProblemDetails`.
- `role` present but blank → `400 { "error": "Paramètre role requis, ex: ?role=Architect CRM" }`.
- `role` not found in `CompetenceRequiseParPoste` → `404 { "error": "Rôle '{role}' inconnu dans le référentiel CRM." }`.

**Backend source**: Entirely inline in `ChatbotController.GetStaffingCRM`. No engine/service (a fourth independent scoring formula in this controller, alongside `GetPromotion`, `GetPromotables`, and the 60/15/15/10 `SuccessionEngine`).

---

## GET /api/chatbot/hr-talent

**Params**: none.

**Response 200 shape** — **bare JSON array, no wrapper object** (unlike almost every other endpoint in this controller):
```json
[
  { "nom": "Sami Trabelsi", "grade": "Partner", "departement": "Assurance", "moyenneCompetences": 4.7 }
]
```
- Filtered to `moyenneCompetences >= 4`, ordered descending, capped at 10.
- `moyenneCompetences`: `double`, rounded to 1 decimal; `0` if the collaborator has no competencies (as a plain `0`, which serializes as the JSON number `0`, not `0.0` — `System.Text.Json` renders integral doubles without a decimal point).

**Error shapes**: none — always `200`; `[]` if nobody qualifies.

**Backend source**: Inline in `ChatbotController.GetHighPotentials`. No engine/service.

---

## GET /api/chatbot/simulate-departs?ids=

**Params**: `ids`: `string` (required, `[FromQuery]` explicit, comma-separated integer IDs, no default ⇒ implicitly required)

**Response 200 shape**:
```json
{
  "scenario": "Départ simultané de 2 collaborateur(s)",
  "collaborateursAnalyses": [
    { "id": 12, "nom": "Sami Trabelsi", "departement": "Assurance", "poste": "Partner" }
  ],
  "impactParDepartement": [
    {
      "departement": "Assurance",
      "collaborateursPartants": ["Sami Trabelsi"],
      "effectifRestant": 8,
      "competencesRequises": 4,
      "competencesPerdues": ["Business Development"],
      "niveauAlerte": "Risque modéré"
    }
  ]
}
```
- `scenario`: `string`, interpolated with the **count of departing collaborators actually found in the DB** (not the count of IDs supplied — if some IDs don't resolve, this number can be smaller than the number of comma-separated values in `ids`).
- `collaborateursAnalyses`: one entry per resolved departing collaborator; `poste`/`departement`: `string?`.
- `impactParDepartement`: one entry per **distinct department among the departing collaborators** (not all departments in the company).
  - `competencesRequises` (per-department item): `int` — count of **distinct** competency names required across `CompetenceRequiseParPoste` for the departing collaborators' postes in that department (**note**: same field name as the top-level array-of-names field used in `staffing-crm`, but here it's a **count**, not a list).
  - `competencesPerdues`: `string[]` — required competency names not covered by anyone remaining active in that department (case-insensitive comparison against remaining staff's competency names).
  - `niveauAlerte`: `string`, one of exactly three values: `"Aucun risque"` (`competencesPerdues.Count == 0`), `"Risque modéré"` (`<= 2`), `"Risque critique"` (`> 2`).

**Error shapes**:
- `ids` missing entirely → automatic `400 ValidationProblemDetails`.
- `ids` present but blank/whitespace → `400 { "error": "Paramètre ids requis, ex: ?ids=1,3,5" }`.
- `ids` present but contains no valid positive integers after parsing (e.g. `?ids=abc` or `?ids=0,-1`) → `400 { "error": "Aucun ID valide fourni." }`.
- None of the parsed IDs match an existing collaborator → `404 { "error": "Aucun collaborateur trouvé pour ces IDs." }`.
- IDs that parse but don't match anyone are **silently dropped** from `idsDepart`/`partants` without individual reporting (contrast with `simulate-departs-by-name`, which does report unresolved entries via `nomsIntrouvables`).

**Backend source**: Inline in `ChatbotController.SimulateDeparts`. No engine/service.

---

## POST /api/chatbot/ask

**Params** (JSON body, `ChatRequest`):
| name | type | required | default | source |
|---|---|---|---|---|
| `message` | `string` | required (validated in-body, not by NRT-inference since the property has an initializer `= ""`) | `""` | body |
| `page` | `string?` | optional | `null` → coerced to `"general"` before forwarding | body |
| `contextId` | `string?` | optional | `null` | body |
| `context` | `JsonElement?` | optional | `null` | body |
| `sessionMemory` | `JsonElement?` | optional | `null` | body |
| `sessionHistory` | `JsonElement?` | optional | `null` | body |

Body property names as received: exactly `message`, `page`, `contextId`, `context`, `sessionMemory`, `sessionHistory` (camelCase, matching the C# property names' policy-transformed form; the `Context` property exists on `ChatRequest` but is **not read anywhere** in the action body — only `SessionMemory` is forwarded, duplicated into both the `sessionMemory` and `context` keys of the outbound n8n payload).

**Response 200 shape**: **this endpoint does not construct its own JSON** — on success it forwards the **raw response body received from the n8n webhook** (`http://localhost:5678/webhook/hr-copilot`) byte-for-byte via `Content(body, "application/json", ...)`, after only checking that it parses as valid JSON (`JsonSerializer.Deserialize<JsonElement>(body)` — a syntax check only, not a shape/schema check). The n8n workflow's own reply shape (`answer`, `analysis`, `reasoning`, `actions`, `suggestions`, `sources`, `cards`, `executionHistory`, `context`) is documented in `PROJECT_KNOWLEDGE.md` §9/§10 from the `n8n\*.json` workflow exports — this controller does not enforce or know that shape.

**Outbound payload actually sent to n8n** (for reference — not the HTTP response of this endpoint):
```json
{
  "message": "string",
  "page": "string (defaults to \"general\")",
  "contextId": "string|null",
  "sessionMemory": "JsonElement|null (passthrough)",
  "sessionHistory": "JsonElement|null (passthrough)",
  "context": "same value as sessionMemory (bridge — n8n reads body.context)",
  "role": "\"HR\" | \"Manager\" | \"Employee\" (resolved server-side, never trusts the client)",
  "selfCollaborateurId": "int|null"
}
```

**Error shapes**:
- `request == null` or `request.Message` blank/whitespace → `400` with a **plain JSON string body** (not an object): `"Message cannot be empty."` — because `BadRequest(string)` serializes the string itself as the JSON payload (`Content-Type: application/json`, body literally `"Message cannot be empty."` including the quotes).
- Malformed JSON body (fails to deserialize into `ChatRequest` at all) → automatic ASP.NET Core `400` (framework-level, before the action runs).
- n8n webhook responds with a non-success HTTP status → this endpoint returns **that same status code** (`StatusCode((int)response.StatusCode, ...)`) with a `BuildFallback("Erreur de connexion avec le service IA.")` body (see shape below) — i.e. the status code is passed through but the body is always the fallback shape, never n8n's actual error body.
- n8n responds `200` but with a body that isn't valid JSON → `200` (not an error status) with a `BuildFallback(body)` wrapper, where the raw non-JSON text is placed into the `answer` field.
- Any exception during the HTTP call (timeout, connection refused, DNS failure, etc.) → `500` with `BuildFallback("Service IA temporairement indisponible. Veuillez réessayer dans quelques instants.")`.

**`BuildFallback(message)` shape** (used for the three non-passthrough cases above):
```json
{
  "answer": "string (the message passed in)",
  "analysis": null,
  "reasoning": [],
  "actions": [],
  "suggestions": [
    "Quels sont les hauts potentiels ?",
    "Qui est prêt pour une promotion ?",
    "Répartition des effectifs ?"
  ],
  "sources": [],
  "cards": [],
  "executionHistory": [],
  "context": {}
}
```

**Backend source**: HTTP POST via `IHttpClientFactory` to the hardcoded n8n webhook URL `http://localhost:5678/webhook/hr-copilot` (not read from `appsettings.json`, unlike the Power Automate flow URLs). Role resolution via `ITeamAccessService.IsPrivileged`/`GetCurrentCollaborateurIdAsync` (`Services\TeamAccessService.cs`). No local scoring engine — this endpoint is a pass-through proxy to the external n8n workflow.
