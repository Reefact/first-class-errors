# Workflow `release-dryrun`

🌍 🇬🇧 [English](release-dryrun.en.md) · 🇫🇷 Français (ce fichier)

> Documentation mainteneur — fait partie de la [référence des workflows](README.fr.md).
> Ne fait pas partie de la documentation utilisateur sous `doc/`.

**Fichier du workflow :** [`.github/workflows/release-dryrun.yml`](../../../../.github/workflows/release-dryrun.yml)

## À quoi il sert

`release-dryrun` répète en continu la portion **sans effet de bord** de la
release — build, pack et embarquement du SBOM pour les deux projets publiés — sur
chaque push et pull request. Comme [`release`](release.fr.md) ne tourne lui-même
que sur un tag ou un dispatch manuel, son chemin d'empaquetage serait autrement
exercé pour la première fois en production, sur un tag, une seule fois. Celui-ci
attrape les régressions d'empaquetage/SBOM dans la CI ordinaire à la place.

C'est le pendant automatique du dry run **manuel** documenté dans
[Répétition de release à blanc (« dry run » manuel)](../ReleaseDryRun.fr.md) :
celui-ci laisse volontairement de côté le chemin attestation/OIDC (qui a des
effets de bord), le manuel l'ajoute.

## Quand il s'exécute

- À chaque **push sur `main`**, **pull request visant `main`**, et à la demande
  via **`workflow_dispatch`**.

## Comment il s'exécute

Un seul job, `pack` : checkout → setup du SDK de release (10.0.x) →
`dotnet build` → **`tools/packaging/pack.sh`**, qui packe les deux projets
publiés avec leur SBOM et assert que le SBOM est bien embarqué.

## Permissions & sécurité

`contents: read` seulement. Il s'arrête avant chaque étape qui a un effet de
bord, donc il n'a besoin d'aucun des périmètres en écriture de `release` :

- **pas d'attestation de provenance** — cela écrit un enregistrement public
  permanent (Sigstore/Rekor + le magasin d'attestations) ; réservé au dry run
  manuel et aux vraies releases ;
- **pas de login / push NuGet** — nuget.org n'a pas de « push à blanc », donc la
  publication reste réservée à la release ;
- **pas de GitHub Release** — aucun tag ni release n'est jamais créé ici.

## À manipuler avec précaution

- **Il partage `tools/packaging/pack.sh` avec `release`, et c'est le but.** Il
  existe exactement une définition de « packer les artefacts de release », donc
  cette répétition ne peut pas diverger de la vraie release. N'écrivez pas une
  commande de pack séparée ici — changez `pack.sh` et les deux suivent.
- **Gardez-le sans effet de bord.** L'intérêt de ce workflow est qu'il tourne sur
  *chaque* PR sans attestation ni publication. N'ajoutez pas ici l'attestation ni
  une étape de login ; celles-ci appartiennent au dry run manuel de `release`,
  qui tourne délibérément, pas à chaque push.
- **Le `DRYRUN_VERSION` est jetable.** Rien n'est publié, donc la valeur exacte
  doit seulement être un SemVer valide que le pack accepte. La vraie version vient
  du tag et est validée dans `release`.
- **La contribution unique de ce job est l'*empaquetage*.** Les tests
  unitaires/intégration tournent dans [`ci`](ci.fr.md) ; ne les dupliquez pas ici.

## En rapport

- [`release`](release.fr.md) — le vrai chemin de publication, partageant le même
  `pack.sh`.
- [Répétition de release à blanc (« dry run » manuel)](../ReleaseDryRun.fr.md) —
  le dry run manuel qui répète en plus le chemin attestation/OIDC.
