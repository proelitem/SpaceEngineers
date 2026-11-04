# VoidForge Odyssey — graphiques séparés du cyberdeck

39 éléments graphiques originaux réalisés avec **image_gen.imagegen** le 3 novembre 2026, pour les cinq maquettes de `docs/concepts/2026-11-03-cyberdeck-ui-v1`. Les PNG retenus sont conservés tels que fournis par le générateur. Le catalogue final est [manifest.json](manifest.json), la [galerie](index.html) permet de changer le fond, la taille et la teinte des pictogrammes. Les prompts exacts sont dans [generation-prompts.json](generation-prompts.json).

## Contenu retenu

| Famille | Éléments |
|---|---|
| Minerais — 3 | Fer, nickel, silicium. |
| Produit — 1 | Lingots de fer. |
| Recettes — 3 | Plaques métalliques, tubes, circuits. La vignette de sortie représente la recette. |
| Outils — 3 | Drill, Welder, Grinder. |
| Blocs — 8 | Armure, gyroscope, batterie, cockpit, propulseur, **raffinerie, assembleur, soute**. |
| Carte — 3 | Astéroïde, vaisseau du joueur, avant-poste. |
| Interface — 18 | Inventaire/soute, construction, vaisseau, GPS, batterie, chevron, plus, moins, recentrage/ciblage, alimentation, pause, validation, énergie, siège cockpit, base, minerais et balise, flèche de flux. |

Les 21 objets sont des PNG RGBA transparents. Les 18 pictogrammes retenus sont des **PNG opaques de masque de luminance** dans `interface-masks/` : noir = transparent à l'affichage, blanc = couleur du pictogramme. Ce format permet de les afficher en ivoire, cyan, ambre ou anthracite sans carré noir, avec le shader fourni. Ce ne sont pas des PNG déjà détourés par leur canal alpha.

Les premières générations de pictogrammes avec alpha avaient des résidus visibles. Elles restent dans `interface/` avec leurs reçus pour la traçabilité, **sont rejetées et exclues du catalogue et du ZIP**. Le test cyan a été écarté également. `generation-jobs.json` est l'historique initial ; seul `generation-prompts.json` décrit les 39 fichiers retenus.

## Blocs de MyrinoxPrime retrouvés

Les sources ont été consultées en lecture seule dans `D:/Projets/Trement/MyrinoxPrime`. Les premiers concepts montrent des volumes compacts utilisables comme direction visuelle, ainsi que de grandes plateformes de base distinctes. Les nouvelles icônes représentent des **blocs de construction carénés**.

| Source examinée | Observation et emploi |
|---|---|
| `docs/Top-Down-Concept/16a86187-6a27-4899-bf86-c16197a650c0.png` | Ancien inspecteur avec une raffinerie compacte ivoire/orange. Référence directe de `block-refinery`. |
| `docs/Top-Down-Concept/38fb3e62-2df6-4d9f-b610-e7c5c1515db5.png` | Ancien inventaire contenant des caisses cubiques carénées. Référence de `block-cargo`. |
| `docs/Top-Down-Concept/Module-Assemblers.png` | Bras et mécanismes de fabrication sur une plateforme. Ces idées ont été recomposées dans une enveloppe rectangulaire pour `block-assembler`. |
| `docs/Top-Down-Concept/V2/01-Construction/ecran-complet.png` | Catalogue avec vignettes de raffinerie, soute et assembleur ; confirmé visuellement. |
| `Assets/MyrinoxPrime/Resources/UI/V2/Construction/Thumbnails/{Refinery,Assembler,StorageCrate}.png` | Vignettes existantes inspectées ; elles montrent les plateformes de base. Aucun prefab autonome des trois blocs compacts recherchés n'a été identifié par cette recherche. |

Le fait que ces anciens concepts ne soient plus utilisés n'a **pas** été établi. Aucun de ces fichiers source n'a été déplacé ou modifié. Aucun code ni asset Keen n'est importé. L'index de `G:/UnityAssets/asset_manager/store/_jarvis_index.json` a été consulté ; les pictogrammes de rang spatial et les minerais fantaisie disponibles n'ont pas été retenus pour cette direction industrielle originale demandée avec ImageGen.

Pour une éventuelle production 3D, les silhouettes proposent : raffinerie verticale environ 1 × 2 × 1 cellules, assembleur horizontal environ 2 × 1 × 1, soute cubique 1 × 1 × 1, sur la grille de référence de 1 m du kit VoidForge. Ces proportions sont des **intentions de concept** : les PNG ne valident ni dimensions métriques, ni surfaces de montage, ni colliders, ni imbrication réelle. Les futurs modèles devront garder les faces de jonction planes et leurs mécanismes dans l'enveloppe du bloc. Aucun modèle Meshy ou Blender supplémentaire n'est produit ici.

