# Outils — ressources locales et DRILL-R

Demande de Pierre du 3 novembre 2026 : outil de minage à main, meuleuse, soudeuse et
DRILL-R de MyrinoxPrime pour le vaisseau. Cette passe prépare les modèles, sans nouveau gameplay ni APK.

| Usage | Ressource | Résultat |
|-------|-----------|----------|
| Foreuse de vaisseau | MyrinoxPrime DRILL-R / PL-ORIG-DRILL-R | Retenue selon la demande de Pierre ; paquet visuel autonome copié et vérifié |
| Minage à main | SteelPickaxe.fbx, Survival Template PRO / Polymind Games | Candidat : pioche métallique, 2 meshes, 2 423 triangles ; style industriel futuriste non établi |
| Découpe / base pour meuleuse | SA_Wep_CircularSaw.fbx, Simple Apocalypse / Synty Studios | Candidat : scie circulaire, 436 triangles ; aucune vraie meuleuse portative identifiée à l'identique |
| Soudage | MyrinoxPrime UT-04-WELDER | Candidat : tête de soudeuse, 1 mesh, 4 627 triangles ; poignée et adaptation à la main à préparer |

Les candidats à main sont conservés pour revue, sans être équipés au joueur. La préférence
entre ces candidats et un ensemble industriel futuriste reste à préciser.

## DRILL-R : source et vérifications

Source autonome : `D:/Projets/Trement/MyrinoxPrime/ArgosWorkshop/Assets/MyrinoxPrime/Resources/Campaign/Argos/QuaiDrillR`.
Destination : `Assets/ThirdParty/ToolAssets/Ship/DRILL-R/`.
Les 18 fichiers ont été vérifiés par SHA-256 ; GUID, textures et provenance.json sont conservés.
Le prefab ne contient aucun MonoBehaviour, Animator ou Animation. Les dépendances visuelles
sont internes, sauf URP/Lit et AssetVersion, vérifiés dans le package URP de VoidForge.

Les LOD comptent **16 608 / 14 763 / 14 763 triangles** : les deux derniers ont le même nombre
de triangles. Les textures conservent les réglages source (maximum 2048, Android automatique).
La compression ASTC et la résolution cible seront à régler lors de l'intégration Quest.
Rotation mécanique et raccordement au vaisseau restent à implémenter.

## Candidats à main

Les 32 fichiers copiés et vérifiés par SHA-256 sont sous `Assets/ThirdParty/ToolAssets/Candidates/`,
sans scripts de jeu tiers. Sources :

- `G:/UnityAssets/asset_manager/store/Survival_Template_PRO/Files/PolymindGames/STP/Art/Models/Wieldables/SteelPickaxe`.
  Les métadonnées FBX mentionnent également HQ Survival Pack 1.3, produit 185364.
- `G:/UnityAssets/asset_manager/store/Simple_Apocalypse_-_Cartoon_Assets/Files/SimpleApocalypse`.
- `D:/Projets/Trement/MyrinoxPrime/docs/production/ModularFleetV1/motion-parts/UT-04/Meshy/01a122ef-3166-71d3-8e51-1c936c4509cd.fbx`
  et ses textures existantes. Aucune nouvelle génération Meshy n'a été lancée.

Reproduction depuis VoidForge, Unity fermé :

```powershell
.\Build\import-drill-r.ps1
.\Build\stage-hand-tool-candidates.ps1
```

Les scripts acceptent des racines source explicites et refusent d'écraser une copie locale différente.
Les rapports de copie restent locaux avec les modèles. Les ressources conservent leur provenance
et leurs licences ; rien sous Assets/ThirdParty n'est requalifié en MIT ni distribué dans Git.

## Portée de la recherche et limites

Le catalogue et les noms de fichiers de **200 packs 3D indexés** ont été recherchés avec plusieurs
alias de drill, grinder, welder et solder. Cela ne garantit pas l'identification de tout objet
regroupé sous un nom générique. Les foreuses fixes Sci-Fi Base, meules d'atelier et sons de
perceuse sont écartés. La torch.fbx de Hurricane VR a été inspectée : c'est une lampe torche.

Les FBX candidats ont été chargés et rendus dans un Blender séparé, avec code 0 ; aperçus
et métriques locaux dans `Logs/tool_review/`. Import Unity, matériaux URP des candidats,
prise en main, fonctions de meulage/soudage et performances Quest restent à vérifier.
La recette J1 antérieure ne qualifie pas automatiquement ces nouveaux outils.

Incident : le premier lancement Blender partiellement isolé a tenté un nettoyage du profil
utilisateur, refusé pour cv2.pyd et PIL/_imaging. L'impact éventuel n'a pas été établi ; aucune
restauration n'a été tentée. Journal : `Logs/tool-review-blender.log`.
Les reprises ont isolé CONFIG, SCRIPTS et EXTENSIONS sous `Library/BlenderToolReview/`, hors ligne.
Définir BLENDER_USER_RESOURCES et BLENDER_USER_EXTENSIONS en plus de CONFIG/SCRIPTS pour les
futurs lancements : [documentation Blender](https://docs.blender.org/manual/en/latest/advanced/command_line/arguments.html).

Suite : [minage](MINAGE.md), [architecture](ARCHITECTURE.md).
