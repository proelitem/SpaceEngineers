# HUD 2D du cyberdeck

Extension actuelle : [économie jouable et procédure](ECONOMIE_CYBERDECK.md).
Le HUD utilise désormais 75 graphiques et le registre réel de stocks/production ;
les données d'aperçu ci-dessous restent réservées aux anciennes recettes visuelles.

Implémentation demandée par Pierre le 3 novembre 2026. Les cinq vues utilisent des composants
uGUI natifs et les 39 graphiques ImageGen du catalogue `Art/UI/Cyberdeck/2026-11-03-v1`.
Les images de maquette ne sont jamais chargées par le HUD. Les textes, boutons, jauges,
listes, files, carrousel et carte sont construits séparément.

## Dans Unity

Le prefab chargé au démarrage est `Assets/Resources/VoidForge/Cyberdeck/Hud.prefab`.
`CyberdeckRuntime` attend le `CyberdeckSurface` de l'équipement local, puis attache le Canvas
à `hudAnchor`, 0,8 mm devant la dalle. L'interface mesure environ **199,58 × 85,71 mm** et
tient dans les deux dimensions de la zone sûre de 200 × 85,71 mm.
Le canevas de référence conserve les **1914 × 822** des concepts sans déformer les icônes.
La dalle complète mesure 216 × 92,571 mm. Le facteur d'échelle compense celui du FBX parent.

Le menu **VoidForge → Cyberdeck → Ouvrir la scène de recette** crée/ouvre une scène distincte
avec caméra et données de démonstration. Le bandeau **APERÇU • DONNÉES DE RECETTE** y reste
visible. Cette scène ne remplace pas la scène principale et n'est pas ajoutée au build du jeu.

Commandes : viser avec la manette droite, presser puis relâcher la gâchette sur le même bouton.
Le contour blanc indique le survol ; la visée est limitée à deux mètres et tient compte des
obstacles. Une visée depuis le dos de l'écran est refusée. La perte de focus ou de suivi
annule l'appui et impose un relâchement avant une nouvelle commande.
Le bouton **Y** de la manette gauche ouvre/ferme le mode de lecture agrandi, qui place
l'interface à 62 cm, sur 62 cm de largeur.
Sur PC : souris, Tab/Entrée, flèches du carrousel, M pour agrandir et Échap pour revenir au poignet.

## Vues livrées

| Vue | Comportement |
|---|---|
| Inventaire | Sélection Drill/Welder/Grinder ; filtre Minerais/Matériaux ; source déroulante ; lignes icône/nom/quantité, sélection, défilement et capacité. |
| Construction | Cinq cartes visibles, carrousel circulaire de huit entrées, filtre Structure, dimensions fournies par le catalogue, disponibilité et commande de sélection du bloc. |
| Raffinerie | Bloc compact, alimentation, puissance, conversion, progression, file, pause et accès à la soute ; commandes retirées à la sortie du cockpit. |
| Assemblage | Recettes illustrées, disponibilité des composants, quantité, ajout à la file, progression, attente et pause. Quantités nulles ou supérieures au stock refusées. |
| GPS/Carte | Repères Vector3, catégories, distance et coordonnées réelles, ciblage, deux échelles, zoom, déplacement orbital et recentrage. |

Dans le jeu actuel, la sélection d'outil prépare la prise avec le **grip droit** ; relâcher ce
grip range l'outil selon la mécanique existante. La sélection n'invente pas de consommation
ou de transfert. Les dimensions et la disponibilité des blocs proviennent du catalogue livré.
La commande `BlockRequested` fournit le bloc choisi au futur mode de placement ; le HUD ne
crée pas une construction sans visée, aperçu ni validation réseau.

Les astéroïdes du champ existant sont enregistrés comme **repères**, sans leur attribuer
arbitrairement du fer. Les positions suivent les transforms du monde. Les bases et minerais
peuvent être publiés comme repères par leurs systèmes de jeu.

## Frontière avec les systèmes de jeu

`CyberdeckSession` contient l'état publié par le jeu et les événements de commande :
`EquipRequested`, `BlockRequested`, `AssemblyRequested`, `RefineryPowerRequested`,
`RefineryPauseRequested` et `AssemblyPauseRequested`. Le backend vérifie l'autorité et les
ressources, puis publie un nouvel instantané avec `Publish()`. Il n'existe aucune écriture
directe vers un inventaire réseau depuis un bouton.

La soute, les machines industrielles et le cockpit pilotable n'existent pas dans la livraison
actuelle. Le jeu affiche donc **aucune soute connectée**, **s'asseoir dans un cockpit connecté**
et **bloc indisponible**, selon le cas. Leurs cinq interfaces sont utilisables avec un état
publié par le backend ; les exemples visuels restent confinés à la scène de recette.
Le lot de plaques de cette scène emploie un ratio fictif, uniquement pour éprouver les bornes
de quantité et la réservation des entrées. Les recettes non définies restent indisponibles.
Ces valeurs ne constituent pas les recettes du jeu.

