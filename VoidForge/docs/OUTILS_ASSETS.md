# Outils — ressources locales et DRILL-R

Demande de Pierre du 3 novembre 2026 : outil de minage à main, meuleuse, soudeuse et
DRILL-R de MyrinoxPrime pour le vaisseau. Cette passe prépare les modèles, sans nouveau gameplay ni APK.

Après cette recherche, Pierre a demandé la production originale des trois outils à main
dans Blender MCP avec GPT-Astra. Cette production est suivie dans [OUTILS_MAIN.md](OUTILS_MAIN.md).
Le tableau ci-dessous conserve les résultats de la recherche dans les ressources existantes.

| Usage | Ressource | Résultat |
|-------|-----------|----------|
| Foreuse de vaisseau | MyrinoxPrime DRILL-R / PL-ORIG-DRILL-R | Retenue selon la demande de Pierre ; paquet visuel autonome copié et vérifié |
| Minage à main | Aucun modèle futuriste portatif approprié identifié | SteelPickaxe écartée par Pierre ; recherche non satisfaite |
| Meuleuse | Aucune meuleuse futuriste portative identifiée | SA_Wep_CircularSaw écartée par Pierre ; recherche non satisfaite |
| Soudage | MyrinoxPrime UT-04-WELDER | Candidat : tête de soudeuse, 1 mesh, 4 627 triangles ; poignée et adaptation à la main à préparer |
| Soudage portatif, alternative stylisée | Toon Soldiers / Polygon Blacksmith, weapon_welder.FBX | Candidat existant avec poignée et réservoir, 1 mesh, 269 triangles ; style cartoon, échelle et URP à adapter |

**Direction confirmée par Pierre le 3 novembre 2026 : outils industriels futuristes,
sans retenir la pioche et la scie.** Ces deux copies locales de référence ne sont ni sélectionnées
ni équipées ; le script ne les importe plus. La soudeuse UT-04 reste une piste à adapter.
L'alternative Toon Soldiers est conservée pour comparaison, sans choix final de style.
DRILL-R reste retenu pour le vaisseau.

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

La première recherche avait copié et vérifié 32 fichiers par SHA-256 sous
`Assets/ThirdParty/ToolAssets/Candidates/`, sans scripts de jeu tiers. Les copies de la pioche
et de la scie restent des références écartées ; aucun fichier suivi par Git n'a été supprimé.
Sources conservées pour la provenance :

- `G:/UnityAssets/asset_manager/store/Survival_Template_PRO/Files/PolymindGames/STP/Art/Models/Wieldables/SteelPickaxe`.
  Les métadonnées FBX mentionnent également HQ Survival Pack 1.3, produit 185364.
- `G:/UnityAssets/asset_manager/store/Simple_Apocalypse_-_Cartoon_Assets/Files/SimpleApocalypse`.
- `D:/Projets/Trement/MyrinoxPrime/docs/production/ModularFleetV1/motion-parts/UT-04/Meshy/01a122ef-3166-71d3-8e51-1c936c4509cd.fbx`
  et ses textures existantes. Aucune nouvelle génération Meshy n'a été lancée.
- `G:/UnityAssets/asset_manager/store/Toon_Soldiers/Files/Toon_Soldiers/ToonSoldiers_2/models/weapons/weapon_welder.FBX`,
  matériau `models/Materials/ToonSoldiers_weapons.mat` et atlas `TS_weapons_texture.tga`.
  Catalogue local : Toon Soldiers 2.1, Polygon Blacksmith, produit 52220.

Reproduction depuis VoidForge, Unity fermé :

```powershell
.\Build\import-drill-r.ps1
.\Build\stage-hand-tool-candidates.ps1
```

Les scripts acceptent des racines source explicites et refusent d'écraser une copie locale différente.
`stage-hand-tool-candidates.ps1` prépare désormais les six fichiers existants de la tête
UT-04-WELDER et six fichiers Toon Soldiers (FBX, matériau, texture, leurs .meta).
Son rapport est `Candidates/futuristic-copy-report.json` ; les modèles sont sous
`Candidates/WelderHead/` et `Candidates/ToonWelder/`.
Les rapports de copie restent locaux avec les modèles. Les ressources conservent leur provenance
et leurs licences ; rien sous Assets/ThirdParty n'est requalifié en MIT ni distribué dans Git.

## Portée de la recherche et limites

Le catalogue et les noms de fichiers de **200 packs 3D indexés** ont été recherchés avec plusieurs
alias de drill, grinder, welder et solder. Cela ne garantit pas l'identification de tout objet
regroupé sous un nom générique. Les foreuses fixes Sci-Fi Base, meules d'atelier et sons de
perceuse sont écartés. La torch.fbx de Hurricane VR a été inspectée : c'est une lampe torche.

La reprise orientée futuriste examine aussi les inventaires de SciFi Space Base/Colony,
Sci-Fi Heavy Station Kit, Sci-Fi Arsenal et Space Journey, ainsi que les productions Myrinox :

- `MiningTool_01/spec.json` décrit un module de vaisseau de 4,5 m, sans poignée ;
  `MiningEmitter_02/spec.json` le remplace par un émetteur monté sur coque. Aucun n'est un outil à main.
- `UT-02-HEAD` est une tête de découpe à mâchoire, avec interfaces d'axe pour un bras.
  Son visuel source a été examiné ; ce n'est pas une meuleuse à disque portative et elle n'est pas retenue.
- UT-04-WELDER a été examiné lors de la première passe. Une poignée, l'échelle et les points
  de prise restent à préparer.

L'inventaire élargi de `G:/UnityAssets` s'est terminé avec **88 565 chemins de modèles/prefabs**
(animations et variantes incluses), puis recherche par noms avec alias français et anglais.
Il a révélé la soudeuse Toon Soldiers, absente des résultats de la première passe.
Son FBX réel a été importé et rendu avec son atlas dans un Blender isolé (code 0) :
269 triangles, un mesh, dimensions importées 1,215 × 0,690 × 0,198 m avant adaptation.
La silhouette comporte une poignée et un réservoir ; le style est très simplifié.
Le matériau Unity source est un ancien fichier binaire ; sa conversion URP reste à vérifier.
Aperçu local : `Logs/tool_review/toon_welder_textured.png`.
Cette recherche par noms n'est pas une inspection visuelle exhaustive de tous les modèles.
Aucune foreuse à main ni vraie meuleuse futuriste appropriée n'a été identifiée.

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
