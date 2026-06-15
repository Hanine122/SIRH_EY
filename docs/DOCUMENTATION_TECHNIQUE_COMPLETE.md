# DOCUMENTATION TECHNIQUE COMPLÈTE — SIRH EY
## Système d'Information des Ressources Humaines — Ernst & Young Tunisie

> **Version :** 1.0 — Générée le 15 juin 2026  
> **Usage :** Mémoire projet pour Claude Browser / tout agent IA  
> **Objectif :** Permettre à une IA de devenir experte du projet sans relire le code

---

## TABLE DES MATIÈRES

1. [Vision du Projet](#1-vision-du-projet)
2. [Architecture Technique](#2-architecture-technique)
3. [Base de Données](#3-base-de-données)
4. [APIs REST — Catalogue Complet](#4-apis-rest--catalogue-complet)
5. [Talent Management](#5-talent-management)
6. [Module Formation](#6-module-formation)
7. [Chatbot RH IA](#7-chatbot-rh-ia)
8. [Workflows n8n](#8-workflows-n8n)
9. [Fonctionnalités Terminées](#9-fonctionnalités-terminées)
10. [Fonctionnalités Futures](#10-fonctionnalités-futures)
11. [Cartographie Complète](#11-cartographie-complète)

---

## 1. VISION DU PROJET

### 1.1 Objectif Métier

Le SIRH EY est une application web de gestion des ressources humaines destinée au bureau tunisien d'Ernst & Young (cabinet international d'audit, conseil, fiscalité et transactions). Elle centralise la gestion des collaborateurs, de leurs compétences, de leurs formations et de leur carrière, tout en offrant des fonctionnalités avancées d'analyse prédictive RH (talent management, succession planning, promotion readiness, workforce impact).

**Contexte :** EY Tunisie emploie des profils allant du Consultant Junior au Partner, répartis dans des départements tels que Assurance, Consulting, Tax, Strategy & Transactions, CBS (Corporate Business Services).

### 1.2 Périmètre Fonctionnel

| Domaine | Statut |
|---|---|
| Gestion des collaborateurs (CRUD + hiérarchie) | ✅ Opérationnel |
| Gestion des compétences (évaluation à double signature) | ✅ Opérationnel |
| Catalogue de formations (inscriptions, progression, certificats) | ✅ Opérationnel |
| Talent Management (9-Box, hauts potentiels, OKRs) | ✅ Opérationnel |
| Succession Planning (ChoisirRemplacant, comparaison PDF) | ✅ Opérationnel |
| Analytics RH (RH Insights, KPIs, heatmaps compétences) | ✅ Opérationnel |
| Chatbot RH IA (via n8n + webhooks) | ✅ Opérationnel (n8n requis) |
| Authentification Identity (4 rôles) | ✅ Opérationnel |
| Génération PDF (certificats, comparaison) | ✅ Opérationnel |
| Intégration Flowise/IA générative | ⚠️ Placeholder (simulée) |
| Intégration Dataverse (Microsoft) | ⚠️ Conditionnelle (config) |

### 1.3 Modules Existants

1. **Collaborateurs** — Fiche RH complète, hiérarchie manager/équipe, départ/remplacement
2. **Compétences** — Catalogue EY par secteur, auto-évaluation + validation manager, matrice équipe
3. **Formations** — Catalogue enrichi (Udemy, Coursera, EY Learning), inscriptions, parcours, certificats PDF
4. **Talent Management** — Dashboard hauts potentiels, matrice 9-Box, OKRs
5. **Succession** — Identification des remplaçants, analyse des écarts, export PDF comparatif
6. **RH Insights** — KPIs exécutifs IA, alertes de continuité, simulateur promotion, simulateur workforce impact
7. **Chatbot RH** — Assistant IA intégré dans toutes les pages (widget flottant)
8. **Certifications** — Génération PDF de certificats de formation complétée
9. **Reporting** — Executive Dashboard

---

## 2. ARCHITECTURE TECHNIQUE

### 2.1 Stack Technologique

```
┌─────────────────────────────────────────────────────────┐
│                    SIRH EY — ASP.NET Core MVC           │
├──────────────┬──────────────┬───────────────────────────┤
│   Frontend   │   Backend    │      Infrastructure        │
│              │              │                            │
│ Razor Views  │ Controllers  │  SQL Server (EF Core)      │
│ Tailwind CSS │ Services     │  ASP.NET Identity          │
│ JavaScript   │ Repositories │  n8n (Automation)          │
│ Chart.js     │ EF Core      │  Flowise (IA - placeholder)│
│ Font Awesome │ LINQ queries │  QuestPDF (PDF gen)        │
└──────────────┴──────────────┴───────────────────────────┘
```

### 2.2 Framework & Technologies

- **Framework :** ASP.NET Core 8.0 MVC
- **ORM :** Entity Framework Core avec SQL Server
- **Authentification :** ASP.NET Identity (IdentityDbContext)
- **Frontend :** Razor Pages + Tailwind CSS + JavaScript vanilla
- **Graphiques :** Chart.js
- **PDF :** QuestPDF (CertificatFormationPdf, ComparaisonRemplacantsPdf)
- **Automation :** n8n (local, port 5678)
- **IA :** Flowise (placeholder simulé) + n8n webhooks
- **Email :** IEmailSender (SMTP configurable)
- **Cache :** IMemoryCache

### 2.3 Couches Applicatives

```
┌─────────────────────────────────┐
│         Presentation Layer       │
│   Razor Views (.cshtml)          │
│   JavaScript (fetch API)         │
│   Tailwind CSS                   │
├─────────────────────────────────┤
│         Controller Layer         │
│   MVC Controllers (11 total)     │
│   API Controllers (ChatbotCtrl)  │
│   Authorization Filters          │
├─────────────────────────────────┤
│         Service Layer            │
│   PromotionReadinessService      │
│   WorkforceImpactService         │
│   PlanDeveloppementService       │
│   ReferentielRhService           │
│   TeamAccessService              │
│   OwnershipService               │
│   UserContextService             │
│   FlowiseService (IA)            │
│   CompetenceCatalogService       │
├─────────────────────────────────┤
│         Data Layer               │
│   ApplicationDbContext (EF Core) │
│   SQL Server Database            │
│   Migrations (25 fichiers)       │
│   Seeders (3 seeders)            │
└─────────────────────────────────┘
```

### 2.4 Authentification & Autorisations

**4 rôles Identity :**

| Rôle | Accès | Utilisateurs seed |
|---|---|---|
| `ITAdmin` | Accès total | admin@ey.tn / Admin@123456 |
| `RH` | Gestion RH complète | rh@ey.tn / Rh@123456 |
| `Manager` | Vue équipe + évaluations | hanine.hammami@ey.com, Ahmed.benyoussef@ey.com, ibtissem.bessrour@ey.com |
| `Collaborateur` | Vue personnelle uniquement | Tous les autres collaborateurs |

**Politique globale :** Chaque requête doit être authentifiée (filter global `AuthorizeFilter`).

**Règle d'attribution des rôles (seed) :**
- Département "RH" → rôle `RH`
- Grade ou Poste contient "Manager" → rôle `Manager`
- Sinon → rôle `Collaborateur`

**Services d'autorisation :**
- `ITeamAccessService` — Scope des données (ITAdmin/RH = tout, Manager = équipe, Collaborateur = soi)
- `IOwnershipService` — Vérifie la propriété (OKR, compétence, inscription)

### 2.5 Injection de Dépendances (Program.cs)

```
Services enregistrés :
- IParametreService           → ParametreService (Scoped)
- IReferentielRhService       → ReferentielRhService (Scoped)
- IPlanDeveloppementService   → PlanDeveloppementService (Scoped)
- IPromotionReadinessService  → PromotionReadinessService (Scoped)
- IWorkforceImpactService     → WorkforceImpactService (Scoped)
- IUserContextService         → UserContextService (Scoped)
- ITeamAccessService          → TeamAccessService (Scoped)
- IOwnershipService           → OwnershipService (Scoped)
- IEmailSender                → EmailSender (Transient)
- FlowiseService              → FlowiseService (HttpClient)
- IDataverseService           → DataverseService (conditionnelle, Scoped)
- IMemoryCache
```

### 2.6 Routing

```
Route admin  : {area:exists}/{controller=AdminHome}/{action=Index}/{id?}
Route défaut : {controller=Home}/{action=Index}/{id?}
```

---

## 3. BASE DE DONNÉES

### Architecture Générale

La base de données est SQL Server gérée par EF Core avec 25 migrations. Elle contient 3 couches de tables :
- **Tables HR Core** : entités métier principales
- **Tables Master Data** : référentiels RH (départements, grades, postes...)
- **Tables ASP.NET Identity** : gestion des utilisateurs

### 3.1 Table : Collaborateurs

**Rôle métier :** Entité centrale — représente un employé EY.

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | Identifiant unique |
| `Nom` | string (required) | Nom de famille |
| `Prenom` | string (required) | Prénom |
| `UserId` | string (FK → AspNetUsers) | Lien vers le compte Identity |
| `Email` | string | Email EY (@ey.com) |
| `DateNaissance` | DateTime? | Date de naissance |
| `Genre` | string? | Femme / Homme / Non renseigné |
| `Nationalite` | string? | Nationalité |
| `EtatCivil` | string? | Célibataire/Marié(e)/... |
| `Adresse`, `Ville`, `Pays` | string? | Adresse personnelle |
| `TelephonePersonnel` | string? | Téléphone |
| `ContactUrgence` | string? | Contact d'urgence |
| `Matricule` | string? | Numéro matricule RH |
| `Grade` | string? | Junior/Senior/Manager/Director/Partner (legacy) |
| `Departement` | string? | Audit/Tax/Consulting/... (legacy string) |
| `Poste` | string? | Titre du poste (legacy string) |
| `BusinessUnit` | string? | BU string (legacy) |
| `Localisation` | string? | Lieu de travail (legacy) |
| `ManagerId` | int? (FK self-ref) | Manager direct |
| `DepartmentId` | int? (FK) | Référence département (master data) |
| `SubDepartmentId` | int? (FK) | Référence sous-département |
| `PositionId` | int? (FK) | Référence poste (master data) |
| `GradeId` | int? (FK) | Référence grade (master data) |
| `BusinessUnitId` | int? (FK) | Référence BU (master data) |
| `LocationId` | int? (FK) | Référence localisation |
| `ContractTypeId` | int? (FK) | Type de contrat |
| `TypeContrat` | string? | CDI/CDD/Stage/... (legacy) |
| `NiveauHierarchique` | string? | Junior/Senior/Manager/... |
| `DateEmbauche` | DateTime | Date d'embauche (default = now) |
| `DatePrisePoste` | DateTime? | Date de prise de poste actuelle |
| `FormationsObligatoires` | string? | Liste CSV des formations obligatoires |
| `NiveauPreparationSuccession` | int? | 1-5 |
| `PotentielCarriere` | string? | Emergent/Solide/Haut potentiel/Succession prioritaire |
| `Actif` | bool | true = employé actif |
| `Statut` | enum StatutCollaborateur | Actif/Vacant/EnPassation/Inactif |
| `Anciennete` | int (computed) | Années depuis DateEmbauche |

**Relations :**
- Collaborateur → User (1:1, FK UserId, SetNull on delete)
- Collaborateur → Manager (self-ref, ManagerId, Restrict on delete)
- Collaborateur → Competences (1:N)
- Collaborateur → Inscriptions (1:N)
- Collaborateur → TalentEvaluations (1:N)
- Collaborateur → OKRs (1:N)
- Collaborateur → Equipe (1:N via ManagerId inverse)

**Note architecturale :** Les champs string legacy (Grade, Departement, Poste, BusinessUnit, Localisation, TypeContrat) sont synchronisés automatiquement depuis les FK via `SyncLegacyStringFieldsAsync()` à chaque sauvegarde.

### 3.2 Table : Competences

**Rôle métier :** Compétence d'un collaborateur, évaluée sur une échelle 1-5.

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | |
| `Nom` | string | Nom de la compétence (ex: "Audit financier", "Azure Functions") |
| `CollaborateurId` | int (FK) | Propriétaire |
| `CategorieCompetenceId` | int? (FK) | Catégorie (Audit, Tech, Management, ...) |
| `NiveauActuel` | int | Niveau actuel 1-5 |
| `NiveauCible` | int | Niveau objectif selon le grade |
| `DateEvaluation` | DateTime | Date de la dernière évaluation |
| `EvaluationCompetence` | nav | 1:1 → détail évaluation à double signature |

**Navigation :** `EvaluationCompetence` (table EvaluationsCompetences) stocke l'auto-évaluation du collaborateur et la validation du manager.

### 3.3 Table : EvaluationCompetence

**Rôle métier :** Double signature sur une compétence (collaborateur + manager).

| Champ | Type | Description |
|---|---|---|
| `CompetenceId` | int (PK/FK) | |
| `SeuilRh` | int | Seuil attendu par RH (0-100) |
| `AutoEvaluationCollaborateur` | int | Auto-évaluation 0-100 |
| `CommentaireCollaborateur` | string? | Commentaire libre |
| `DateAutoEvaluation` | DateTime? | |
| `EvaluationManager` | int? | Note du manager 0-100 |
| `ValidationManager` | bool | Manager a validé ? |
| `CommentaireManager` | string? | |
| `DateValidationManager` | DateTime? | |

### 3.4 Table : Formations

**Rôle métier :** Catalogue de formations disponibles (internes + externes).

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | |
| `Titre` | string (required) | Titre de la formation |
| `Formateur` | string? | Nom du formateur |
| `DureeHeures` | int | Durée en heures |
| `CapaciteMax` | int | Capacité maximale (défaut 20) |
| `PlacesPrises` | int | Places déjà occupées |
| `Categorie` | string? | Catégorie thématique |
| `DateDebut` | DateTime | Date de début |
| `Organisme` | string? | Organisme formateur |
| `CompetenceVisee` | string? | Compétence développée par cette formation |
| `DepartementCible` | string? | Département cible |
| `MetierCible` | string? | Métier cible |
| `PosteCible` | string? | Poste cible |
| `DomaineCompetence` | string? | Domaine de compétence |
| `NiveauDifficulte` | string? | Fondamental/Intermédiaire/Avancé/Expert |
| `EstCertifiante` | bool | Formation certifiante ? |
| `Plateforme` | string? (max 100) | Udemy/Coursera/Microsoft Learn/EY Learning/LinkedIn Learning/AWS Skill Builder |
| `ExternalUrl` | string? (max 500) | Lien externe vers la plateforme |
| `Description` | string? | Description détaillée |
| `CompetencesRequises` | string? (max 500) | Prérequis (CSV) |
| `CertificationNom` | string? (max 200) | Nom de la certification obtenue |
| `SupportPdfUrl` | string? (max 500) | URL support PDF |
| `MentorEmail` | string? (max 200) | Email du mentor |
| `EstStrategique` | bool | Formation stratégique EY |
| `EstForteDemande` | bool | Formation très demandée |

### 3.5 Table : Inscriptions

**Rôle métier :** Inscription d'un collaborateur à une formation (suivi de progression).

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | |
| `CollaborateurId` | int (FK) | |
| `FormationId` | int (FK) | |
| `DateInscription` | DateTime | Date d'inscription |
| `Terminee` | bool | Formation terminée ? |
| `DateCompletion` | DateTime? | Date de complétion |
| `DateFinReelle` | DateTime? | Date de fin réelle |
| `Progression` | int | % de progression (0-100) |
| `DateExamen` | DateTime? | Date d'examen planifiée |
| `SourceCertification` | string? | Source (ex: "EY Learning", "Udemy") |
| `EvaluationsFormation` | nav | Collection EvaluationCompetence liées |

**Règle métier :** Quand une formation est terminée (`TerminerFormation`), la compétence visée (`CompetenceVisee`) est automatiquement créée ou incrémentée sur le profil du collaborateur.

### 3.6 Table : TalentEvaluations

**Rôle métier :** Évaluation manuelle pour la matrice 9-Box (Performance + Potentiel).

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | |
| `CollaborateurId` | int (FK) | |
| `PerformanceScore` | int (1-5) | Score de performance |
| `PotentielScore` | int (1-5) | Score de potentiel |
| `Category` | enum NineBoxCategory | Catégorie 9-Box calculée |
| `CommentairesPerformance` | string? | |
| `CommentairesPotentiel` | string? | |
| `EvaluateurId` | string? (FK) | ApplicationUser évaluateur |
| `DateEvaluation` | DateTime | |
| `Actif` | bool | Évaluation active ? (permet l'historique) |

**Enum NineBoxCategory (9 valeurs) :**
- `Star` (1) — Haute perf / Haut potentiel
- `FutureLeader` (2) — Haute perf / Potentiel moyen
- `HighProfessional` (3) — Haute perf / Bas potentiel
- `EmergingTalent` (4) — Perf moyenne / Haut potentiel
- `SolidProfessional` (5) — Perf moyenne / Potentiel moyen
- `InPlace` (6) — Perf moyenne / Bas potentiel
- `RisingStar` (7) — Basse perf / Haut potentiel
- `NeedDevelopment` (8) — Basse perf / Potentiel moyen
- `Underperformer` (9) — Basse perf / Bas potentiel

### 3.7 Table : OKRs

**Rôle métier :** Objectifs et Résultats Clés d'un collaborateur (gestion par trimestre).

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | |
| `CollaborateurId` | int (FK) | |
| `Objectif` | string (max 200) | Objectif principal |
| `Description` | string? | Description détaillée |
| `Annee` | int | Année (ex: 2026) |
| `Trimestre` | enum Trimestre | Q1/Q2/Q3/Q4 |
| `Statut` | enum OKRStatut | Draft/Active/OnTrack/AtRisk/Completed/Cancelled |
| `ProgressionGlobale` | int (0-100) | Calculée depuis les KeyResults |
| `DateDebut` | DateTime | |
| `DateFinCible` | DateTime | |
| `ManagerId` | string? (FK) | Manager validateur |
| `ValideParManager` | bool | |
| `DateValidation` | DateTime? | |
| `KeyResults` | nav | Collection de KeyResult |

### 3.8 Table : KeyResults

**Rôle métier :** Résultat clé mesurable associé à un OKR.

| Champ | Type | Description |
|---|---|---|
| `ValeurCible` | double | Valeur à atteindre |
| `ValeurActuelle` | double | Valeur courante |
| `Unite` | string? | Unité (%, nombre, etc.) |
| `Progression` | int (computed) | (ValeurActuelle/ValeurCible) × 100 |
| `Difficulte` | enum KeyResultDifficulty | Easy/Medium/Hard/Stretch |
| `Statut` | enum KeyResultStatut | NotStarted/InProgress/AtRisk/Completed/Cancelled |

### 3.9 Tables de Référentiel (Master Data)

**Rôle métier :** Référentiels RH utilisés pour les dropdowns et la normalisation.

| Table | Champs principaux | Exemples de données |
|---|---|---|
| `Departments` | Name, Code, IsActive | Assurance (ASS), Consulting (CON), Tax (TAX), CBS, Risk Management... |
| `SubDepartments` | Name, DepartmentId, IsActive | Audit Financier, Risk Advisory, Business Consulting... |
| `Positions` | Name, SubDepartmentId, IsActive | Consultant, Senior Consultant, Manager, Auditeur, HR Director... |
| `GradeEntities` | Name, Level, IsActive | Junior(1), Senior(2), Manager(3), Senior Manager(4), Director(5), Partner(6) |
| `BusinessUnits` | Name, Code, IsActive | Assurance, Consulting, SaT, Tax, CBS |
| `Locations` | Name, City, Country, IsActive | Tunis Lac 1, Tunis Lac 2, Sfax, Remote, Hybride |
| `ContractTypes` | Name, Code, MaxDurationMonths, IsActive | CDI, CDD(12m), Stage(6m), Alternance(24m), Freelance, Consultant externe |
| `SystemParameters` | Key, Value, Category, IsEditable | HR.MaxFormationsParAn=5, HR.SeuilCompetenceCible=3, HR.AncienneteMin=2 |

### 3.10 Table : PlansDeveloppement

**Rôle métier :** Plan de développement associant un collaborateur à une formation.

| Champ | Type | Description |
|---|---|---|
| `Id` | int (PK) | |
| `CollaborateurId` | int (FK) | |
| `FormationId` | int (FK) | |
| Date, statut, commentaire | ... | ... |

### 3.11 Table : CompetencesRequisesParPoste

**Rôle métier :** Compétences obligatoires pour un poste donné (référentiel de matching succession).

| Champ | Type | Description |
|---|---|---|
| `Poste` | string | Titre du poste |
| `Competence` | string | Nom de la compétence |
| `NiveauRequis` | int | Niveau requis (1-5) |

### 3.12 Table : FormationCompetences

**Rôle métier :** Table de jointure M:N entre formations et compétences (clé composite).

```
FormationId + CompetenceId → clé composite (HasKey)
```

### 3.13 Tables Position Relations

| Table | Rôle |
|---|---|
| `PositionRequiredCompetences` | Compétences requises pour une position référentielle |
| `PositionMandatoryFormations` | Formations obligatoires pour une position |
| `PositionGradeEligibilities` | Grades éligibles pour une position |

### 3.14 Tables Identity (ASP.NET)

```
AspNetUsers     — ApplicationUser (étend IdentityUser avec Nom, Prenom)
AspNetRoles     — Rôles (ITAdmin, RH, Manager, Collaborateur)
AspNetUserRoles — Association User ↔ Role
AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
```

### 3.15 Autres Tables

| Table | Rôle |
|---|---|
| `CategoriesCompetences` | Catégories de compétences (Audit, Tech, Management, Risk...) |
| `EvaluationsHistoriques` | Historique des évaluations de compétences |
| `Parametres` | Paramètres applicatifs clé/valeur (héritage, remplacée par SystemParameters) |

---

## 4. APIS REST — CATALOGUE COMPLET

### 4.1 ChatbotController — `/api/chatbot/`

> `[ApiController]`, Route = `api/[controller]`, tous les endpoints GET sont `[AllowAnonymous]`

---

**`GET /api/chatbot/stats`**

**Description :** Statistiques RH globales — endpoint principal appelé par n8n (hr-stats).

**Paramètres :** Aucun

**Réponse JSON :**
```json
{
  "collaborateursActifs": 30,
  "formationsEnCours": 12,
  "totalInscriptions": 45,
  "terminees": 22,
  "tauxCompletion": 48.9,
  "repartitionDept": [
    { "departement": "Consulting", "total": 8 },
    { "departement": "Tax", "total": 6 }
  ],
  "topCompetences": [
    { "nom": "Audit financier", "nbCollaborateurs": 5, "niveauMoyen": 3.8 }
  ]
}
```

**Use case métier :** Répondre à des questions générales sur les effectifs, les formations, les compétences.

---

**`GET /api/chatbot/hr-talent`**

**Description :** Liste des hauts potentiels (moyenne compétences ≥ 4).

**Paramètres :** Aucun

**Réponse JSON :**
```json
[
  { "Nom": "Sofien Klaou", "Grade": "Senior", "Departement": "Advisory", "MoyenneCompetences": 4.5 }
]
```

**Règle métier :** Seuil = `MoyenneCompetences >= 4.0`, Top 10, trié par moyenne décroissante.

---

**`GET /api/chatbot/ai/talent-summary`**

**Description :** Résumé talent complet : top talents + collaborateurs à risque + distribution.

**Réponse JSON :**
```json
{
  "totalCollaborateurs": 30,
  "successionReady": 8,
  "topTalents": [...],
  "atRisk": [...],
  "departmentDistribution": [...]
}
```

**Règle :** `atRisk` = moyenne > 0 et < 2.0. `successionReady` = Grade "Senior" ou "Manager".

---

**`GET /api/chatbot/hr-copilot-data`**

**Description :** Données pour le HR Copilot (talents + promotables).

**Réponse JSON :**
```json
{
  "totalTalents": 30,
  "topTalents": [...],
  "promotionReady": [...],
  "atRisk": []
}
```

---

**`GET /api/chatbot/promotables?dept={departement}`**

**Description :** Liste des collaborateurs candidats à la promotion.

**Paramètres :**
- `dept` (string, optionnel) — Filtre par département (recherche partielle, insensible à la casse)

**Réponse JSON :**
```json
{
  "total": 5,
  "collaborateurs": [
    { "id": 7, "nom": "Sofien Klaou", "poste": "Senior Consultant", "grade": "Senior", "departement": "Advisory", "score": 4.5 }
  ]
}
```

**Règle métier :** Score compétences ≥ 4.0, Top 10, trié par score décroissant.

---

**`GET /api/chatbot/postes-sans-successeur`**

**Description :** Postes critiques sans successeur identifié (matching compétences < 50%).

**Réponse JSON :**
```json
{
  "total": 3,
  "collaborateurs": [
    { "id": 5, "nom": "Ahmed Ben Youssef", "poste": "Audit Manager", "grade": "Manager", "departement": "Audit", "nbCompetencesRequises": 6 }
  ]
}
```

**Règle métier :** Pour chaque collaborateur, cherche des candidats ayant ≥ 50% de ses compétences. Si aucun candidat trouvé → poste "sans successeur".

---

**`GET /api/chatbot/postes-a-risque`**

**Description :** Collaborateurs dont le niveau de compétence est inférieur au seuil attendu pour leur grade.

**Paramètres :** Aucun

**Seuils par grade :**
```
Junior         → 2.0
Senior         → 3.0
Manager        → 3.5
Senior Manager → 4.0
Director       → 4.0
Partner        → 4.5
```

**Réponse JSON :**
```json
{
  "total": 4,
  "collaborateurs": [
    {
      "id": 3, "nom": "Raed Amri", "poste": "Consultant", "grade": "Junior",
      "departement": "Consulting", "scoreActuel": 1.5, "seuilAttendu": 2.0,
      "anciennete": 3.2, "niveauRisque": "Élevé", "ecart": 0.5
    }
  ]
}
```

**Règle :** Niveau risque = "Élevé" si score < seuil ET ancienneté > 2 ans ; "Moyen" si score < seuil ; "Faible" si ancienneté < 1 an. `niveauRisque = null` si pas à risque (exclu du résultat).

---

**`GET /api/chatbot/succession/{collaborateurId}`**

**Description :** TOP 3 meilleurs successeurs pour un collaborateur donné.

**Paramètres :**
- `collaborateurId` (int, path) — ID du collaborateur à remplacer

**Algorithme de matching :**
1. Collecte les compétences requises = union(compétences du profil + CompetencesRequisesParPoste pour ce poste)
2. Pour chaque autre collaborateur actif, calcule :
   - `communes` = compétences communes avec les requises
   - `scoreMatch` = communes / requises × 100
   - `profilTransversal` = vient d'un autre département avec compétences communes
3. Trie : d'abord profils transversaux, puis par nb compétences communes, puis par nb compétences manquantes

**Réponse JSON :**
```json
{
  "collaborateurNom": "Ahmed Ben Youssef",
  "poste": "Audit Manager",
  "competencesRequises": ["Audit financier", "IFRS", "Risk Assessment"],
  "top3": [
    {
      "id": 3, "nom": "Mariem Safri", "poste": "Senior Auditor", "grade": "Senior",
      "departement": "Audit", "scoreMatch": 67, "competencesCommunes": 2,
      "competencesManquantes": ["Leadership"], "profilTransversal": false
    }
  ]
}
```

---

**`GET /api/chatbot/collaborateur/{id}`**

**Description :** Profil complet d'un collaborateur avec ses compétences.

**Réponse :**
```json
{
  "id": 5, "nom": "Sofien Klaou", "poste": "Senior Consultant",
  "grade": "Senior", "departement": "Advisory",
  "competences": [
    { "Nom": "Stakeholder Management", "NiveauActuel": 4 }
  ]
}
```

---

**`GET /api/chatbot/find?nom={nom}`**

**Description :** Recherche d'un collaborateur par nom (partielle, insensible à la casse).

**Réponse :** `{ "id": 7, "nom": "Sofien Klaou" }` ou `404 Not Found`

---

**`POST /api/chatbot/ask`**

**Description :** Point d'entrée principal du chatbot — route le message vers le bon webhook n8n.

**Body JSON :**
```json
{
  "message": "Quels sont les hauts potentiels ?",
  "page": "succession",
  "contextId": null
}
```

**Logique de routage (priorité) :**

| Condition | Webhook n8n cible |
|---|---|
| Page = "succession" OU mot-clé copilot détecté | `http://localhost:5678/webhook/hr-copilot` |
| Mot-clé talent détecté | `http://localhost:5678/webhook/hr-talent` |
| Défaut | `http://localhost:5678/webhook/hr-stats` |

**Mots-clés copilot :** haut potentiel, top talent, 9 box, promotion, succession, remplacer, départ, risque, poste critique, développement, carrière, profil, voir, fort potentiel, talent prêt, mérite, évoluer, remplaçant, successeur, sans successeur, poste vacant.

**Mots-clés talent :** talent, potentiel, meilleur.

**Réponse JSON :**
```json
{
  "answer": "Il y a 5 hauts potentiels dans votre organisation...",
  "analysis": "Analyse approfondie...",
  "actions": ["Voir les profils", "Lancer une revue talent"],
  "suggestions": ["Voir les postes à risque", "Qui est prêt pour une promotion ?"]
}
```

---

### 4.2 RhInsightsController — `/api/rhinsights/`

> `[Authorize(Roles = "ITAdmin,RH")]` requis

---

**`GET /api/rhinsights/matching/{id}`**

**Description :** Analyse complète des remplaçants pour un poste vacant — version enrichie avec Gap Analysis et livrables.

**Réponse :** Liste des 3 meilleurs candidats avec ScoreMatching, compétences communes/manquantes, `GapAnalysis` (écarts par compétence avec priorité High/Medium/Low), `LivrablesManquants`, `TransitionPlanNotes`.

---

**`GET /api/rhinsights/alertes`**

**Description :** Collaborateurs avec statut `Vacant` ou `EnPassation`.

**Réponse :** Liste des alertes de continuité (Id, Nom, Poste, Départment, Statut).

---

**`GET /api/rhinsights/compare/{id1}/{id2}`**

**Description :** Comparaison IA entre deux collaborateurs (compétences communes, manquantes, transversales).

**Réponse :**
```json
{
  "compatibilityScore": 75.0,
  "sharedSkills": ["Audit financier"],
  "missingSkills": ["Leadership"],
  "transversalSkills": ["IFRS"],
  "readinessScore": 80.0,
  "aiSummary": "Analyse IA : ...",
  "recommendedFormations": ["Formation recommandée : Leadership"]
}
```

---

**`POST /api/rhinsights/promotion-readiness`**

**Body :** `{ "collaborateurId": 5, "targetKey": "Manager|Manager|Audit" }`

**Description :** Simule la readiness promotionnelle d'un collaborateur vers un rôle cible.

**Réponse :** `PromotionReadinessResultViewModel` — ReadinessPercentage, CompatibilityScore, PromotionPotential, EstimatedMonthsMin/Max, MissingCompetencies (avec Gap), RecommendedFormations, ExecutiveSummary.

---

**`POST /api/rhinsights/workforce-impact`**

**Body :** `{ "collaborateurId": 5 }`

**Description :** Simule l'impact d'un départ sur l'organisation.

**Réponse :** `WorkforceImpactResultViewModel` — ContinuityRisk%, OperationalImpact%, DepartmentFragility%, StrategicDependencyScore%, RiskLevel (Critical/Elevated/Controlled), ImmediateSuccessors, PartialSuccessors, RecommendedActions.

---

### 4.3 CollaborateursController — APIs internes

**`POST /Collaborateurs/RecommendFormation`** — Body `{ userPrompt }` → Flowise IA (simulée)

**`POST /Collaborateurs/AskIA`** — Body `{ userPrompt }` → Flowise IA (simulée)

**`GET /Collaborateurs/GetProfilCandidat/{id}`** — Profil candidat (JSON)

**`GET /Collaborateurs/GetRemplacants/{id}`** — Remplaçants de même grade

**`GET /Collaborateurs/GetPostesParDepartement?departement=X`** — Postes disponibles par département

**`POST /Collaborateurs/EnvoyerDemandeEntretiens`** — Body `{ partantId, candidatsIds, commentaire }` → Email RH (simulation console)

**`POST /Collaborateurs/ExportComparaisonRemplacantsPdf`** — Export PDF comparaison jusqu'à 3 candidats

---

### 4.4 CompetencesController — APIs internes

**`POST /api/competences/ajouter-catalogue`** — Ajoute des compétences depuis le catalogue EY

Body :
```json
{
  "collaborateurId": 5,
  "competences": [
    { "nom": "Azure Functions", "niveau": 4 }
  ]
}
```

**`GET /Competences/GetCompetencesParGrade?grade=Senior`** — Compétences recommandées par grade (depuis ReferentielRhService)

---

### 4.5 TalentController — APIs AJAX

**`GET /Talent/GetCollaborateurDetails/{id}`** — PartialView `_EvaluatePanel` avec scores auto-calculés

**`POST /Talent/EvaluateAjax`** — Sauvegarde une évaluation manuelle 9-Box

**`POST /Talent/UpdateKeyResult`** — Met à jour la valeur d'un KeyResult OKR

**`POST /Talent/ValidateOKR`** — Valide un OKR (Manager uniquement)

---

## 5. TALENT MANAGEMENT

### 5.1 Vue d'Ensemble du Module

Le module Talent Management (`TalentController`) est accessible aux rôles `ITAdmin`, `RH`, `Manager`. Il offre :
- Un dashboard des hauts potentiels
- La matrice 9-Box interactive
- La gestion des OKRs

### 5.2 Hauts Potentiels

**Définition :** Collaborateur actif avec une moyenne de compétences ≥ 4.0/5.

**Vue : `Talent/Index`**

**Algorithme de détection :**
```
Score = moyenne(NiveauActuel) de toutes les compétences du collaborateur
Si Score >= 4.0 → Haut Potentiel
```

**Badge attribué automatiquement :**
- Score ≥ 4.5 → "Talent stratégique"
- Grade = "Manager" → "Expert confirmé"
- Sinon → "Leader émergent"

**Limite :** Top 8 talents affichés sur le dashboard.

**Scope :** Managers ne voient que leur équipe. ITAdmin/RH voient tout.

### 5.3 Matrice 9-Box

**Vue : `Talent/Matrix9Box`**

**Filtres disponibles :** Département, Grade

**Calcul automatique (si pas d'évaluation manuelle) :**

*Score de Performance (1-5) :*
```
Base = 3
+ round(moyenneCompétences / 5 × 2) → 0 à 2 points
+ 1 si taux de complétion formations > 80%
Max = 5
```

*Score de Potentiel (1-5) :*
```
Base = 3
+ 1 si ancienneté < 2 ans ET grade Senior ou Manager
+ 1 si a des formations terminées
Max = 5
```

*Catégorie 9-Box :*
```
(Perf >= 4, Pot >= 4) → Star            ⭐ Talent stratégique
(Perf >= 4, Pot = 3)  → FutureLeader    🚀 Leader stratégique
(Perf >= 4, Pot <= 2) → HighProfessional 💎 Expert métier
(Perf = 3,  Pot >= 4) → EmergingTalent  🌱 Potentiel émergent
(Perf = 3,  Pot = 3)  → SolidProfessional ✅ Collaborateur clé
(Perf = 3,  Pot <= 2) → InPlace         📍 Stable dans le poste
(Perf <= 2, Pot >= 4) → RisingStar      ⭐ Haut potentiel
(Perf <= 2, Pot = 3)  → NeedDevelopment 📈 Besoin d'accompagnement
Sinon                 → Underperformer  ⚠️ Performance insuffisante
```

**Évaluation manuelle :** Un manager/RH peut surcharger le calcul automatique via `POST /Talent/EvaluateAjax`. La dernière évaluation manuelle active (`Actif = true`) prévaut sur le calcul auto.

### 5.4 Promotions — Simulateur de Readiness

**Service : `IPromotionReadinessService`**

**Input :** CollaborateurId + TargetKey (format `"Poste|Grade|Département"`)

**Algorithme :**
1. Résout les compétences requises pour le poste cible (via `CompetencesRequisesParPoste` + fallback)
2. Calcule les gaps par compétence (RequiredLevel - CurrentLevel)
3. Calcule `ReadinessPercentage` = `max(35, 100 - (totalGap × 100 / maxGap))`
4. Calcule `CompatibilityScore` = % de compétences couvertes
5. Calcule `PromotionPotential` = combinaison readiness (58%) + compatibilité (24%) + indicateurs leadership + formations
6. Estime le délai : `estimatedMonths = max(2, totalGap × 2 + nbRecommandations) - trainingBonus`

**Niveau requis par grade cible :**
- Manager → niveau 4
- Senior → niveau 3
- Junior → niveau 2

### 5.5 Succession Planning

**Vue : `Collaborateurs/ChoisirRemplacant/{id}`**

**Processus complet :**
1. Identifie les compétences requises pour le poste du partant (profil + référentiel poste)
2. Pour chaque autre collaborateur actif :
   - Calcule le score de matching (% compétences communes / requises)
   - Identifie les compétences manquantes
   - Recommande des formations pour chaque gap
   - Détecte si le profil est "transversal" (autre département)
3. Trie : profils transversaux d'abord, puis par nb communes, puis par moins de manquantes
4. Affiche la liste complète avec export PDF possible (jusqu'à 3 candidats)

**Export PDF (`ComparaisonRemplacantsPdf`) :**
- Tableau de couverture par compétence (✅/❌ par candidat)
- Score de compatibilité par candidat
- Formations recommandées par candidat (jusqu'à 2)

### 5.6 Postes à Risque (Succession)

**Endpoint : `GET /api/chatbot/postes-a-risque`**

**Règle :** Un poste est à risque si le titulaire a un score de compétences inférieur au seuil de son grade. Niveaux de risque : Élevé (< seuil + ancienneté > 2 ans), Moyen (< seuil), Faible (ancienneté < 1 an).

### 5.7 Mobilité Interne

**Service : `WorkforceImpactService.SimulateAsync()`**

Identifie 3 niveaux de successeurs :
- **Immediat** (Readiness ≥ 75%) — peut prendre le poste rapidement
- **Partiel** (Readiness 45-74%) — nécessite un accompagnement
- **Haut potentiel** (< 75%) — potentiel futur identifié

**Formule de readiness successeur :**
```
skillScore     = partagéesAvecCible / requisesTotal × 100
levelScore     = moyenneNiveauxCompétences × 14
transversalBonus = 12 si autre département, 7 si même département
seniorityBonus = 8 si grade Senior ou Manager
trainingBonus  = completedTrainings × 2

readiness = skillScore × 0.62 + levelScore × 0.18 + transversalBonus + seniorityBonus + trainingBonus
```

### 5.8 OKRs (Objectifs et Résultats Clés)

**Vue : `Talent/MyOKRs`**

**Cycle de vie d'un OKR :**
```
Draft → (validation Manager) → Active → OnTrack / AtRisk → Completed
```

**Règles de calcul :**
- ProgressionGlobale = moyenne des progressions des KeyResults
- Statut automatique : ≥ 100% → Completed, ≥ 70% → OnTrack, ≥ 30% → Active, < 30% → AtRisk

**Accès :**
- Collaborateur : voit ses propres OKRs
- Manager/RH/ITAdmin : voient les OKRs de leur équipe / tous
- Seul le propriétaire (via `IOwnershipService.OwnsOkrAsync`) peut modifier ses KeyResults

---

## 6. MODULE FORMATION

### 6.1 Catalogue de Formations

**Vue : `Formations/Index`** — Catalogue complet avec filtres

**Filtres disponibles :**
- Par département cible
- Par poste cible
- Par domaine de compétence
- Formations "déjà inscrit" masquées

**Champs enrichis (ajoutés lors du `FormationEnrichmentSeeder`) :**
- `Plateforme` : Udemy, Coursera, Microsoft Learn, AWS Skill Builder, LinkedIn Learning, EY Learning
- `ExternalUrl` : lien direct vers la plateforme
- `Description` : description complète
- `CertificationNom` : nom de la certification obtenue
- `EstStrategique` / `EstForteDemande` : badges visuels

### 6.2 Inscriptions

**Action : `POST /Formations/Inscrire`**

**Règles :**
1. Vérification des places disponibles (`PlacesPrises < CapaciteMax`)
2. Création de l'inscription avec `Terminee = false`, `Progression = 0`
3. Incrémentation de `Formation.PlacesPrises`

**Accès :** Tous les rôles peuvent s'inscrire à leur propre compte (scope via `ITeamAccessService`).

### 6.3 Progression et Module

**Vue : `Formations/ModuleFormation`** (prototype)

**Action : `POST /Formations/AvancerModule`**

- Incrémente la progression de `deltaPourcent` (défaut 20%)
- Plafond à 100%

### 6.4 Terminer une Formation

**Action : `POST /Formations/TerminerFormation`**

**Règles métier :**
1. Marque l'inscription `Terminee = true`, `DateCompletion = now`, `Progression = 100`
2. Récupère la `CompetenceVisee` de la formation
3. Si compétence trouvée et non existante sur le profil → **crée la compétence** avec NiveauActuel = 1, NiveauCible selon le grade
4. Si compétence existante et NiveauActuel < NiveauCible → **incrémente NiveauActuel de 1**
5. Si déjà au niveau cible → message informatif

**Règle `NiveauCible` par grade (`CompetenceRules.GetNiveauCibleParGrade`) :**

| Grade | NiveauCible |
|---|---|
| Junior | 2 |
| Senior | 3 |
| Manager | 4 |
| Director / Partner | 5 |

### 6.5 Certifications

**Vue : `Certificats/Index`** — Liste des certifications obtenues

**Export PDF : `GET /Formations/TelechargerCertificat/{inscriptionId}`**

- Disponible uniquement si `inscription.Terminee = true`
- Génère un PDF via `CertificatFormationPdf.Generer(inscription)`
- Nom du fichier : `Certificat_{TitreFormation}.pdf`

**Sécurité :** `IOwnershipService.OwnsInscriptionAsync` — seul le propriétaire peut télécharger son certificat.

### 6.6 Planification d'Examen

**Vue : `Formations/PlanifierExamen`**

**Action : `POST /Formations/PlanifierExamen`**

- Enregistre `inscription.DateExamen`
- Validation : date doit être aujourd'hui ou dans le futur

### 6.7 Recommandations de Formations

**Vue : `Formations/Recommandations/{collaborateurId}`**

**3 types de recommandations :**

| Type | Source | Score |
|---|---|---|
| `plan` | Formation dans le plan de développement non encore suivi | 90 |
| `competence` | Formation ciblant une compétence manquante (NiveauActuel < NiveauCible) | 85 |
| `grade` | Formation préparant au prochain grade | 75 |

**Prochain grade :**
- Junior → Senior
- Senior → Manager
- Manager → Director
- Director → Partner

### 6.8 Parcours Carrière

**Vue : `Formations/ParcoursCarriere/{collaborateurId}`**

Affiche :
- Compétences acquises (NiveauActuel ≥ NiveauCible)
- Compétences manquantes avec formation recommandée
- Progression grade (% compétences au niveau cible)
- Formations recommandées couvrant les gaps

### 6.9 Plan de Développement

**Service : `IPlanDeveloppementService.GenererPourCollaborateurAsync()`**

Génère automatiquement un plan de développement basé sur les compétences manquantes.

### 6.10 Formations Obligatoires

**Règle :** Une formation est considérée obligatoire si :
1. Son titre contient "RGPD" ou "Conformité", OU
2. Elle est listée dans le champ `FormationsObligatoires` du collaborateur

### 6.11 Catalogue de Compétences par Secteur EY

Le `CompetencesController` expose un catalogue structuré par secteur EY :

| Secteur | Exemples de compétences |
|---|---|
| Assurance | Audit financier, Audit interne, IFRS, Risk Assessment, Compliance |
| Consulting | BRD Execution, Requirements Gathering, Stakeholder Management, Change Management |
| Strategy & Transactions | Financial Modeling, Valuation, Due Diligence, Market Analysis |
| TAX | Fiscalité internationale, Transfer Pricing, Tax Compliance, VAT/GST |
| CBS Support | Azure Functions, Azure Service Bus, Power Automate, Customization D365, Data Migration |

---

## 7. CHATBOT RH IA

### 7.1 Architecture Actuelle

```
[Utilisateur / n8n]
        │
        │ POST /api/chatbot/ask
        │ { message, page, contextId }
        ▼
[ChatbotController.Ask]
        │
        │ Détection d'intention (mots-clés)
        ▼
[Routage vers webhook n8n]
        │
    ┌───┴────────────────────┐
    │                        │
    ▼                        ▼
hr-stats webhook     hr-talent webhook    hr-copilot webhook
(questions générales) (talents, top)    (succession, promotion)
    │                        │                   │
    ▼                        ▼                   ▼
[n8n flow]          [n8n flow]           [n8n flow]
    │
    ▼
[GET /api/chatbot/stats] ou autres endpoints
    │
    ▼
[Format Reply en n8n]
    │
    ▼
[Réponse JSON] → ChatbotController → Réponse finale
```

### 7.2 Widget Frontend

**Fichier :** `Views/Shared/_ChatbotWidget.cshtml`

**Fonctionnalités :**
- Bouton flottant en bas à droite de toutes les pages
- Panel chat avec historique de la session
- Quick prompts prédéfinis : "Top talents & hauts potentiels?", "Candidats à la promotion?", "Répartition des effectifs?"
- Envoi du contexte de page (`page` = URL courante)
- Affichage structuré : answer + analysis + actions + suggestions

**JavaScript :** Appels `fetch('/api/chatbot/ask')` en POST JSON.

### 7.3 Endpoints Utilisés par le Chatbot

| Webhook n8n | Endpoint SIRH appelé | Données retournées |
|---|---|---|
| `hr-stats` | `GET /api/chatbot/stats` | Statistiques RH globales |
| `hr-talent` | `GET /api/chatbot/hr-talent` | Top talents (score ≥ 4) |
| `hr-copilot` | `GET /api/chatbot/hr-copilot-data` et autres | Talents, promotables, succession |

### 7.4 Format de Réponse

**Succès :**
```json
{
  "answer": "Texte principal de la réponse",
  "analysis": "Analyse complémentaire",
  "actions": ["Action suggérée 1", "Action 2"],
  "suggestions": ["Question suggérée 1", "Question 2"]
}
```

**Erreur (n8n indisponible) :**
```json
{
  "answer": "Service temporairement indisponible.",
  "analysis": "",
  "actions": [],
  "suggestions": []
}
```

### 7.5 Intégration Flowise (Placeholder)

`FlowiseService.GetPredictionAsync(userPrompt)` retourne actuellement une réponse simulée :
```
"Réponse simulée pour: {userPrompt}"
```

Deux endpoints du CollaborateursController l'utilisent : `RecommendFormation` et `AskIA`. **Ce composant est à brancher sur un vrai LLM (Flowise, Claude API, GPT)**.

### 7.6 Contexte Conversationnel

**État actuel :** Le chatbot est stateless — pas de mémoire de session. Chaque message est indépendant.

**Champ `contextId` dans la requête** — Non utilisé côté serveur actuellement, prévu pour un futur contexte conversationnel.

**Page context (`page`)** — Transmis dans la requête, permet de router vers `hr-copilot` si la page est "succession".

---

## 8. WORKFLOWS N8N

### 8.1 Infrastructure n8n

- **URL locale :** `http://localhost:5678`
- **Format des webhooks :** `POST http://localhost:5678/webhook/{nom}`
- **3 webhooks documentés :**
  - `hr-stats` — Statistiques générales
  - `hr-talent` — Hauts potentiels
  - `hr-copilot` — Succession, promotions (non documenté en JSON mais référencé)

### 8.2 Workflow 1 : SIRH-EY Chatbot UC1 — Statistiques RH

**Fichier :** `n8n/sirh-chatbot-uc1.json`

**Déclencheur :** `POST http://localhost:5678/webhook/hr-chatbot`

#### Nœud 1 — Webhook

| Propriété | Valeur |
|---|---|
| Méthode | POST |
| Path | `hr-chatbot` |
| Response Mode | responseNode |

**Input attendu :** `{ body: { message: "..." } }`

#### Nœud 2 — Detect Intent (Code JavaScript)

**Rôle :** Analyse le message et détecte l'intention parmi 5 intents.

**Intents détectés :**

| Intent | Mots-clés déclencheurs |
|---|---|
| `COLLAB_ACTIFS` | actif, actifs, collaborateur, collaborateurs, effectif, combien |
| `FORMATIONS_EN_COURS` | formation, formations, cours, en cours, suivi, inscrit, inscrits |
| `REPARTITION_DEPT` | departement, département, repartition, répartition, service, equipe |
| `COMPETENCES` | competence, compétence, skill, niveau, top |
| `TAUX_COMPLETION` | taux, completion, terminé, achevé, fini |
| `UNKNOWN` | Défaut si aucun intent détecté |

**Output :** `{ message, intent }`

#### Nœud 3 — Get RH Stats (HTTP Request)

| Propriété | Valeur |
|---|---|
| Méthode | GET |
| URL | `http://localhost:5000/api/chatbot/stats` |
| Timeout | 10 000 ms |

**Note :** L'URL cible est `localhost:5000` (port local de l'app ASP.NET Core en développement).

#### Nœud 4 — Format Reply (Code JavaScript)

**Rôle :** Formate la réponse selon l'intent détecté.

| Intent | Réponse type |
|---|---|
| COLLAB_ACTIFS | "Il y a actuellement **{n}** collaborateurs actifs dans le SIRH EY." |
| FORMATIONS_EN_COURS | "{n} formation(s) sont en cours sur un total de {total} inscription(s)." |
| REPARTITION_DEPT | Liste des 5 premiers départements avec leur effectif |
| COMPETENCES | Top compétences avec nb collaborateurs et niveau moyen |
| TAUX_COMPLETION | Taux de complétion des formations en % |
| UNKNOWN | Message d'aide listant les topics disponibles |

#### Nœud 5 — Respond to Webhook

Retourne `{ reply: "..." }` au format JSON.

### 8.3 Workflows Référencés (Non documentés en JSON)

| Webhook | URL | Usage |
|---|---|---|
| `hr-talent` | `localhost:5678/webhook/hr-talent` | Questions sur les talents, potentiels |
| `hr-copilot` | `localhost:5678/webhook/hr-copilot` | Questions sur succession, promotions, carrieres |

**Ces workflows doivent appeler :**
- `GET /api/chatbot/hr-talent` (pour hr-talent)
- `GET /api/chatbot/hr-copilot-data`, `/api/chatbot/succession/{id}`, `/api/chatbot/promotables` (pour hr-copilot)

---

## 9. FONCTIONNALITÉS TERMINÉES

### 9.1 Authentification & Sécurité ✅

- [x] ASP.NET Identity avec 4 rôles (ITAdmin, RH, Manager, Collaborateur)
- [x] Politique globale d'authentification (toute route protégée)
- [x] Seed automatique des rôles et utilisateurs au démarrage
- [x] Attribution des rôles selon département/grade
- [x] `ITeamAccessService` — scope des données par rôle
- [x] `IOwnershipService` — vérification de propriété (OKR, compétence, inscription)
- [x] Principe 4-yeux pour la validation des compétences (manager ne peut pas valider les siennes)
- [x] Login/Logout via ASP.NET Identity Razor Pages (`/Identity/Account/Login`)

### 9.2 Gestion des Collaborateurs ✅

- [x] CRUD complet (Create, Read, Update, Delete)
- [x] Hiérarchie manager/équipe (self-referential)
- [x] Fiche complète avec données personnelles, contractuelles, RH
- [x] Assignation de manager (bulk)
- [x] Processus de départ (Actif = false)
- [x] ChoisirRemplacant avec algorithme de matching
- [x] Export PDF comparaison remplaçants (jusqu'à 3)
- [x] Sync automatique des champs legacy depuis les FK master data
- [x] Filtres et tri dans la liste (nom, département)
- [x] Dropdowns dynamiques département→postes
- [x] Vue "En attente de validation" (collaborateurs avec statut particulier)

### 9.3 Gestion des Compétences ✅

- [x] CRUD compétences par collaborateur
- [x] Catalogue EY par secteur (5 secteurs, 35+ compétences)
- [x] Ajout depuis catalogue (API `/api/competences/ajouter-catalogue`)
- [x] Auto-évaluation collaborateur (0-100 → converti en 1-5)
- [x] Validation manager (4-yeux)
- [x] Matrice d'équipe (vue croisée collaborateurs/compétences)
- [x] Catégories de compétences (Audit, Tech, Management, Risk, Fiscalité, Méthodes...)
- [x] Plan de développement automatique (GenererPourCollaborateurAsync)
- [x] Référentiel compétences par grade/poste (CompetencesRequisesParPoste)
- [x] Historique des évaluations (EvaluationsHistoriques)

### 9.4 Formations ✅

- [x] Catalogue enrichi (50+ formations avec métadonnées)
- [x] Filtres catalogue (département, poste, domaine)
- [x] Inscription/Désinscription avec gestion des places
- [x] Suivi de progression (0-100%)
- [x] Module de formation (prototype avancement step-by-step)
- [x] Planification d'examen (DateExamen)
- [x] Terminer une formation (logique compétence automatique)
- [x] Génération PDF de certificat
- [x] Recommandations personnalisées (3 types : plan, compétence, grade)
- [x] Parcours carrière (compétences acquises vs manquantes)
- [x] Formations obligatoires (RGPD, Conformité)
- [x] Enrichissement plateforme (Udemy, Coursera, etc.)

### 9.5 Talent Management ✅

- [x] Dashboard hauts potentiels (score ≥ 4)
- [x] Matrice 9-Box interactive (calcul auto + override manuel)
- [x] Panel de détail collaborateur (AJAX, stats enrichies)
- [x] Système OKR (création, KeyResults, progression, validation)
- [x] Distribution 9-Box (graphique Chart.js)
- [x] Filtres matrice (département, grade)
- [x] Scope équipe pour les managers

### 9.6 Succession Planning ✅

- [x] Identification des remplaçants (algorithm matching compétences)
- [x] Détection des postes sans successeur
- [x] Détection des postes à risque (score vs seuil grade)
- [x] Profils transversaux valorisés dans le matching
- [x] Compétences manquantes + formations recommandées
- [x] Export PDF comparaison (ComparaisonRemplacantsPdf)
- [x] Algorithme de succession API (`/api/chatbot/succession/{id}`)
- [x] Workflow n8n HR Copilot (succession via chatbot)

### 9.7 RH Insights (Analytics) ✅

- [x] Dashboard exécutif (6 KPIs dynamiques)
- [x] Alertes de continuité (postes Vacant/EnPassation)
- [x] Smart Alerts IA (4 types : succession, compétences, formation, mobilité)
- [x] Hidden Talents (4 talents "cachés" avec score readiness)
- [x] Skill Heatmap (couverture compétences : Critical/Warning/Healthy)
- [x] Formation Insights (urgence score, impact attendu)
- [x] Simulateur Promotion Readiness (interactif, API AJAX)
- [x] Simulateur Workforce Impact (impact départ collaborateur)
- [x] Matching remplaçants enrichi (Gap Analysis, Livrables, Transition Plan)
- [x] Comparaison IA 2 collaborateurs (`/api/rhinsights/compare/{id1}/{id2}`)

### 9.8 Chatbot ✅

- [x] Widget flottant dans toutes les pages
- [x] 3 webhooks n8n opérationnels (hr-stats, hr-talent, hr-copilot)
- [x] Détection d'intention par mots-clés
- [x] Quick prompts
- [x] Routage intelligent selon page et mots-clés
- [x] Format réponse structuré (answer + analysis + actions + suggestions)
- [x] Tous les endpoints API `/api/chatbot/*` opérationnels

### 9.9 Infrastructure ✅

- [x] 25 migrations EF Core (historique complet)
- [x] 3 seeders (DemoDataSeeder, EnterpriseDemoSeeder, FormationEnrichmentSeeder)
- [x] Seed HR Master Data (départements, grades, postes, BU, localisations, types contrat)
- [x] 10 collaborateurs seed avec hiérarchie manager
- [x] Paramètres système configurables (SystemParameters)
- [x] QuestPDF pour génération PDF
- [x] Tailwind CSS (build présent)
- [x] Chart.js pour graphiques

---

## 10. FONCTIONNALITÉS FUTURES

### 10.1 HR Copilot Enterprise (Priorité Haute)

**Description :** Passer du chatbot basé sur des règles (mots-clés) à un vrai LLM conversationnel.

**Ce qui manque :**
- Brancher `FlowiseService` sur un vrai modèle (Flowise avec Claude Sonnet ou GPT-4)
- Implémenter un contexte conversationnel (mémoire de session par `contextId`)
- Permettre des questions complexes multi-tours ("Quels sont les talents dans l'équipe d'Ahmed ?" → "Et parmi eux, qui est prêt pour une promotion ?")
- Générateur de réponses structurées avec `actions` et `suggestions` dynamiques

**Implémentation suggérée :**
1. Configurer Flowise avec un pipeline RAG sur les données SIRH
2. Décommenter/compléter `FlowiseService.GetPredictionAsync()`
3. Ajouter la persistance de session via `contextId` (ex: stockage Redis ou DB)

### 10.2 Explainable AI (Priorité Haute)

**Description :** Expliquer les décisions algorithmiques aux utilisateurs RH.

**Exemples :**
- "Pourquoi ce collaborateur est-il classé en 9-Box Star ?" → Détail du calcul Performance/Potentiel
- "Pourquoi ce candidat est recommandé comme successeur ?" → Détail du score matching
- "Pourquoi ce poste est-il à risque ?" → Détail seuil vs score actuel

**Implémentation :** Enrichir les ViewModels avec des champs `Explanation` ou `Reasoning`.

### 10.3 Historique des Décisions RH

**Description :** Tracer toutes les décisions importantes avec date, acteur, contexte.

**Exemples de décisions à tracer :**
- Reclassement 9-Box (qui a changé la catégorie, de quoi à quoi, pourquoi)
- Validation de compétences
- Décisions de promotion
- Changements de statut (Vacant, EnPassation)

**Table à créer :** `DecisionsRH` (Type, EntityId, EntityType, OldValue, NewValue, UserId, Date, Commentaire)

### 10.4 Learning Recommendations Avancées

**Description :** Système de recommandation basé sur des algorithmes plus sophistiqués.

**Améliorations :**
- Collaborative filtering (recommander ce que des pairs similaires ont suivi)
- Learning Path automatique basé sur le gap Grade N → Grade N+1
- Intégration Udemy API / Coursera API pour catalogue en temps réel
- Notifications automatiques (email) pour formations urgentes
- Badge system pour les formations certifiantes complétées

### 10.5 Succession Analytics

**Description :** Tableaux de bord dédiés à la planification de la succession.

**Fonctionnalités :**
- Carte organisationnelle interactive avec succession coverage par nœud
- Timeline de succession (qui est prêt dans 6 mois / 1 an / 2 ans)
- Risques de succession par département (heatmap)
- Alertes automatiques si coverage < seuil (ex: < 1 successeur pour un poste Manager)
- Rapport succession périodique (PDF automatique)

### 10.6 Talent Insights (Dashboard Avancé)

**Description :** Tableau de bord analytique RH enrichi.

**Fonctionnalités :**
- Tendances temporelles (évolution de la moyenne des compétences sur 12 mois)
- Prédiction de départs (modèle basé sur ancienneté + compétences stagnantes)
- Benchmark sectoriel (EY vs marché)
- Analyse de diversité (genre, ancienneté par département)
- ROI formation (progression compétences avant/après formation)
- NPS collaborateur (enquêtes de satisfaction)

### 10.7 Améliorations Techniques

- **Contexte conversationnel chatbot** — Implémenter `contextId` avec Redis/DB
- **Tests unitaires** — Couvrir les services de calcul (PromotionReadiness, WorkforceImpact)
- **Cache distribué** — Remplacer IMemoryCache par Redis pour les endpoints API
- **Notification push** — SignalR pour alertes temps réel (poste vacant, OKR en retard)
- **Audit trail** — Logguer toutes les modifications sensibles
- **Dataverse** — Compléter l'intégration Microsoft Dataverse (actuellement conditionnelle)
- **Export Excel** — Export des tableaux de bord en Excel/CSV
- **Mobile-friendly** — Responsive complet pour usage mobile manager

---

## 11. CARTOGRAPHIE COMPLÈTE

### 11.1 Tableau Use Case → Stack

| Use Case | Controller | Service(s) | Endpoint(s) | Vue | n8n |
|---|---|---|---|---|---|
| **Voir effectifs** | ChatbotController | — | GET /api/chatbot/stats | — | hr-stats webhook |
| **Voir hauts potentiels** | ChatbotController / TalentController | — | GET /api/chatbot/hr-talent | Talent/Index | hr-talent webhook |
| **Matrice 9-Box** | TalentController | — | GET /Talent/Matrix9Box | Talent/Matrix9Box | — |
| **Évaluer talent manuellement** | TalentController | — | POST /Talent/EvaluateAjax | Talent/_EvaluatePanel (partial) | — |
| **Voir successeurs** | ChatbotController / CollaborateursController | — | GET /api/chatbot/succession/{id} | Collaborateurs/ChoisirRemplacant | hr-copilot webhook |
| **Postes sans successeur** | ChatbotController | — | GET /api/chatbot/postes-sans-successeur | — | hr-copilot webhook |
| **Postes à risque** | ChatbotController | — | GET /api/chatbot/postes-a-risque | — | hr-copilot webhook |
| **Candidats promotion** | ChatbotController | PromotionReadinessService | GET /api/chatbot/promotables | RHInsights/_PromotionReadiness | hr-copilot webhook |
| **Simuler promotion readiness** | RhInsightsController | PromotionReadinessService | POST /api/rhinsights/promotion-readiness | RHInsights/Index | — |
| **Simuler workforce impact** | RhInsightsController | WorkforceImpactService | POST /api/rhinsights/workforce-impact | RHInsights/Index | — |
| **Matching remplaçants (Insights)** | RhInsightsController | — | GET /api/rhinsights/matching/{id} | RHInsights/Index | — |
| **Alertes continuité** | RhInsightsController | — | GET /api/rhinsights/alertes | RHInsights/Index | — |
| **Comparaison IA 2 collabs** | RhInsightsController | — | GET /api/rhinsights/compare/{id1}/{id2} | RHInsights/Index | — |
| **Catalogue formations** | FormationsController | ParametreService | GET /Formations/Index | Formations/Index | — |
| **S'inscrire formation** | FormationsController | — | POST /Formations/Inscrire | Formations/Index | — |
| **Terminer formation** | FormationsController | CompetenceRules | POST /Formations/TerminerFormation | — | — |
| **Télécharger certificat** | FormationsController | CertificatFormationPdf | GET /Formations/TelechargerCertificat/{id} | — | — |
| **Recommandations formations** | FormationsController | — | GET /Formations/Recommandations/{id} | Formations/Recommandations | — |
| **Parcours carrière** | FormationsController | — | GET /Formations/ParcoursCarriere/{id} | Formations/ParcoursCarriere | — |
| **Auto-évaluation compétence** | CompetencesController | OwnershipService | GET/POST /Competences/AutoEvaluation | Competences/AutoEvaluation | — |
| **Validation manager compétence** | CompetencesController | OwnershipService, TeamAccessService | GET/POST /Competences/ValidationManager | Competences/ValidationManager | — |
| **Matrice équipe** | CompetencesController | — | GET /Competences/MatriceEquipe | Competences/MatriceEquipe | — |
| **Catalogue compétences EY** | CompetencesController | — | GET /Competences/Catalogue/{id} + POST /api/competences/ajouter-catalogue | Competences/Catalogue | — |
| **Gérer collaborateur** | CollaborateursController | TeamAccessService | GET/POST /Collaborateurs/* | Collaborateurs/Index, Create, Edit, Details | — |
| **Choisir remplaçant** | CollaborateursController | — | GET /Collaborateurs/ChoisirRemplacant/{id} | Collaborateurs/ChoisirRemplacant | — |
| **Export PDF remplaçants** | CollaborateursController | ComparaisonRemplacantsPdf | POST /Collaborateurs/ExportComparaisonRemplacantsPdf | — | — |
| **Gérer OKRs** | TalentController | OwnershipService | GET/POST /Talent/MyOKRs, CreateOKR | Talent/MyOKRs | — |
| **Chatbot RH** | ChatbotController | — | POST /api/chatbot/ask | Shared/_ChatbotWidget (toutes pages) | hr-stats / hr-talent / hr-copilot |
| **Dashboard RH** | HomeController | — | GET /Home/Index | Home/Index | — |
| **Dashboard exécutif** | ReportingController | — | GET /Reporting/ExecutiveDashboard | Reporting/ExecutiveDashboard | — |
| **Plan développement** | CompetencesController | PlanDeveloppementService | POST /Competences/GenererPlanDeveloppement | Competences/Index | — |
| **Paramètres système** | HomeController | ParametreService | GET/POST /Home/Settings | Home/Settings | — |
| **Départ collaborateur** | CollaborateursController | — | GET/POST /Collaborateurs/Depart/{id} | Collaborateurs/Depart | — |

### 11.2 Tableau des Controllers

| Controller | Type | Rôle principal | Nombre d'actions |
|---|---|---|---|
| `HomeController` | MVC | Dashboard principal, Settings | ~5 |
| `CollaborateursController` | MVC + API | CRUD collaborateurs, succession, export | ~15 |
| `CompetencesController` | MVC + API | CRUD compétences, catalogue, évaluation | ~15 |
| `FormationsController` | MVC | Catalogue, inscriptions, parcours, certificats | ~12 |
| `TalentController` | MVC + API | 9-Box, dashboard, OKRs | ~10 |
| `RhInsightsController` | MVC + API | Analytics, simulateurs, alertes | ~8 |
| `ChatbotController` | API (ApiController) | Tous les endpoints chatbot | ~9 |
| `InscriptionsController` | MVC | CRUD inscriptions | ~6 |
| `CertificatsController` | MVC | Gestion certificats | ~3 |
| `ReportingController` | MVC | Executive Dashboard | ~2 |

### 11.3 Tableau des Services

| Interface | Implémentation | Rôle | Scope |
|---|---|---|---|
| `IPromotionReadinessService` | `PromotionReadinessService` | Simulation readiness promotion | Scoped |
| `IWorkforceImpactService` | `WorkforceImpactService` | Simulation impact départ | Scoped |
| `IPlanDeveloppementService` | `PlanDeveloppementService` | Génération plans développement | Scoped |
| `IReferentielRhService` | `ReferentielRhService` | Référentiel compétences/grades | Scoped |
| `IParametreService` | `ParametreService` | Paramètres applicatifs | Scoped |
| `IUserContextService` | `UserContextService` | Contexte utilisateur courant | Scoped |
| `ITeamAccessService` | `TeamAccessService` | Scope données par rôle | Scoped |
| `IOwnershipService` | `OwnershipService` | Vérification propriété | Scoped |
| `IEmailSender` | `EmailSender` | Envoi d'emails | Transient |
| `FlowiseService` | `FlowiseService` | Appels IA (simulés) | HttpClient |
| `IDataverseService` | `DataverseService` | Intégration Microsoft Dataverse | Scoped (conditionnel) |
| — | `CompetenceCatalogService` | Catalogue statique départements/postes | Statique |
| — | `CompetenceRules` | Règles niveau cible par grade | Statique |
| — | `CertificatFormationPdf` | Génération PDF certificats | Statique |
| — | `ComparaisonRemplacantsPdf` | Génération PDF comparaison | Statique |

### 11.4 Tableau des Vues

| Vue | URL | Rôles | Description |
|---|---|---|---|
| `Home/Index` | `/` | Tous | Dashboard principal |
| `Collaborateurs/Index` | `/Collaborateurs` | Tous (scope rôle) | Liste collaborateurs filtrée |
| `Collaborateurs/Details/{id}` | `/Collaborateurs/Details/{id}` | Scope rôle | Fiche complète |
| `Collaborateurs/ChoisirRemplacant/{id}` | `/Collaborateurs/ChoisirRemplacant/{id}` | ITAdmin, RH | Succession planning |
| `Collaborateurs/Depart/{id}` | `/Collaborateurs/Depart/{id}` | ITAdmin, RH | Processus départ |
| `Competences/Index` | `/Competences?collaborateurId={id}` | Scope rôle | Compétences du collaborateur |
| `Competences/Catalogue/{id}` | `/Competences/Catalogue/{id}` | Scope rôle | Catalogue EY par secteur |
| `Competences/AutoEvaluation/{id}` | `/Competences/AutoEvaluation/{id}` | Propriétaire | Auto-évaluation |
| `Competences/ValidationManager/{id}` | `/Competences/ValidationManager/{id}` | Manager, RH, ITAdmin | Validation double signature |
| `Competences/MatriceEquipe` | `/Competences/MatriceEquipe` | Manager+, RH, ITAdmin | Matrice croisée équipe/compétences |
| `Formations/Index` | `/Formations?collaborateurId={id}` | Scope rôle | Catalogue + inscriptions |
| `Formations/Recommandations/{id}` | `/Formations/Recommandations/{id}` | Scope rôle | Recommandations personnalisées |
| `Formations/ParcoursCarriere/{id}` | `/Formations/ParcoursCarriere/{id}` | Scope rôle | Parcours carrière |
| `Formations/Details/{id}` | `/Formations/Details/{id}` | Tous | Détail formation + score adéquation |
| `Talent/Index` | `/Talent` | Manager, RH, ITAdmin | Dashboard talent |
| `Talent/Matrix9Box` | `/Talent/Matrix9Box` | Manager, RH, ITAdmin | Matrice 9-Box interactive |
| `Talent/MyOKRs` | `/Talent/MyOKRs` | Tous | OKRs du collaborateur |
| `RHInsights/Index` | `/RhInsights` | RH, ITAdmin | Analytics RH complet |
| `Reporting/ExecutiveDashboard` | `/Reporting/ExecutiveDashboard` | Tous | Dashboard exécutif |
| `Certificats/Index` | `/Certificats` | Tous | Liste certifications |

### 11.5 Compte Seed par Défaut

| Email | Mot de passe | Rôle | Poste | Département |
|---|---|---|---|---|
| admin@ey.tn | Admin@123456 | ITAdmin | — | — |
| rh@ey.tn | Rh@123456 | RH | — | — |
| hanine.hammami@ey.com | Temp@123456 | RH | HR Director | RH |
| smiai.nour@ey.com | Temp@123456 | Collaborateur | Data Analyst | Tax |
| mariem.safri@ey.com | Temp@123456 | Collaborateur | Senior Auditor | Audit |
| raed.amri@ey.com | Temp@123456 | Collaborateur | Consultant | Consulting |
| ayoub.gombra@ey.com | Temp@123456 | Collaborateur | Consultant | Tax |
| Ahmed.benyoussef@ey.com | Temp@123456 | Manager | Audit Manager | Audit |
| sofien.klaou@ey.com | Temp@123456 | Collaborateur | Senior Consultant | Advisory |
| ibtissem.bessrour@ey.com | Temp@123456 | Manager | Risk Manager | Risk |

**Hiérarchie manager :**
- Ahmed Ben Youssef (Audit Manager) → manage Mariem Safri (Senior Auditor)
- Ibtissem Bessrour (Risk Manager) → manage membres Risk

---

## ANNEXES

### A. Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=SIRH_EY;..."
  },
  "Dataverse": {
    "EnvironmentUrl": "",
    "Username": "",
    "Password": ""
  },
  "Flowise": {
    "BaseUrl": "http://localhost:3000",
    "ApiKey": "..."
  }
}
```

### B. Seeders

| Seeder | Rôle | Données |
|---|---|---|
| `DemoDataSeeder` | Données de démo de base | Compétences, formations, inscriptions initiales |
| `EnterpriseDemoSeeder` | Profil bureau EY Tunisie | 30 collaborateurs, évaluations NineBox, plans développement |
| `FormationEnrichmentSeeder` | Enrichissement formations | ExternalUrl, Plateforme, Description, badges pour formations existantes |

### C. Migrations (chronologique)

| Date | Migration | Objet |
|---|---|---|
| 2026-04-08 | InitialCreate | Schéma initial |
| 2026-04-10 | AjoutOrganisme | Champ Organisme sur Formation |
| 2026-04-13 | AjoutCompetenceVisee | Champ CompetenceVisee sur Formation |
| 2026-04-15 | AjoutGrade | Champ Grade sur Collaborateur |
| 2026-04-21 | AjoutCompetencesRequisesParPoste | Table CompetencesRequisesParPoste |
| 2026-04-21 | AjoutPlanDeveloppement | Table PlansDeveloppement |
| 2026-04-21 | AjoutEvaluationHistorique | Table EvaluationsHistoriques |
| 2026-04-21 | AjoutReferentielCompetences | Référentiel compétences |
| 2026-04-22 | AjoutManagerCollaborateur | Champ ManagerId sur Collaborateur |
| 2026-04-23 | AjoutTableParametres | Table Parametres |
| 2026-04-24 | AjoutDateExamenEtProgression | Champs DateExamen, Progression sur Inscription |
| 2026-04-28 | AjoutEvaluationCompetence | Table EvaluationsCompetences |
| 2026-05-05 | AddIdentity | Tables ASP.NET Identity |
| 2026-05-05 | AddUserToCollaborateur | Champ UserId sur Collaborateur |
| 2026-05-06 | InitClean | Nettoyage schéma |
| 2026-05-07 | InitialSeed | Données seed initiales |
| 2026-05-07 | FormationCompetence | Table FormationCompetences (M:N) |
| 2026-05-07 | AddDateFinReelleToInscription | Champ DateFinReelle |
| 2026-05-07 | AjoutInscriptionIdEvaluationCompetence | FK InscriptionId sur EvaluationCompetence |
| 2026-05-07 | AddCategorieCompetenceRelation | Relation CategorieCompetence |
| 2026-05-07 | AddCategorieCompetenceSystem | Catégories système |
| 2026-05-07 | AddTalentManagementTables | Tables TalentEvaluations, OKRs, KeyResults |
| 2026-05-18 | AddStatutCollaborateur | Enum StatutCollaborateur |
| 2026-05-21 | EnrichCollaborateurEY | Champs EY enrichis |
| 2026-05-26 | HrProfileAndFormationCatalogColumns | Champs profil HR + catalogue formations |
| 2026-05-28 | SyncCollaborateurSchema | Sync schéma collaborateur |
| 2026-05-31 | AddHrMasterData | Tables Master Data (Departments, Grades, Positions...) |
| 2026-06-01 | AddHrMasterDataV2 | Extension Master Data |
| 2026-06-11 | FormationLearningEnrichment | Champs enrichissement formation (Plateforme, ExternalUrl...) |

### D. Enum StatutCollaborateur

```csharp
public enum StatutCollaborateur
{
    Actif,       // Employé actif
    Vacant,      // Poste vacant (départ sans remplaçant)
    EnPassation, // En cours de remplacement
    Inactif      // Désactivé
}
```

---

*Fin de la documentation — SIRH EY v1.0*  
*Générée le 15 juin 2026 par analyse automatisée du code source*
