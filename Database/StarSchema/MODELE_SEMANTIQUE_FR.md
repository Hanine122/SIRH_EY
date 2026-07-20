# SIRH.EY — Modèle sémantique Power BI (français)

Construit sur `dim.*` / `fact.*` (schémas techniques en anglais, contenu 100% français). Import direct des tables, ou via `analytics.vw_Competences` / `vw_Formations` / `vw_EvaluationsTalent` / `vw_Promotions` / `vw_Successions` pour un import déjà dénormalisé.

---

## Dimensions (noms d'affichage Power BI)

| Table Power BI | Source SQL | Clé | Notes |
|---|---|---|---|
| **Calendrier** | `dim.Calendrier` | `Date` | Année/Trimestre/Mois/Jour. Généré 2020–2035. |
| **Collaborateur** | `dim.Collaborateur` | `CollaborateurId` | SCD Type 2 : `DateEffective`/`DateFin`/`EstVersionCourante` conservent l'historique de grade/poste/service. |
| **Collaborateur (Candidat)** | copie à jeu de rôles de **Collaborateur** | `CollaborateurId` | Nécessaire : `Fait Succession` a deux rôles (titulaire et candidat) sur la même dimension ; Power BI n'autorise qu'une relation *active* entre deux tables. Importer une seconde copie renommée « Collaborateur (Candidat) », comme pour le modèle existant sur `stg`. |
| **Organisation** | `dim.Organisation` | `PositionId` | Poste/Sous-département/Département aplatis. |
| **Grade** | `dim.Grade` | `GradeId` | Nom, Niveau, seuils de promotion. |
| **Unité Affaires** | `dim.UniteAffaires` | `UniteAffairesId` | |
| **Localisation** | `dim.Localisation` | `LocalisationId` | |
| **Compétence** | `dim.Competence` | `CompetenceId` | Catalogue des compétences (`stg.Skills`), catégorie et criticité. |
| **Formation** | `dim.Formation` | `FormationId` | |
| **Cycle Évaluation** | `dim.CycleEvaluation` | `CycleEvaluationId` | |

## Tables de faits

| Table Power BI | Source SQL | Grain |
|---|---|---|
| **Évaluation Compétences** | `fact.EvaluationCompetences` | Collaborateur × Compétence évaluée |
| **Formation** | `fact.Formation` | Inscription |
| **Évaluation Talent** | `fact.EvaluationTalent` | Collaborateur × Cycle d'évaluation |
| **Promotion** | `fact.Promotion` | Collaborateur actif (photo courante, datée par `CleDateSnapshot`) |
| **Succession** | `fact.Succession` | Plan de succession × candidat classé |

## Relations

Toutes les relations sont **à sens unique** (Dimension → Fait), sauf mention contraire, pour garder la propagation de filtre prévisible.

| De | Vers | Cardinalité | Active |
|---|---|---|---|
| Collaborateur[CollaborateurId] | Évaluation Compétences[CollaborateurId] | 1:* | Oui |
| Collaborateur[CollaborateurId] | Formation[CollaborateurId] | 1:* | Oui |
| Collaborateur[CollaborateurId] | Évaluation Talent[CollaborateurId] | 1:* | Oui |
| Collaborateur[CollaborateurId] | Promotion[CollaborateurId] | 1:1 | Oui |
| Collaborateur[CollaborateurId] | Succession[IdCollaborateurTitulaire] | 1:* | Oui |
| Collaborateur (Candidat)[CollaborateurId] | Succession[IdCollaborateurCandidat] | 1:* | Oui |
| Organisation[PositionId] | Collaborateur[PositionId] | 1:* | Oui |
| Grade[GradeId] | Collaborateur[GradeId] | 1:* | Oui |
| Unité Affaires[UniteAffairesId] | Collaborateur[UniteAffairesId] | 1:* | Oui |
| Localisation[LocalisationId] | Collaborateur[LocalisationId] | 1:* | Oui |
| Compétence[CompetenceId] | Évaluation Compétences[NomCompetence] | 1:* | Oui |
| Formation[FormationId] | Formation (fait)[TitreFormation] | 1:* | Oui |
| Cycle Évaluation[CycleEvaluationId] | Évaluation Talent[NomCycleEvaluation] | 1:* | Oui |
| Calendrier[Date] | Évaluation Compétences[DateEvaluation] | 1:* | Oui |
| Calendrier[Date] | Formation[DateInscription] | 1:* | Oui |
| Calendrier[Date] | Formation[DateCompletion] | 1:* | **Non** — inactive, activer par mesure avec `USERELATIONSHIP` pour une analyse « par date de complétion ». |
| Calendrier[Date] | Évaluation Talent[DateEvaluation] | 1:* | Oui |
| Calendrier[Date] | Promotion[DateSnapshot] | 1:* | Oui |
| Calendrier[Date] | Succession[DateSnapshot] | 1:* | Oui |
| Calendrier[Date] | Succession[DateCreation] | 1:* | **Non** — même logique. |

