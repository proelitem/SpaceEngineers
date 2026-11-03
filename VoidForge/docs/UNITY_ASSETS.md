# Bibliothèque locale et prototype de creusage

Demande de Pierre du 3 novembre 2026 : réutiliser `G:/UnityAssets` et ajouter un
prototype de creusage jouable. Cette extension du J1 est distincte de J2 Construction.

## Ressources identifiées

| Pack local | Éditeur / version locale | Usage retenu |
|------------|--------------------------|--------------|
| Asteroid Pack 01, ID 19575 | Dark Anvil / 1.0.0 | 12 variantes, 3 LOD, 4 textures diffuses et normales ; remplace les sphères décoratives |
| Digger PRO Voxel Terrain Sculpting, ID 149753 | Amandine Entertainment / 8.1 | Volume rocheux isolé créé par Add, puis creusage asynchrone par Dig |
| Ultimate Terrain | Pampel Games | Générateur de terrains ; pas retenu comme moteur de creusage voxel |

Catalogue : `G:/UnityAssets/asset_manager/store/_jarvis_index.json`.
Chaque pack contient ses métadonnées dans `.jarvis/manifest.json` et sa documentation
dans `Files/`. La bibliothèque est lue uniquement ; aucune modification de ses fichiers.

## Installation locale

Fermer Unity, puis depuis VoidForge :

```powershell
.\Build\import-local-assets.ps1
```

La racine est configurable avec `-LibraryRoot` ; pour l'import des modèles dans Unity,
utiliser la variable de processus `VF_UNITY_ASSETS_ROOT` si la bibliothèque est déplacée.
Le script Windows de build appelle cette installation si la bibliothèque est présente
et que l'intégration locale n'est pas encore activée.

Ouvrir Unity 6000.4.0f1, puis **VoidForge > 5. Construire la zone de creusage**.
Cela importe les astéroïdes et construit le prefab local du prototype. En ligne de commande :

```powershell
& 'D:\Unity\6000.4.0f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'G:\Projets\SpaceEngineers\VoidForge' -buildTarget Android -executeMethod VoidForge.Editor.MiningPrototypeBuilder.BuildBatch -logFile 'G:\Projets\SpaceEngineers\VoidForge\Logs\assets-mining-import.log'
```

Le pipeline APK régénère ce prefab s'il est absent et que Digger est installé.
Sans les ressources locales, le code MIT conserve le repli sur les primitives J1 et
n'active pas l'adaptateur Digger. Le fichier local `Assets/csc.rsp` active
`VF_LOCAL_DIGGER` avant compilation ; il est ignoré par Git.

## Intégration technique

- Seuls les FBX et les textures du pack d'astéroïdes sont importés ; aucun ancien script
  ni prefab Unity 4.5 du pack n'est exécuté ou conservé dans la scène.
- Maillages normalisés autour d'un même repère pour conserver l'alignement des trois LOD.
  Matériaux URP/Lit, instancing, mipmaps, textures Android 1024 maximum en ASTC 6x6.
- Les 40 positions déterministes et les colliders sphériques du J1 sont conservés.
  Les rochers du pack sont décoratifs ; leurs FBX ne deviennent pas des voxels.
- Digger exige un Terrain Unity. Le prototype utilise un support technique de 16 × 16 m,
  hauteur maximale 16 m, résolution 65, voxels 0,25 m et chunks de 16 voxels utiles.
  Le rendu et le collider de ce terrain sont masqués. L'opération Digger `Add` crée un
  volume sphérique isolé (rayon 2,5 m, centre 0/-1/6), dont les colliders de chunks
  restent actifs et sont modifiés par `Dig`. Aucun moteur de meshing propre n'est réécrit.
- Dépendances : AI Navigation 2.0.11 et modules Terrain/Terrain Physics et Unity Web Request
  fournis par Unity. Digger utilise UnityWebRequest pour lire ses données sur Android.
  Les références optionnelles MicroSplat et HDRP sont retirées des `.asmdef` de la copie
  locale de Digger, car ces intégrations ne sont pas installées dans ce projet URP.
- Le shader Digger URP 17 fourni avec le pack est utilisé. Aucune réécriture du moteur voxel.

## Parcours jouable

Dans l'espace, viser le volume marqué **ASTÉROÏDE CREUSABLE**, devant le joueur.
Pointer la manette droite vers ce volume, à moins de 8 m, puis maintenir sa gâchette.
Un rayon indique la cible. La brosse sphérique est limitée à 0,75 m et à quatre
demandes par seconde, avec une seule opération asynchrone en cours.

Le compteur de la zone augmente à chaque excavation terminée. Redémarrer l'application
réinitialise l'essai ; la persistance est désactivée. Le prototype se désactive pendant
une partie réseau : les modifications voxel ne sont pas encore synchronisées.

Un test éditeur utilise une copie du TerrainData, crée le volume et vérifie que son
collider recule après excavation, sans modifier le prefab de départ.
En build développement, un test creuse aussi un point au démarrage et exige qu'un raycast
constate le recul du collider. Le journal doit contenir `[VoidForge Mining] SELFTEST OK`.
Une simple apparition d'un trou visuel ne suffit pas à valider la collision.

Pierre a confirmé le 3 novembre 2026 que le creusage fonctionne en casque avec des
performances élevées. La suite doit utiliser les formes des assets, des veines de minerais
et des outils à main ou sur vaisseau : voir [MINAGE.md](MINAGE.md).

## Licences et livraison

Le code propre à VoidForge reste sous MIT. Les ressources Asset Store et leurs dérivés
conservent leurs licences ; elles ne sont pas requalifiées en MIT.
`Assets/ThirdParty`, `Assets/Digger`, `Assets/DiggerData` et les données de build Digger
restent locaux et exclus de Git. Les adaptateurs, procédures et résultats sont versionnés.
Cette séparation suit la distribution d'assets incorporés au jeu décrite dans
l'[EULA Asset Store](https://unity.com/legal/as-terms) et sa
[FAQ officielle](https://assetstore.unity.com/browse/eula-faq).

Résultats observés : [suivi Hermes de cette extension](../Hermes_Works/Jalons/J1_ASSETS.md).
