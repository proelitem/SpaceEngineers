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
          AsteroidAssetSet         meshes et matériaux locaux, 3 LOD, repli sur primitives
          MiningPrototype          outil Digger solo, gâchette droite, édition asynchrone bornée
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

Extension autorisée le 3 novembre 2026 : prototype local de creusage avec Digger PRO,
via Add/Dig, avec un Terrain Unity de support masqué et un volume isolé, chargé depuis
un prefab Resources généré localement.
L'adaptateur est conditionné par `VF_LOCAL_DIGGER` ; les sources tierces restent hors Git.
Voir [UNITY_ASSETS.md](UNITY_ASSETS.md). Ce prototype ne convertit pas les FBX décoratifs
en volumes voxel ; persistance et réplication des excavations restent à concevoir.

La cible de minage confirmée par Pierre utilise les formes des assets existants, des veines
suivant un profil Space Engineers documenté publiquement, et des outils à main ou de vaisseau.
La conversion des formes en volumes creusables et le traitement commun matière/minerai/inventaire
restent à réaliser : [exigences de minage](MINAGE.md).

Hors périmètre du MVP initial : destruction complète des astéroïdes, planètes,
IA ennemie, grande échelle (plus de 8 joueurs).

## Ressources visuelles des outils à main

Les modèles des outils à main possèdent une source éditable sous `Art/HandTools/`
et des exports individuels sous `Assets/VoidForge/Art/HandTools/`. Les pièces mobiles
sont séparées du corps et les ancrages de prise/contact sont nommés, pour préparer
le raccordement XR et les comportements définis dans [MINAGE.md](MINAGE.md).
La livraison visuelle et ses contrôles sont décrits dans [OUTILS_MAIN.md](OUTILS_MAIN.md).

## Première intégration locale des ressources

`Integration/` raccorde l'astronaute VRIK aux poses XR, le cyberdeck à l'avant-bras
gauche et les outils aux prises manette. `EquipmentRig` consomme le grip droit pendant
une prise ; `PlayerInputProvider.RightGripConsumed` évite de freiner avec cette même
entrée. Le grip gauche conserve le freinage. `IntegrationBootstrap` charge les prefabs
Resources depuis `GameBootstrap` ; l'absence de dépendances locales conserve le rig J1.

Les étuis sont enfants de `Hips`, avec une poignée à hauteur de ceinture. Le rappel
par cyberdeck utilise la même méthode `Take` que la prise de proximité : il attend le
premier grip avant d'autoriser l'activation, puis son relâchement range l'outil.
Le générateur calibre les axes de fermeture vers la paume dans la copie intégrée de
l'avatar. Le cyberdeck fait face à la paume, ses sangles sont omises de l'export dérivé.
Le disque utilise un pivot intermédiaire centré sur le maillage et son axe local mesuré.

`BlockCatalogue` réserve les identifiants des blocs disponibles et en production.
`BlockGrid` vérifie l'occupation entière et les rotations de grille avant instanciation.
L'assemblage de démonstration demeure local ; les RPC de construction, la physique de
vaisseau et l'avatar réseau complet restent à réaliser. Contrats, limites et procédure :
[INTEGRATION.md](INTEGRATION.md).

## Budget de performance Quest 3 (cibles)

La boucle industrielle solo et son état persistant sont maintenant dans `Economy/` :
`EconomyModel` tient les stocks et transactions ; `EconomyRuntime` raccorde les outils,
blocs, cockpit et 75 graphiques au Cyberdeck ; `EconomyMiningAdapter` crédite les opérations
Digger confirmées. Les gisements finis et le registre de production sont sauvegardés.
Les quantités sont décimales ; le HUD ne possède plus le stock de jeu.
Voir [le profil actif et la recette](ECONOMIE_CYBERDECK.md). Cette extension ne réplique pas
encore l'économie ni les excavations ; leurs commandes sont suspendues en session réseau.

- 72 Hz minimum, viser 90 Hz ; une image = 11 à 14 ms.
- Moins de 150 draw calls, moins de 500 000 triangles par œil.
- Rendu single-pass instancié, MSAA 4x, pas de HDR, pas de post-traitement au J1.
- Au J4 : fusion de maillages (greedy meshing) pour les grilles de plus de 200 blocs.

## Extension de scène du 4 novembre — INTEGRATION_03

La scène active est sélectionnée par `Resources/VoidForge/Survival/Settings` avec les
symboles locaux Digger/économie. `GameBootstrap` charge le champ creusable ; la sphère
J1 et le présentoir ne sont plus instanciés. La scène Main générée conserve étoiles,
rig XR et réseau. Les dépendances locales absentes gardent le repli du rig J1.

- `ThighToolHolster` suit la cuisse après VRIK sans son échelle d'import. `EquipmentRig`
  sépare sélection et prise ; `HandTool` restaure aussi l'échelle au rangement.
- `WristDisplayMount` suit des points de la peau de la manche après le solveur.
  La vue suivie est le repère extérieur de la visière ; le corps s'y recale.
- `MinableAsteroidField` garde les LOD des assets à distance, crée les colliders/meshes
  Digger à proximité et encode les veines dans leurs vrais indices de matériau.
  `OreDigOperation` compte la matière retirée par matériau, hors bords de chunks partagés.
  Les accès Terrain sont préparés sur main avant la phase worker Digger.
- `ConstructionSystem` conserve plans, grilles et repères physiques : premier plan libre,
  suivants adjacents, vérification des collisions, consommation à la fin du soudage.
- `SurvivalSession` pilote le registre économique existant et une sauvegarde atomique
  commune aux stocks, plans, poses et excavations. `SurvivalDeckAdapter` reprend le HUD
  via les ports publics de transfert d'interface et de rafraîchissement.
- `GpsTargetGuide` dessine le cadre à la profondeur de l'entité et le chevron hors champ.

La tranche reste solo, avec grilles statiques et économie globale. Les paramètres
numériques de veines sont originaux ; l'équivalence Space Engineers reste non certifiée.
Contrats, dépendances, limites et procédure : [SURVIE.md](SURVIE.md).