## Recettes et correspondance avec les maquettes

[recipes-visuals.json](recipes-visuals.json) associe les icônes aux recettes montrées : fer vers lingots, plaques, tubes, circuits, et plaques vers bloc d'armure. Les ingrédients et ratios non définis restent à définir ; aucun équilibrage de jeu n'est inventé. Les quantités des maquettes sont illustratives.

[hud-usage.json](hud-usage.json) indique les associations de chaque page et les réemplois : le cube sert aussi à la soute et à la capacité, la mire au recentrage et au ciblage, l'épingle au GPS et à la distance. En coordonnées écran, un chevron vers le bas tourné de −90° pointe à droite, et de +90° à gauche ; une flèche de flux vers la droite tournée de 180° pointe à gauche. Les points du carrousel, cadres, barres, boutons, grilles, textes et valeurs restent des primitives de l'interface, pas des images à régénérer.

Les images d'outils et des cinq premiers blocs reprennent les maquettes du cyberdeck. Elles constituent une famille graphique cohérente ; elles ne sont pas des rendus techniques certifiés des fichiers 3D existants.

## Utilisation dans Unity

Le dossier est une source artistique hors `Assets/`. Aucune scène, aucun prefab et aucun réglage du jeu n'est modifié. Les fichiers suivants sont préparés pour une intégration ultérieure :

1. Importer les objets en Sprite/Single, maillage Full Rect, pivot au centre, filtrage bilinéaire, Wrap Clamp, alpha conservé, mipmaps désactivées pour l'UI. Garder la résolution source dans `Art/` ; choisir un maximum d'import de 256 ou 512 selon l'usage et vérifier le résultat en casque.
2. Importer les PNG de `interface-masks/` en données linéaires (**sRGB désactivé**), sans mipmaps et sans traitement « Alpha is transparency ». Leur couverture est dans RGB, pas dans alpha. Utiliser un rectangle complet et éviter une compression destructrice du masque pendant la validation.
3. Créer un matériau avec [Unity/VoidForgeGlyphMask.shader](Unity/VoidForgeGlyphMask.shader), prévu pour URP. L'affecter à l'Image/RawImage du pictogramme. Sa teinte multiplie la couleur du composant ; les valeurs noires du masque deviennent transparentes. Stencil et découpe rectangulaire simple sont prévus. Les masques doux et le confort XR restent à tester lors de l'intégration.
4. Conserver les noms et quantités en texte dynamique. Préserver les proportions des images. Le HUD natif conserve la dalle 21:9, sa zone sûre et ses grandes cibles de sélection.

Les preuves effectivement réalisées figurent dans [validation.json](validation.json). Un contrôle d'import/compilation de shader ne constitue pas une recette du HUD en casque.

## Livrable et traçabilité

- `manifest.json` : fichiers retenus, formats, dimensions réelles et SHA-256.
- `generation-prompts.json` : prompts et références exacts des fichiers retenus, outil natif ImageGen.
- `selected-sources.json`, `receipts/`, `receipts-masks/` : provenance et historique local.
- `index.html` : galerie locale autonome, changements de taille, fond et teinte ; les masques sont affichés par leur luminance.
- `VoidForge-Cyberdeck-Graphics-v1.zip` : catalogue retenu, galerie, notes et shader ; exclut les générations rejetées.

Les PNG n'ont pas été découpés, redessinés ou retouchés avec Python. Le script `build_catalogue.py` lit leurs métadonnées, assemble la galerie HTML et prépare le ZIP. Les sources des autres projets et les maquettes précédentes sont préservées. Git reste indisponible (`fatal: cannot change to 'G:/Projets/SpaceEngineers'`) ; aucun commit ni push.

Contrôle visuel de la galerie réalisé dans Chrome : affichage des objets sur fond sombre et des 18 masques en anthracite sur fond clair à 64 px, avec changement de filtre et de teinte. Captures : `qa-gallery-dark.png`, `qa-gallery-light.png`.

Une vue révisée du HUD est également fournie : [raffinerie avec le bloc compact](concept-raffinerie-bloc.png). Elle a été corrigée avec ImageGen depuis la maquette initiale et le nouveau bloc, en conservant les commandes et valeurs de la page. Son prompt est dans `concept-raffinerie-generation.json`.

Statut du contrôle Unity : l’import du shader apparaît dans `Logs/hud-mask-import-2.log`, mais le projet URP isolé n’a pas terminé son démarrage après l’import initial. L’éditeur auxiliaire a été arrêté ; le contrôle final ShaderUtil n’a pas été exécuté. Le shader est fourni comme adaptateur **à valider à l’intégration**, sans déclaration de compilation finale ni de recette XR réussie. La galerie Chrome confirme séparément l’affichage des masques et leurs teintes.
