# Architecture RH cible

## Objectif

Garder l'application ASP.NET MVC maintenable sans casser les vues Razor ni les routes existantes. Les controleurs restent responsables de l'orchestration HTTP, tandis que les regles RH et les lectures de referentiel sont deplacees dans des services applicatifs.

## Decoupage recommande

- `Controllers` : actions MVC, validation HTTP, redirections, JSON endpoints.
- `Services` : cas d'usage RH et regles applicatives.
- `Models` : entites EF Core et view models existants.
- `Views` : Razor + Bootstrap, sans logique metier lourde.
- `wwwroot/js` : comportements AJAX reutilisables.

## Services applicatifs

- `IReferentielRhService` : departements, postes, competences requises par grade, categories.
- `IPlanDeveloppementService` : generation des recommandations de formation selon les ecarts de competence.
- A ajouter ensuite : `IEvaluationCompetenceService` pour auto-evaluation, validation manager, seuil RH et historique.

## Flux cibles

### Evaluations de competences

1. Le collaborateur saisit une auto-evaluation.
2. Le manager valide ou ajuste.
3. Le service d'evaluation met a jour `Competence.NiveauActuel`.
4. Un historique RH est cree pour audit et reporting.

### Parcours de formation

1. Les ecarts `NiveauActuel < NiveauCible` sont detectes.
2. Le plan de developpement propose les formations liees a `CompetenceVisee`.
3. Les inscriptions suivent leur progression et leur statut.
4. La formation terminee peut augmenter la competence visee.

### Competences requises

L'entite existante `CompetenceRequiseParPoste` reste le referentiel principal. La relation grade -> competences est derivee du `NiveauRequis` et du niveau cible du grade pour eviter une migration lourde.

## Extensions futures

- Ajouter `EvaluationHistorique` comme journal d'audit systematique.
- Ajouter une table `GradeCompetenceRequise` si la relation grade devient autonome.
- Ajouter des projections/read models pour matrices de competences volumineuses.
- Ajouter des politiques d'autorisation par role sur les cas d'usage manager/RH.
