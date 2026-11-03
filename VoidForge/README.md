# VoidForge VR

Prototype autonome pour **Meta Quest 3** (Android, sans PC), inspiré du gameplay de Space Engineers :
construction de vaisseaux à blocs, vol en apesanteur, et **multijoueur**. Écrit en **Unity 6**
avec OpenXR, URP et Netcode for GameObjects. L'éditeur retenu pour la recette locale J1 est
**Unity 6000.4.0f1**, autorisé par Pierre le 2 novembre 2026.

> Ce dossier est un projet indépendant sous licence MIT. Il ne contient **aucun** code, modèle,
> texture ni son de Space Engineers ou de Keen Software House (voir `CREDITS.md`). Il vit pour
> l'instant dans le dépôt `proelitem/SpaceEngineers` par commodité ; il est prévu de le déplacer
> dans son propre dépôt, car le reste de ce dépôt est soumis à l'EULA de Keen.

## État : APK J1 construit, installé et lancé sur Quest 3

Recette des 2 et 3 novembre 2026 : packages résolus, compilation C# réussie, configuration
XR et scène générées. L'APK final développement `Builds/VoidForge-Quest3-dev.apk`
(versionCode 6) est installé sur le Quest 3 ; lancement et capture de 30 secondes
sans exception Unity ni erreur AndroidRuntime vérifiés. OpenXR atteint FOCUSED puis
le casque entre en veille : ce test ne valide pas une session de jeu continue.
Pierre a validé tous les contrôles J1, la locomotion, le confort et les performances
en casque le 3 novembre 2026. Les mesures chiffrées et le test réseau à plusieurs appareils
restent à consigner. Le test Play relève toujours l'exception interne à l'indexeur Search de Unity.

- Projet Unity configuré **entièrement en code** (menu *VoidForge* dans l'éditeur) : Android ARM64,
  IL2CPP, Vulkan, OpenXR avec Meta Quest Support, URP réglé pour le Quest 3.
- Scène générée : champ d'étoiles, astéroïdes, rig joueur VR.
- Locomotion en apesanteur : poussée dans la direction de la main gauche, poussée verticale,
  freinage au grip, rotation par crans de 30°.
- Base réseau : hôte/client sur réseau local, avatars tête + mains synchronisés.
- Pipeline APK par menu ou ligne de commande (`Build/build-quest3.sh`, `.ps1`), script d'installation `adb`.

Voir `docs/TEST_J1.md` pour la liste des tests à faire et les limites connues.

Extension demandée par Pierre le 3 novembre : réutilisation de `G:/UnityAssets`,
astéroïdes Dark Anvil avec trois LOD et prototype de creusage solo Digger PRO.
APK `Builds/VoidForge-Quest3-assets-dev.apk` (code 8) construit, installé et lancé sur Quest :
test d'excavation/collider réussi (+0,649 m), capture de 30,38 s sans exception.
Gâchette droite à moins de 8 m pour creuser le volume marqué ; contrôle et visuel à valider en casque.
Installation locale, contrôles et limites : [`docs/UNITY_ASSETS.md`](docs/UNITY_ASSETS.md).
Les ressources Asset Store restent sous leurs licences et sont exclues de Git ; le code
propre à VoidForge reste MIT. La validation de cette extension est suivie séparément
dans [`Hermes_Works/Jalons/J1_ASSETS.md`](Hermes_Works/Jalons/J1_ASSETS.md).

## Démarrage rapide

1. Installer Unity 6000.4.0f1 avec les modules Android (Unity Hub).
2. Ouvrir le dossier `VoidForge/` dans Unity.
3. Menu **VoidForge > 0. Tout faire** (configure, construit la scène, produit l'APK dans `Builds/`).
4. `./Build/install-quest3.sh` avec le Quest 3 branché en mode développeur.

Détails, contrôles et dépannage : `docs/BUILD_QUEST3.md`.

## Documentation

Index Hermes Works : [`Hermes_Works/README.md`](Hermes_Works/README.md) (architecture,
jalons, recette et gouvernance ; sources canoniques conservées dans ce dépôt).

| Fichier | Contenu |
|---------|---------|
| `docs/ARCHITECTURE.md` | Pile technique, modules, rig joueur, conception multijoueur, budget de performance |
| `docs/ROADMAP.md` | Jalons J1 à J5 (gameplay) et M0 à M4 (multijoueur) |
| `docs/BUILD_QUEST3.md` | Prérequis, build, installation, contrôles, dépannage |
| `docs/TEST_J1.md` | Ce qui est vérifié, ce qui reste à tester, limites et risques du J1 |
| `CREDITS.md` | Provenance et licence de chaque ressource |

## Arborescence

```
VoidForge/
├── Assets/VoidForge/Scripts/   code runtime (Core, Player, World, Net, UI)
├── Assets/VoidForge/Editor/    outils : configuration Quest 3, scène, build APK
├── Assets/Scenes/              Main.unity (généré par l'outil 2)
├── Build/                      scripts de build et d'installation
├── Packages/manifest.json      dépendances Unity
├── ProjectSettings/            version de l'éditeur (le reste est généré puis appliqué par l'outil 1)
└── docs/                       documentation en français
```
