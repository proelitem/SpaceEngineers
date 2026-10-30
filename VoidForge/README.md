# VoidForge VR

Prototype autonome pour **Meta Quest 3** (Android, sans PC), inspiré du gameplay de Space Engineers :
construction de vaisseaux à blocs, vol en apesanteur, et **multijoueur**. Écrit en **Unity 6.3 LTS**
avec OpenXR, URP et Netcode for GameObjects.

> Ce dossier est un projet indépendant sous licence MIT. Il ne contient **aucun** code, modèle,
> texture ni son de Space Engineers ou de Keen Software House (voir `CREDITS.md`). Il vit pour
> l'instant dans le dépôt `proelitem/SpaceEngineers` par commodité ; il est prévu de le déplacer
> dans son propre dépôt, car le reste de ce dépôt est soumis à l'EULA de Keen.

## État : jalon J1 livré (non testé en casque)

- Projet Unity configuré **entièrement en code** (menu *VoidForge* dans l'éditeur) : Android ARM64,
  IL2CPP, Vulkan, OpenXR avec Meta Quest Support, URP réglé pour le Quest 3.
- Scène générée : champ d'étoiles, astéroïdes, rig joueur VR.
- Locomotion en apesanteur : poussée dans la direction de la main gauche, poussée verticale,
  freinage au grip, rotation par crans de 30°.
- Base réseau : hôte/client sur réseau local, avatars tête + mains synchronisés.
- Pipeline APK par menu ou ligne de commande (`Build/build-quest3.sh`, `.ps1`), script d'installation `adb`.

Voir `docs/TEST_J1.md` pour la liste des tests à faire et les limites connues.

## Démarrage rapide

1. Installer Unity 6.3 LTS avec les modules Android (Unity Hub).
2. Ouvrir le dossier `VoidForge/` dans Unity.
3. Menu **VoidForge > 0. Tout faire** (configure, construit la scène, produit l'APK dans `Builds/`).
4. `./Build/install-quest3.sh` avec le Quest 3 branché en mode développeur.

Détails, contrôles et dépannage : `docs/BUILD_QUEST3.md`.

## Documentation

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
