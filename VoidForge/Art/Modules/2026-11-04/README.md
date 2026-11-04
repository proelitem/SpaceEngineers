# Modules industriels — Blender MCP / Modeler3D Astra

Production originale du 4 novembre 2026 dans Blender MCP 5.2.1 LTS, selon les trois
références `Art/UI/Cyberdeck/2026-11-03-v1/blocs/block-{cargo,refinery,assembler}.png`.
La soute reprend le verrou orange, les coins graphite et les panneaux chanfreinés ;
la raffinerie reprend les deux chambres et le collecteur ; l'assembleur reprend
le bras articulé à gauche, la pince et le plateau. Les petites traces d'usure
photographiques ne sont pas reproduites : ce sont des matériaux unis opaques.

| Modèle | Dimensions Unity X/Y/Z | Triangles | Maillages / matériaux |
|---|---|---:|---:|
| VF_Cargo | 1 / 1 / 1 m | 5 084 | 5 / 5 |
| VF_Refinery | 1 / 2 / 1 m | 11 376 | 5 / 5 |
| VF_Assembler | 2 / 1 / 1 m | 13 844 | 6 / 6 |

Les FBX sont dans `Assets/VoidForge/Art/Modules/2026-11-04/`. Chaque maillage
correspond à un matériau et porte le nom `VF_<Module>_<Matériau>`.
Le fichier `manifest.json` donne les couleurs linéaires, métallicité, rugosité
et émission. Dans URP, utiliser Smoothness = 1 − roughness, Surface Opaque et
des matériaux partagés ; aucune texture, transparence, lumière ou particule
n'est exportée. Les lampes visibles sont des surfaces émissives.

## Repère et import

Origine au centre de la cellule de base, sans décalage de pivot : toutes les
composantes Blender ont location/rotation nulles et scale 1. Blender utilise
X horizontal, +Y vers la face de service, +Z vertical. Export FBX métrique
`axis_forward=-Z`, `axis_up=Y`, `bake_space_transform=True`,
`apply_scale_options=FBX_SCALE_UNITS`, sans animation.

Le repère cible Unity est X horizontal, +Y vertical, face de service vers −Z.
Scale Factor = 1, Convert Units activé. Les bornes attendues sont :

- Cargo : min (−0,5 ; −0,5 ; −0,5), max (+0,5 ; +0,5 ; +0,5).
- Refinery : mêmes minima, max (+0,5 ; +1,5 ; +0,5).
- Assembler : mêmes minima, max (+1,5 ; +0,5 ; +0,5).

`validation.json` contient les bornes Blender mesurées après export, les
triangles et les contrôles de données finies/matériau unique. La vérification
de l'importeur Unity et la création des prefabs sont confiées à l'intégrateur.
Les éclairages et caméras du `.blend` servent uniquement aux aperçus.

## Sources et reproduction

`VF_Modules_04.blend` contient uniquement les trois scènes dédiées, leurs
matériaux et leurs studios de rendu. `build_modules.py` reconstruit la
géométrie, exporte les trois FBX, écrit le manifeste et valide les bornes.
Exécuter ce script depuis le Text Editor de Blender, avec son chemin de
fichier conservé. Il crée de nouvelles scènes et ne supprime pas la scène
utilisateur ouverte. Le dossier du script doit rester à cette position
relative à la racine VoidForge.

Pour les aperçus, sélectionner l'une des scènes `VF_Cargo_04`,
`VF_Refinery_04`, `VF_Assembler_04`, puis Render Image / Save Render,
ou appeler `bpy.ops.render.render(write_still=True)` après sélection.
Les trois PNG de `Previews/` ont été rendus et inspectés individuellement.

## Provenance et bibliothèque

Consultation préalable de `G:/UnityAssets/asset_manager/store/_jarvis_index.json`,
du manifeste Sci-Fi_base / Manufactura K4 (ID 173333, version 1.1), de ses
fichiers Container/Generator et instructions HDRP, ainsi que de l'inventaire
Containers et de `SpaceBase.txt` de SciFi_Space_Base / Daelonik Artworks.
Ces packs fournissent des accessoires génériques et des matériaux Standard/HDRP ;
ils ne correspondent pas aux silhouettes 2D demandées et leur redistribution
dans le dépôt MIT n'est pas autorisée par cette tâche. Aucun fichier ni
géométrie de ces packs n'est copié. Aucun code ou asset Keen n'est utilisé.

Géométrie et script originaux sous la licence MIT du projet. La scène Blender
préexistante des outils a été préservée. L'environnement Blender a également
montré une activité cockpit indépendante en fin de travail ; aucun objet de
cette activité n'a été modifié.

## Intégration Unity confirmée

Le parent a importé et validé les trois FBX sous Unity 6000.4.0f1. Le prefab enfant reçoit
une rotation Y de 180° pour compenser le repère FBX ; l’assembleur retrouve ainsi son étendue
X de −0,5 à +1,5 m. Les matériaux URP et bornes de grille sont contrôlés par
`VoidForge.Editor.Recette04Workshop`. Voir `docs/RECETTE_04.md` et les preuves INTEGRATION_04.