## Mesures DAX (français)

### Compétences

```
Nombre Évaluations Compétences := COUNTROWS ( 'Évaluation Compétences' )

Compétences Atteintes := CALCULATE ( COUNTROWS ( 'Évaluation Compétences' ), 'Évaluation Compétences'[AtteintCible] = 1 )

Taux Couverture Compétences % := DIVIDE ( [Compétences Atteintes], [Nombre Évaluations Compétences] )

Écarts Critiques := CALCULATE ( COUNTROWS ( 'Évaluation Compétences' ), 'Évaluation Compétences'[SeveriteEcart] = "Critique" )

Écart Moyen := AVERAGE ( 'Évaluation Compétences'[Ecart] )
```

### Formation

```
Nombre Inscriptions := COUNTROWS ( 'Formation' )

Formations Terminées := CALCULATE ( COUNTROWS ( 'Formation' ), 'Formation'[Terminee] = 1 )

Taux Complétion Formation % := DIVIDE ( [Formations Terminées], [Nombre Inscriptions] )

Note Satisfaction Moyenne := AVERAGE ( 'Formation'[NoteGlobaleChaud] )

Taux Utilisation Capacité % :=
AVERAGEX (
    SUMMARIZE ( 'Formation', 'Formation'[TitreFormation] ),
    CALCULATE ( AVERAGE ( 'Formation'[TauxUtilisationCapacite] ) )
)
```
`Taux Utilisation Capacité %` est moyenné par formation distincte, pas par inscription — `TauxUtilisationCapacite` est un attribut de la formation, répété sur chaque ligne d'inscription (cf. `RAPPORT_AUDIT.md`, section redondance) ; une moyenne brute par ligne sur-pondérerait les formations les plus suivies.

### Talent

```
Collaborateurs Évalués := DISTINCTCOUNT ( 'Évaluation Talent'[CollaborateurId] )

Taux Couverture Revue % :=
DIVIDE ( [Collaborateurs Évalués], CALCULATE ( COUNTROWS ( 'Collaborateur' ), 'Collaborateur'[Actif] = TRUE ) )

Taux Haut Potentiel % :=
DIVIDE (
    CALCULATE ( COUNTROWS ( 'Évaluation Talent' ), 'Évaluation Talent'[Code9Boites] IN { 1, 4 } ),
    COUNTROWS ( 'Évaluation Talent' )
)

Taux Réussite OKR % := DIVIDE ( SUM ( 'Évaluation Talent'[OKRTermines] ), SUM ( 'Évaluation Talent'[TotalOKR] ) )
```

### Promotion

```
Nombre Prêts Promotion := CALCULATE ( COUNTROWS ( 'Promotion' ), 'Promotion'[BandeEligibilite] = "Pret" )

Taux Prêts Promotion % := DIVIDE ( [Nombre Prêts Promotion], COUNTROWS ( 'Promotion' ) )

Atteinte Compétences Moyenne := AVERAGE ( 'Promotion'[TauxAtteinteCompetences] )
```

### Succession

```
Nombre Plans Succession := DISTINCTCOUNT ( 'Succession'[SuccessionPlanId] )

Postes Sans Successeur :=
CALCULATE (
    DISTINCTCOUNT ( 'Succession'[SuccessionPlanId] ),
    ISBLANK ( 'Succession'[IdSnapshot] )
)

Taux Couverture Succession % := DIVIDE ( [Nombre Plans Succession] - [Postes Sans Successeur], [Nombre Plans Succession] )
```

### Transversal

```
Effectif Actif := CALCULATE ( COUNTROWS ( 'Collaborateur' ), 'Collaborateur'[Actif] = TRUE )
```

---

## Notes de conformité (voir `RAPPORT_AUDIT.md` pour le détail)

- `Promotion` porte désormais une date de photo (`DateSnapshot`, via `Calendrier`) — un fait périodique Kimball doit toujours être daté.
- Simplification assumée : chaque fait est lié à la version **courante** du Collaborateur, pas à sa version en vigueur à la date du fait (SCD2 non exploité *point-in-time* sur les faits).