La carte reprend les principes réellement observés dans `G:/Downloads/ELANCE-MAP.zip` :
entités nommées et catégorisées à position Vector3, collection de repères, sélection,
caméra orbitale et recentrage. Son rendu est une projection spatiale originale en maillage
uGUI, adaptée aux interactions du cyberdeck. Aucun projet Unity 5.4, script obsolète,
ressource ni cache ELANCE n'a été importé. L'import/export XML historique n'est pas exposé.

## Graphiques et fidélité

Panneaux biseautés à dégradé pétrole, règles et focus cyan, action ambre, texte ivoire.
Les pictogrammes emploient le shader de masque de luminance original ; les objets restent
des textures RGBA. Les cadres UV retirent seulement la marge vide lors de l'affichage,
sans modifier les PNG produits. Les textures de masques sont linéaires, sans mipmaps ;
les objets sont sRGB. L'import Android emploie RGB24 pour les masques et ASTC 6×6 pour les objets.
La police DejaVu Sans Bold non modifiée provient de la publication officielle 2.37 ; la
[licence](https://dejavu-fonts.github.io/License.html), les notices et les empreintes sont
conservées avec le fichier. Sa largeur de titre « INVENTAIRE » à 49 px est de 330,94 px,
contre environ 330 px dans la référence. Liberation Sans/TMP reste une solution de repli.

L'index UnityAssets et les scripts pertinents ont été consultés : VR User Interfaces dépend
du symbole et du framework `VRInteraction` de 2017 ; Mobile VR Inventory utilise l'ancien
`UnityEngine.VR` et la visée par regard. Ultimate Inventory System ajoute la pile Opsive
Shared. Aucun ne correspond directement à cette dalle, au rig OpenXR actuel et aux cinq
maquettes. Le HUD réutilise donc uGUI et Input System déjà présents dans le projet.

**98 % est la cible demandée, pas une mesure certifiée.** Les captures natives et les
résultats des contrôles sont conservés dans `docs/validation/cyberdeck-2026-11-03`.
La typographie, les nouveaux objets ImageGen, les contenus réels et la projection GPS
diffèrent des images générées initiales. La lisibilité, le confort, les performances et la
précision des cibles en casque demandent encore une recette Quest par Pierre.

## Reproduction

1. `python Build/stage-cyberdeck.py` prépare les PNG, UV, police et shader sous Assets.
2. Le menu **Générer le HUD 2D** reconstruit uniquement les ressources du cyberdeck.
3. Pour valider sans toucher à l'éditeur principal : `python Build/validate-cyberdeck.py`, puis
   Unity 6000.4.0f1 sur `Library/HudGraphicsValidation20261103` avec
   `-batchmode -executeMethod VoidForge.Editor.CyberdeckWorkshop.RunBatch` et
   `-vfHudOutput <chemin absolu du dossier de preuves>`, **sans `-quit`**.
4. Après succès natif : `python Build/validate-cyberdeck.py --collect` récupère les ressources
   et leurs `.meta`. Le cache de ce projet de contrôle demeure privé.

Fixer `BEE_BUILD_THREADS=4` et `GRADLE_USER_HOME=VoidForge/Library/GradleUserHome` dans le
processus Unity. Aucun package externe ni ressource du dépôt Keen n'est nécessaire.

## Vérifications du 3 novembre

La passe finale sur le projet principal conserve la cible Android. `cyberdeck-ui-main-2.log`
se termine avec le code 0 : **36 contrôles**, cinq rendus natifs et trois états de secours,
39 textures résolues et aucune erreur du shader de masque. La galerie a été rendue et
inspectée dans un profil Chrome isolé. Les 39 PNG sous Assets conservent les empreintes
des originaux ImageGen.

`cyberdeck-game-1.log` se termine avec le code 0 : **10 assertions ciblées du HUD** passent
sur le montage Equipment final. Mesures réelles : largeur **0,19958290 m**, hauteur
**0,08571430 m**, produit scalaire normale Canvas/ancre **−0,99999988**. Trois outils et
40 astéroïdes réels sont détectés. `device-preview.png` montre le rendu du Canvas sur le
maillage livré, avec les données de recette explicitement signalées.

Le journal global du Play n'est **pas** sans exception : à sa sortie, Digger signale
`UnityException: get_internalHeightmapResolution can only be called from the main thread`,
avec une pile `TerrainCutter.GetCreateHoles → VoxelChunk.UpdateVoxelsOnSurface →
DiggerSystem.Modify → MiningPrototype.Start`. Un avertissement final mentionne deux
allocations persistantes. Cette observation est transmise à la passe minage/équipement ;
la cause n'est pas établie ici et aucun code Digger n'a été modifié. Le succès des assertions
du HUD ne certifie donc pas l'absence d'erreurs de tous les systèmes du jeu.

Les tests casque, le budget de rendu XR et la fidélité perceptuelle globale de 98 % ne sont
pas certifiés par ces contrôles. La passe équipement gère séparément le build APK correctif ;
cette livraison contient les sources, ressources Unity et preuves du HUD.
