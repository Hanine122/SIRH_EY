# Rapport d'audit — Star Schema SIRH.EY (avant exécution)

Portée : `Database/StarSchema/001_Staging.sql` à `005_Seed.sql`, tels qu'écrits avant cette revue. Aucune exécution n'a eu lieu entre l'écriture initiale et cet audit.

## 1. Conformité Kimball

| Point vérifié | Constat | Action |
|---|---|---|
| Grain déclaré par table de faits | Les 5 faits ont un grain unique et documenté (1 évaluation, 1 inscription, 1 revue talent, 1 collaborateur actif, 1 plan × candidat) | Conforme, aucune action |
| Clés de substitution (surrogate keys) IDENTITY sur chaque dimension | Présentes partout, clé naturelle (NK) conservée en colonne + contrainte UNIQUE | Conforme |
| Fait périodique (`Promotion`) sans date de photo (snapshot date) | **Violation** : un « periodic snapshot fact » Kimball doit porter une date de photo. `fact.Promotion` n'avait aucune colonne de date | **Corrigé** : ajout d'une clé de date de snapshot (date de génération du calcul), référencée vers la dimension calendrier |
| Dimension à jeu de rôles (*role-playing*) sur `Succession` (titulaire / candidat) | Une seule table `dim.Collaborateur` référencée deux fois par deux FK distinctes — pattern Kimball standard, mais nécessite en Power BI soit deux imports du même import (duplication), soit une relation inactive + `USERELATIONSHIP` | Documenté dans le modèle sémantique français ; aucune correction SQL nécessaire, c'est le comportement attendu |
| Outriggers joints par clé métier plutôt que clé de substitution (`Organisation`, `Grade`, `UniteAffaires`, `Localisation` reliés à `Collaborateur` via `PositionId`/`GradeId`/etc., pas via FK physique) | Choix intentionnel, déjà présent et validé dans le modèle sémantique Power BI existant (jointures Power Query sur clé métier) | Conservé tel quel, documenté explicitement plutôt que silencieusement absent |
| SCD Type 2 sur `dim.Collaborateur` | Logique de fermeture/ouverture de version correcte, mais aucune contrainte n'empêchait un état incohérent (`IsCurrent = 1` avec `EffectiveTo` renseignée, ou l'inverse) | **Corrigé** : ajout d'une contrainte `CHECK` de cohérence SCD2 |

## 2. Redondance et tables inutiles

