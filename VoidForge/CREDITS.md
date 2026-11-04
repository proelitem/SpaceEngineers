# Crédits et provenance des ressources

VoidForge VR ne contient **aucun** code, modèle, texture ni son provenant de
Space Engineers ou de Keen Software House. Le dossier `VoidForge/` est un projet
indépendant, sous licence MIT (voir `LICENSE`), qui s'inspire du *gameplay*
(construction à blocs, vol en apesanteur) sans rien en reprendre.

## Jalon J1

| Ressource | Origine | Licence |
|-----------|---------|---------|
| Code C# (`Assets/VoidForge/`) | Écrit pour ce projet | MIT |
| Géométrie de repli et manettes | Primitives Unity générées en code | Licence Unity (moteur) |
| Champ d'étoiles | Maillage généré procéduralement en code | MIT |
| Police du HUD de débogage | Police « LegacyRuntime » fournie par le moteur Unity | Licence Unity (moteur) |

## Extension assets et creusage — 3 novembre 2026

| Ressource | Origine | Licence et stockage |
|-----------|---------|---------------------|
| Asteroid Pack 01 1.0.0, ID 19575 | Dark Anvil ; G:/UnityAssets/asset_manager/store/Asteroid_Pack_01 | Asset Store EULA ; sources, textures et meshes normalisés locaux sous Assets/ThirdParty, non distribués dans Git |
| Digger PRO 8.1, ID 149753 | Amandine Entertainment ; G:/UnityAssets/asset_manager/store/Digger_PRO_Voxel_Terrain_Sculpting | Asset Store EULA ; copie locale sous Assets/Digger et données générées exclues de Git |
| Adaptateurs d'import, placement et commandes de creusage | Écrits pour VoidForge | MIT |

Les ressources tierces ne sont pas couvertes par la licence MIT du code propre au projet.
L'installation locale et les adaptations de compatibilité sont décrites dans
[UNITY_ASSETS.md](docs/UNITY_ASSETS.md). Sources :
[Asteroid Pack 01](https://assetstore.unity.com/packages/p/19575),
[Digger PRO](https://assetstore.unity.com/packages/p/149753),
[EULA Asset Store](https://unity.com/legal/as-terms).

## Ressources d'outils — 3 novembre 2026

DRILL-R et la tête UT-04-WELDER sont issus des ressources existantes MyrinoxPrime.
Les candidats SteelPickaxe (Survival Template PRO, origine FBX HQ Survival Pack) et
SA_Wep_CircularSaw (Simple Apocalypse, Synty Studios) viennent de G:/UnityAssets.
Ces deux candidats ont ensuite été écartés par Pierre. L'alternative de soudeuse portative
weapon_welder vient de Toon Soldiers 2.1 / Polygon Blacksmith, produit 52220, dans la même
bibliothèque locale ; FBX, matériau et atlas sont conservés sous leurs licences d'origine.
Modèles, textures, matériaux et métadonnées sont locaux sous Assets/ThirdParty/ToolAssets,
exclus de Git et non requalifiés en MIT. Sources et état de préparation :
[OUTILS_ASSETS.md](docs/OUTILS_ASSETS.md). Les scripts de copie propres à VoidForge restent MIT.

## Outils à main originaux — 3 novembre 2026

Foreuse portative, meuleuse et soudeuse : géométrie, matériaux et scripts créés pour
VoidForge avec GPT-Astra dans Blender MCP, sous licence MIT. Le contrat de production
Modeler3DAgent de Jarvis3D a guidé l'exécution ; aucun de ses fichiers backend n'est
copié dans le projet. La palette et la direction industrielle de DRILL-R servent de
référence visuelle. Aucun maillage Keen ni ressource tierce n'entre dans ces trois modèles.
Sources et contrôles : [OUTILS_MAIN.md](docs/OUTILS_MAIN.md).

## Première intégration des ressources — 3 novembre 2026

| Ressource | Provenance et stockage |
| --- | --- |
| Armures cube/pente/coin, gyro, cyberdeck v2 ; cockpit, propulseur, batterie v1 | Ressources produites pour VoidForge avec Meshy et corrections Blender, livraisons locales `meshy_output/voidforge-kit-20261103*`. Conversions créées par cette intégration dans `Assets/VoidForge/Art/Kit`, empreintes des GLB sources dans `kit-materials.json`. Aucune nouvelle génération payante |
| Astronaute hybride et mains HVR | Livraison locale de l'astronaute, `docs/ASTRONAUTE.md` ; modèles hybrides et maillage local sans tête sous `Assets/ThirdParty`, non redistribués en MIT |
| FinalIK 2.5 | RootMotion, bibliothèque locale `G:/UnityAssets/asset_manager/store/Final_IK` ; Asset Store EULA, runtime sous `Assets/ThirdParty`, exclu de Git |
| Adaptateurs XR, grille locale, configurateurs et matériaux URP d'outils | Créés pour VoidForge, MIT |

Les sources graphiques encore en production et leurs scripts ne sont pas inclus par
la livraison d'intégration. Contrats et limites : [INTEGRATION.md](docs/INTEGRATION.md).

## Packages Unity utilisés

OpenXR Plugin, XR Plug-in Management, XR Interaction Toolkit, XR Core Utilities,
Input System, Universal Render Pipeline, Netcode for GameObjects, Unity Transport,
AI Navigation et modules Terrain/Terrain Physics, fournis par le Package Manager.
La licence propre à chaque package est indiquée dans ses fichiers de licence.

Toute ressource ajoutée aux jalons suivants (sons, modèles, textures) doit être
listée ici avec son auteur, sa source et sa licence (CC0 ou MIT de préférence).
