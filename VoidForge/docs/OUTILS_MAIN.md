# Outils à main — production originale dans Blender

Demande de Pierre du 3 novembre 2026 : produire une foreuse portative, une meuleuse
et une soudeuse industrielles futuristes avec GPT-Astra dans Blender MCP.
La gamme reprend la direction visuelle de DRILL-R : carters ivoire, métal graphite,
poignées sombres et repères de sécurité sobres. DRILL-R demeure l'outil de vaisseau.

## Périmètre et critères

| Outil | Parties fonctionnelles attendues |
|-------|--------------------------------|
| Foreuse | Corps, poignée principale, gâchette, prise auxiliaire, mandrin et fraise séparés |
| Meuleuse | Corps portatif, renvoi d'angle, disque séparé, carter partiel et poignée latérale |
| Soudeuse | Corps, poignée, gâchette et buse séparée |

Les dimensions sont exprimées en mètres. Les repères `GripPrimary` et `ToolTip`
doivent survivre aux exports ; une prise secondaire est prévue selon l'outil.
Les pièces destinées à tourner sont séparées du corps fixe.
La cible de production est de 6 000 triangles par outil, avec un plafond de 8 000
dans chacun des exports. Ces budgets ne sont pas des mesures de performances Quest.

## Procédure et provenance

Le contrat utilisé est celui de **Modeler3DAgent** de Jarvis3D :
`D:/jarvis3d/docs/MODELER3D_AGENT.md` et les instructions `SYSTEME_MODELEUR` de
`D:/jarvis3d/backend/codex_blender.py`.
L'exécution est réalisée par un agent **gpt-6-astra**, effort high, dans Codex,
qui pilote les outils Blender MCP de cette session. Le point HTTP de Jarvis3D
n'est pas appelé. Les fichiers du backend sont seulement lus.

La création est originale et suit une boucle de construction, captures,
inspection sous plusieurs angles et corrections dans Blender. Les scripts et
ressources produits pour VoidForge relèvent de la licence MIT du projet.
La bibliothèque locale a été consultée avant cette production ; les candidats
et leurs limites sont conservés dans [OUTILS_ASSETS.md](OUTILS_ASSETS.md).

## Organisation des fichiers

- `Art/HandTools/Source/` : source Blender éditable.
- `Art/HandTools/Scripts/` : construction et finitions reproductibles.
- `Art/HandTools/Previews/` : rendus pour la revue visuelle.
- `Assets/VoidForge/Art/HandTools/` : exports individuels FBX et GLB.
- `Art/HandTools/Validation/` : résultats de relecture indépendante.

Depuis la racine VoidForge, après export :

```powershell
.\Build\check-hand-tool-exports.ps1
.\Build\check-hand-tool-unity.ps1
```

Le contrôle importe chaque FBX et GLB dans un Blender distinct, avec un profil
entièrement isolé sous `Library/HandToolExportValidation`. Il vérifie les données
géométriques finies, le plafond de triangles, l'échelle portative, les matériaux
et les ancrages. Les deux formats doivent avoir le même nombre de triangles et
des dimensions concordantes à 2 mm près.

Le second contrôle utilise Unity `6000.4.0f1` dans un petit projet jetable sous
`Library/HandToolUnityValidation`. Il importe les seuls exports et un validateur
éditeur, puis conserve leurs `.meta`. Le projet principal et ses scripts de jeu
ne sont pas ouverts par ce contrôle. Aucun code Keen n'est compilé.

Pour reconstruire dans une nouvelle session Blender avec MCP : exécuter
`build_hand_tools.py`, puis `checkpoint_hand_tools.py`, `package_hand_tools.py`
et `render_hand_tools.py` depuis `Art/HandTools/Scripts/`. Ces scripts utilisent
leur propre chemin pour trouver la racine du projet. Les corrections de la
première revue sont déjà dans le constructeur ; `refine_hand_tools.py` conserve
la trace de cette première correction. La source finale contient les pièces
éditables cachées dans les collections `SourceParts` et les assemblages exportés.

## État de livraison

**Trois modèles produits, six exports relus et trois FBX importés dans Unity.**

| Outil | Triangles par export | Longueur | Assemblages |
|-------|---------------------:|---------:|-------------|
| Foreuse | 5 520 | 460 mm | Corps, rotor mandrin/fraise, gâchette |
| Meuleuse | 5 064 | 362 mm | Corps/carter, disque, gâchette |
| Soudeuse | 4 920 | 341 mm | Corps, buse, gâchette |

Les quatre matériaux PBR sont partagés. Les six relectures Blender retrouvent les
mêmes dimensions, les mêmes triangles, les prises et les points de contact,
avec zéro triangle dégénéré. Unity retrouve les trois meshes et les trois ancrages
de chaque outil, aux mêmes dimensions en mètres après conversion des axes.
Onze `.meta` avec GUID distincts sont conservés.

Source : [VoidForge_HandTools.blend](../Art/HandTools/Source/VoidForge_HandTools.blend).
Aperçu : [les trois outils](../Art/HandTools/Previews/hand_tools.png).
Preuves : [manifeste](../Art/HandTools/modeler3d_report.json),
[relecture FBX/GLB](../Art/HandTools/Validation/export-readback.json) et
[import Unity](../Art/HandTools/Validation/unity-import.json).

**À préparer avant utilisation dans le jeu :** Unity importe les matériaux FBX
avec le shader `Standard`. Les quatre matériaux doivent être remappés vers URP/Lit
et leur rendu vérifié dans le projet cible. Le GLB conserve les facteurs PBR.
Les fonctions de minage/meulage/soudage, les animations, colliders, la prise XR,
les LOD et les performances sur Quest restent à intégrer ou vérifier.
Les nombres de triangles ci-dessus ne constituent pas une mesure de FPS ou de draw calls.

Incident de production : interruption de connexion native pendant le regroupement
des pièces. La trace Blender relevée indique `EXCEPTION_ACCESS_VIOLATION` dans
`library_foreach_ID_link / deg_update_eval_copy_datablock`. Les modèles ont été
restaurés depuis les scripts et sauvegardés ; le regroupement utilise désormais
les matériaux originaux plutôt que les identifiants temporaires du depsgraph.
Les exports obtenus ensuite ont passé les contrôles indépendants et Unity.
Une dernière demande de capture MCP a encore renvoyé `WinError 10054`, tandis que
la relecture de scène a confirmé les modèles présents. Les rendus studio sauvegardés
ont été inspectés directement ; ne pas réinitialiser la scène sur la seule base de cette erreur.

Références : [architecture](ARCHITECTURE.md), [minage](MINAGE.md),
[index Hermes Works](../Hermes_Works/README.md).