- Aucune table physique redondante ou inutile identifiée : les 9 dimensions et 5 faits correspondent chacun à un besoin analytique réel déjà validé dans `PowerBI_SemanticModel.md`.
- Deux redondances **de colonnes** (pas de tables) héritées du modèle existant, assumées et documentées plutôt que corrigées (cohérence avec les vues `analytics.vw_Talent`/`vw_Training` déjà auditées) :
  - `TotalOKR`/`OKRTermines` répétés sur chaque ligne de `fait.EvaluationTalent` (agrégat par collaborateur dupliqué sur toutes ses évaluations).
  - `TauxUtilisationCapacite` répété sur chaque ligne de `fait.Formation` (attribut de la formation, pas de l'inscription).
  - Corriger proprement demanderait une table de faits supplémentaire (OKR) hors du périmètre des 5 faits déjà validés — signalé, non traité pour rester dans le périmètre demandé.

## 3. Relations

- Toutes les FK vérifiées pointent dans le bon sens (dimension ← fait), avec la bonne cardinalité (1:N) et la bonne nullabilité (FK nullable uniquement quand la donnée source l'est réellement, ex. `CompetenceKey` sur `fait.EvaluationCompetences` quand `SkillId` est NULL côté source).
- Aucune relation incorrecte ou orpheline détectée dans la conception. Le script `005_Seed.sql` inclut déjà une vérification d'orphelins post-chargement (0 attendu).

## 4. Performance

- **Manque identifié** : aucun index non-clusterisé sur les colonnes de clé étrangère des tables de faits (SQL Server n'indexe jamais automatiquement les colonnes FK, contrairement à la PK). **Corrigé** : un index par colonne FK sur chacune des 5 tables de faits.
- **Arbitrage documenté, non corrigé** : les procédures de rechargement des dimensions SCD1 utilisent `DELETE` plutôt que `TRUNCATE` (nécessaire tant que la table est référencée par une FK active). Sur des volumes de quelques dizaines à centaines de lignes (échelle d'un PFE), l'écart de performance est négligeable ; la seule vraie conséquence est une croissance non bornée des clés IDENTITY au fil des rechargements successifs — sans impact pratique à cette échelle. Un design `DROP FK → TRUNCATE → RECREATE FK` resterait possible si le volume de données devait un jour justifier ce niveau de complexité.

## 5. Francisation (portée de cette révision)

- Schémas techniques (`dbo`, `stg`, `dim`, `fact`, `analytics`) : conservés en anglais, conformément à la consigne.
- Renommage complet en français : tables `dim.*`/`fait.*` (ex. `dim.Skill` → `dim.Competence`, `dim.BusinessUnit` → `dim.UniteAffaires`, `dim.Location` → `dim.Localisation`, `dim.ReviewCycle` → `dim.CycleEvaluation`, `fact.Skills` → `fait.EvaluationCompetences`, `fact.Training` → `fait.Formation`, `fact.Talent` → `fait.EvaluationTalent`), colonnes, vues `analytics.vw_*`, alias SQL, procédures stockées, commentaires.
- Les 5 vues `analytics.vw_*` reconstruites sur `dim`/`fait` portent des noms distincts des 6 vues existantes (`vw_Skills`, `vw_Training`, etc., construites sur `stg` et non touchées) : `vw_Competences`, `vw_Formations`, `vw_EvaluationsTalent`, `vw_Promotions`, `vw_Successions` — pluriel ou terme différent, donc aucune collision, et l'existant reste intact conformément à la règle « ne jamais recréer l'existant ».
- Un document `MODELE_SEMANTIQUE_FR.md` (dimensions, faits, relations, mesures DAX en français, noms d'affichage Power BI) accompagne le star schema francisé, sur le modèle de `PowerBI_SemanticModel.md` existant (resté en anglais, non modifié, car il documente le modèle stg existant et non touché).

## 6bis. Audit final (avant exécution, sur la version francisée)

| Problème | Gravité | Correction |
|---|---|---|
| `dim.usp_ChargerCalendrier` : CTE récursive sur 2020-01-01→2035-12-31 (5843 récursions) avec `OPTION (MAXRECURSION 5000)` | **Bloquant** — SQL Server lève l'erreur 530, l'INSERT échoue, `dim.Calendrier` reste vide et tout l'ETL faits plante par violation de FK en cascade | `MAXRECURSION 0` (illimité) |
| 4 procédures `fact.usp_Charger*` calculaient `CleDate`/`CleDateInscription`/`CleDateCreation`/`CleDateSnapshot` sans vérifier leur existence dans `dim.Calendrier` | Latent — fonctionne aujourd'hui (données réelles dans la plage), mais toute date hors plage ferait échouer tout l'INSERT par violation de FK au lieu de n'exclure que la ligne concernée | Jointure (`JOIN` pour les FK NOT NULL, `LEFT JOIN` pour les FK nullable) contre `dim.Calendrier` ajoutée dans les 4 procédures |

Aucun autre problème détecté sur : intégrité référentielle des autres relations, conformité Kimball, index PK/FK, doublons, cohérence des vues `analytics.vw_*`, compatibilité SQL Server (`CREATE OR ALTER`, `DROP ... IF EXISTS`, `STRING_AGG` — toutes supportées par l'instance cible), compatibilité Power BI (types de clés, cardinalités, nommage).

## 6. Décision : `fact` devient `fait` ?

Les schémas techniques doivent rester en anglais (`dim`, `fact`, `analytics`...), mais les **noms de tables** doivent être français. Pour éviter toute ambiguïté, ce rapport et les scripts qui suivent utilisent `fact.<NomFrançaisDeTable>` (schéma anglais, table française) — ex. `fact.EvaluationCompetences`, pas `fait.EvaluationCompetences` — conformément à la consigne qui autorise explicitement `fact` en anglais.
