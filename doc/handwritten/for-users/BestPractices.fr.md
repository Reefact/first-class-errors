# Bonnes pratiques

🌍 **Langues :**  
🇬🇧 [English](./BestPractices.en.md) | 🇫🇷 Français (ce fichier)

Utilisez cette page comme une checklist de revue compacte. Les explications détaillées se trouvent dans les guides spécialisés liés depuis chaque section.

## Modéliser la bonne situation

- Une factory représente une situation d’erreur précise.
- Évitez les erreurs génériques comme `INVALID_OPERATION` ou `PROCESSING_FAILED`.
- Choisissez le type d’erreur selon le sens de l’échec, pas selon la classe ou le dossier courant.
- Ne documentez pas les exceptions du framework, les crashes accidentels ou le bruit technique bas niveau comme de la connaissance applicative.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md) et [Quand ne pas utiliser FirstClassErrors](WhenNotToUseFirstClassErrors.fr.md).

## Garder les identifiants stables

- Utilisez un code spécifique en `UPPER_SNAKE_CASE`.
- Ne réutilisez jamais un code pour une autre situation.
- Ne renommez ou ne supprimez pas un code à la légère.
- Gardez stables les noms et types des clés de contexte lorsque des dashboards ou consommateurs en dépendent.

Les codes et la forme du contexte constituent un contrat opérationnel. Utilisez le [Versionnage du catalogue](CatalogVersioning.fr.md) pour rendre ses modifications visibles.

## Centraliser la construction dans des factories

Préférez :

```csharp
throw InvalidAmountOperationError.CurrencyMismatch(left, right).ToException();
```

N’assemblez pas les erreurs ou exceptions directement dans la logique métier.

Une classe factory statique annotée avec `[ProvidesErrorsFor(...)]` doit regrouper les situations liées, avec une méthode par situation et sa documentation à proximité.

Cela garde le happy path lisible et garantit à chaque occurrence les mêmes code, messages, contexte et point d’ancrage documentaire.

## Séparer documentation stable et messages runtime

La documentation stable explique la catégorie d’erreur :

- titre ;
- description ;
- règle violée ;
- hypothèses de diagnostic ;
- exemples représentatifs.

Les messages runtime expliquent une occurrence :

- `ShortMessage` : résumé public sûr ;
- `DetailedMessage` : détail public optionnel et maîtrisé ;
- `DiagnosticMessage` : détail de diagnostic interne.

Ne placez pas d’identifiants propres à une occurrence dans la documentation stable et n’exposez pas de détail interne dans les messages publics.

Voir [Écrire la documentation d’une erreur](WritingErrorsGuide.fr.md) et [Écrire les messages d’une erreur](WritingErrorMessages.fr.md).

## Écrire pour l’investigation, pas pour accuser

- Les causes décrivent des états ou conditions plausibles.
- `ErrorOrigin` classe l’endroit où une cause peut se situer ; il n’attribue pas une responsabilité.
- Les pistes d’analyse commencent par des verbes neutres comme *Vérifier*, *Contrôler* ou *Examiner*.
- N’encodez pas les procédures de ticketing, d’escalade ou de contact d’équipe dans la documentation.

Les processus opérationnels évoluent indépendamment du comportement applicatif.

## Garder le contexte utile et sûr

- Ajoutez le contexte au niveau de la factory afin que chaque occurrence soit cohérente.
- Utilisez des clés nommées, typées et réutilisables.
- Incluez les faits propres à l’occurrence qui améliorent réellement le diagnostic.
- Évitez les secrets, les payloads volumineux et les données qui ne peuvent pas être loguées sans risque.
- Préférez un contexte structuré à l’insertion de toutes les valeurs dans un message.

Voir [Contexte d’erreur](ErrorContext.fr.md).

## Choisir intentionnellement exception ou `Outcome`

Utilisez une exception lorsque l’échec doit interrompre l’opération courante, par exemple pour une violation d’invariant ou un état irrécupérable à ce niveau.

Utilisez `Outcome` / `Outcome<T>` lorsque l’échec est une branche attendue du flux, par exemple pour la validation, le parsing, les traitements par lots ou les succès partiels.

Les deux chemins doivent porter la même `Error`, créée par la même factory. Ne créez pas un second modèle d’erreur appauvri pour les flux sans exception.

Voir [Cas d’usage](UsagePatterns.fr.md).

## Rendre les exemples pédagogiques

- Utilisez des valeurs simples et réalistes.
- Rendez la règle violée immédiatement visible.
- Appelez la factory documentée au lieu de recopier un message.
- Gardez les cas limites et de stress dans les tests, pas dans les exemples du catalogue.

## Checklist de pull request

Avant de merger une modification liée aux erreurs, vérifiez que :

- [ ] chaque nouvelle factory représente une situation précise ;
- [ ] chaque code est spécifique, stable et unique ;
- [ ] la documentation est liée avec `[DocumentedBy]` ;
- [ ] les messages publics ne contiennent aucune information interne ou sensible ;
- [ ] les messages de diagnostic expliquent les occurrences concrètes ;
- [ ] les données interrogeables utilisent un contexte typé lorsque pertinent ;
- [ ] les diagnostics sont des hypothèses et les pistes d’analyse sont actionnables ;
- [ ] les exemples sont réalistes et appellent la vraie factory ;
- [ ] les documentations anglaise et française restent alignées lorsqu’elles sont modifiées ;
- [ ] les modifications de baseline du catalogue (le snapshot de catalogue commité — voir [Versionnage du catalogue](CatalogVersioning.fr.md)) sont délibérées et relues lorsque le contrat évolue.

---

<div align="center">
<a href="UsagePatterns.fr.md">← Cas d’usage</a> · <a href="README.fr.md#-étapes-suivantes">↑ Table des matières</a> · <a href="Testing.fr.md">Guide des tests →</a>
</div>

---