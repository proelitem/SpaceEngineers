# Architecture de VoidForge VR

Projet Unity autonome (dossier `VoidForge/`), sans aucun code ni asset de Keen Software House.
Il s'inspire du gameplay de Space Engineers : construction de vaisseaux à blocs, vol en
apesanteur, et, en plus du rapport initial, **multijoueur** dès la conception.

## Pile technique

| Couche | Choix | Pourquoi |
|--------|-------|----------|
| Moteur | Unity 6000.4.0f1, URP | Version installée et retenue par Pierre pour le J1 le 2 novembre 2026 ; remplace la cible initiale 6000.3.x |
| VR | OpenXR Plugin 1.15 + fonctionnalité Meta Quest Support, XR Interaction Toolkit 3 | Pile recommandée par Meta ; pas de SDK propriétaire obligatoire |
| Entrées | Input System (actions créées en code) | Reproductible sans asset `.inputactions` |
| Réseau | Netcode for GameObjects 2 + Unity Transport | Officiel, gratuit, hôte sur casque possible, relais ajoutable plus tard |
| Cible | Android ARM64, IL2CPP, Vulkan, minSdk 32 | Exigences Quest 3 |

## Principe : tout en code

Aucune scène, prefab ou réglage n'est fait à la main. Trois outils d'éditeur (menu **VoidForge**)
génèrent tout, ce qui permet à un agent sans éditeur Unity de livrer du code vérifiable :

1. `Quest3ProjectConfigurator` : PlayerSettings Android, Input System, URP, XR Management, OpenXR, matériaux.
2. `MainSceneBuilder` : scène `Assets/Scenes/Main.unity`, rig joueur, monde, `NetworkManager`, prefab d'avatar.
3. `QuestBuildPipeline` : APK (menu ou ligne de commande via `Build/build-quest3.sh` / `.ps1`).

## Modules (`Assets/VoidForge/Scripts`)

```
Core/     GameBootstrap            point d'entrée, rig de secours
Player/   XRInputActions           actions Input System (sticks, grips, boutons)
          PlayerInputProvider      partage des actions entre composants
          PlayerRigFactory         construit le rig XR (XROrigin, caméra, mains)
          ZeroGLocomotion          poussée 6 axes, freinage, vitesse max (Rigidbody sans gravité)
          SnapTurn                 rotation par crans autour de la tête
          HandVisual               visuel minimal des manettes
World/    MaterialLibrary          matériaux dans Resources, repli en mémoire
          Starfield                champ d'étoiles procédural suivant la caméra
          AsteroidField            astéroïdes décoratifs à partir d'une graine
Net/      NetworkLauncher          héberger / rejoindre / quitter (manettes ou clavier)
          NetworkPlayerAvatar      tête + mains synchronisées (variable réseau propriétaire)
UI/       DebugHud                 i/s, vitesse, état réseau (texte 3D)
```

Modules prévus (jalons suivants) : `Building/` (grille de blocs, placement, fantôme),
`Ship/` (grille → corps rigide, masse, propulseurs, cockpit), `Save/` (JSON), `Net/` étendu.

## Hiérarchie du rig joueur

```
PlayerRig                Rigidbody (sans gravité, rotation gelée), SphereCollider,
                         PlayerInputProvider, ZeroGLocomotion, SnapTurn
└─ XR Origin             XROrigin, mode « Device » (tête à l'origine du rig)
   └─ Camera Offset
      ├─ Main Camera     Camera, AudioListener, TrackedPoseDriver (<XRHMD>)
      │  └─ DebugHud     TextMesh
      ├─ LeftHand        TrackedPoseDriver (<XRController>{LeftHand}), HandVisual
      └─ RightHand       TrackedPoseDriver (<XRController>{RightHand}), HandVisual
```

Le mode de suivi « Device » plutôt que « Floor » est volontaire : en apesanteur il n'y a pas de sol,
et la position de la tête doit coïncider avec le centre du corps physique.

## Conception multijoueur

**Modèle retenu** : autorité serveur, un joueur héberge (host) sur son casque ou un PC, les autres
rejoignent par adresse IP sur le réseau local. C'est suffisant pour tester à deux ou trois.

**Ce qui est synchronisé au J1** : poses tête et mains de chaque joueur (30 Hz, interpolées côté
récepteur). Le joueur ne voit pas son propre avatar.

**Évolutions prévues** (voir `ROADMAP.md`, jalons M1 à M3) :

- M1 : rig local = objet réseau avec autorité propriétaire sur la position (locomotion restant locale, sans latence).
- M2 : grille de blocs répliquée. L'état d'une grille est une liste de blocs (position entière,
  type, orientation) ; les placements passent par des RPC serveur, le serveur valide et diffuse.
  Les mailles sont régénérées localement, jamais envoyées.
- M3 : vaisseaux pilotés : le pilote a l'autorité de prédiction, le serveur réconcilie
  (NetworkTransform ou NetworkRigidbody selon les mesures).
- Plus tard : relais (Unity Relay ou serveur dédié Linux) pour jouer hors réseau local, voix.

Hors périmètre du MVP : voxels destructibles, planètes, IA ennemie, grande échelle (plus de 8 joueurs).

## Budget de performance Quest 3 (cibles)

- 72 Hz minimum, viser 90 Hz ; une image = 11 à 14 ms.
- Moins de 150 draw calls, moins de 500 000 triangles par œil.
- Rendu single-pass instancié, MSAA 4x, pas de HDR, pas de post-traitement au J1.
- Au J4 : fusion de maillages (greedy meshing) pour les grilles de plus de 200 blocs.
