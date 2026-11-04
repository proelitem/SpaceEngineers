# VoidForge Odyssey — interfaces du cyberdeck, v1

Concepts demandés par Pierre le 3 novembre 2026. Quatre familles, cinq vues : l'onglet Vaisseau possède une vue Raffinerie et une vue Assemblage distinctes. Images originales générées avec `image_gen.imagegen`, puis conservées sans retouche. Les prompts complets sont dans [generation-prompts.json](generation-prompts.json) ; les dimensions, sources et empreintes des PNG dans [manifest.json](manifest.json).

[Ouvrir la galerie](index.html)

## Géométrie et direction visuelle

- Dalle du modèle : **216 × 92,571 mm**, format **21:9** ; zone sûre **200 × 85,714 mm**. Référence mesurée : `meshy_output/voidforge-kit-20261103-v2/functional_refinement_report.json`, surface `VFv2_Cyberdeck_ScreenBlank`.
- Les PNG ont le format panoramique approché fourni par le générateur. Pour une prévisualisation sur la dalle, conserver leur proportion et ajuster par `contain`, avec une marge sombre minime. Les futures interfaces Unity devront employer une mise en page native 21:9 et maintenir chaque commande dans la zone sûre, sans étirer les glyphes.
- Fond charbon/navy, panneaux pétrole, texte ivoire, traits cyan, focus et action principale ambre. Les références de MyrinoxPrime ont été consultées pour leur organisation en fonctions et leur langage d'instruments ; les interfaces sont recomposées pour le cyberdeck de VoidForge.
- Trois lignes de ressources visibles et cinq cartes de construction au maximum. Navigation persistante : Inventaire / Construire / Vaisseau / GPS–Carte. L'état actif combine couleur, contour et libellé. Les pictogrammes gardent leur nom.
- Pour la réalisation : viser des cibles principales d'environ 10–12 mm et des lettres de 3–4 mm minimum sur la dalle ; proposer un agrandissement de lecture. Ce sont des objectifs de conception, pas des résultats mesurés en casque. Lisibilité, distance du bras, confort et précision de sélection restent à vérifier avec Pierre.

## Vues et interactions prévues

| Vue | Contenu | Action prévue |
|---|---|---|
| [01 — Inventaire](01-inventaire.png) | Colonne de gauche : Drill, Welder, Grinder. À droite : source de soute déroulante, filtres Minerais/Matériaux, icône, nom et quantité ; capacité et défilement. | Sélectionner un outil pour l'équiper ; changer de source de stockage ; défiler dans son contenu. Choisir une ressource ouvre sa fiche et les actions de transfert explicites. |
| [02 — Construction](02-construction.png) | Carrousel de blocs : gyroscope, batterie, armure, cockpit, propulseur. Carte centrale agrandie, taille de grille et coût visibles. | Flèches ou geste horizontal pour parcourir ; Sélectionner ouvre ensuite le mode de placement dans le monde. |
| [03a — Raffinerie](03a-vaisseau-raffinerie.png) | Contexte cockpit, module et alimentation, minerai vers lingots, progression et file de traitement. | Choisir Raffinerie/Assemblage, activer ou mettre en pause, accéder à la soute pour alimenter/récupérer. |
| [03b — Assemblage](03b-vaisseau-assemblage.png) | Liste de recettes illustrées, disponibilité des composants, quantité réglable et file de fabrication. | Sélectionner une recette, régler le lot et ajouter à la file ; mettre en pause et accéder à la soute. |
| [04 — GPS / Carte](04-gps-carte.png) | Carte spatiale locale 3D, repères, catégories, distance et coordonnées ; commandes zoom/recentrage. | Sélectionner un repère, le cibler, changer l'échelle et recentrer la vue. Le trait pointillé représente une direction de visée. |

Le menu Construction est une page dédiée ; le carrousel n'empiète pas sur l'inventaire. Le filtre de catégorie affiché « Structure » illustre l'emplacement du sélecteur : dans la réalisation, la liste mêlant les cinq blocs utilisera « Tous les blocs », et « Structure » ne contiendra que les armures.

Vaisseau n'est accessible que si le personnage est assis dans un cockpit valide et lié à ce vaisseau. À pied, cet onglet reste visible et indique « S'asseoir au cockpit ». Sortir du siège retire l'accès aux commandes du vaisseau. Inventaire, Construction et GPS restent disponibles selon leur propre contexte.

Les valeurs, capacités, recettes, coûts, puissances et temps sont **illustratifs**. Les trois recettes de la vue Assemblage explorent la présentation future ; elles ne fixent pas le catalogue du premier jalon. La feuille de route conserve sa chaîne initiale courte, ses recettes définies et ses transferts explicites. Le nom « Odyssey-01 » est un exemple de vaisseau. Les icônes d'outils et de blocs sont des propositions graphiques, distinctes des rendus exacts des assets 3D livrés.

États à prévoir dans Unity : aucune soute connectée, soute vide ou pleine, composants insuffisants, énergie absente, sortie saturée, file vide ou suspendue, recette indisponible et repère non sélectionné. Chaque état donne une raison lisible et désactive seulement l'action concernée. Une quantité de fabrication ne peut pas descendre sous 1 ; elle est limitée aux ressources disponibles. La navigation et la sélection nécessiteront un retour visuel au survol/focus et à la validation.

## ELANCE-MAP : examen de la technologie fournie

Archive examinée en lecture seule : `G:/Downloads/ELANCE-MAP.zip`, sans import de son projet dans VoidForge. `ProjectSettings/ProjectVersion.txt` indique **Unity 5.4.0f3**. VoidForge utilise Unity 6000.4.0f1 ; aucune compatibilité directe ni migration réussie n'est établie.

Fondations réellement observées dans l'archive :

| Fichier | Fonction observée | Usage envisagé pour la carte |
|---|---|---|
| `Assets/Scripts/Data/EntityData.cs` | Nom, catégorie, position Vector3 et description. | Données de repères et d'objets spatiaux. |
| `Assets/Scripts/Entity.cs` | Catégories PointOfInterest, Planet, Asteroid, SmallShip, LargeShip et Waypoint ; taille/couleur ; sélection par OnMouseDown. | Représentation et sélection des entités. La sélection XR remplacera le clic souris. |
| `Assets/Scripts/World.cs` | Création, suppression et liste d'entités ; export des données du monde. | Conteneur des entités affichées. |
| `Assets/Scripts/CameraController.cs` | Recentrage autour d'une entité et rotation orbitale automatique. | Cadrage de la carte sur le joueur ou une cible ; commandes manuelles à adapter. |
| `Assets/Scripts/XMLDataBase.cs` | Sérialisation et chargement XML ; chemins persistants. | Base à évaluer pour les repères enregistrés, avec gestion d'erreurs adaptée. |

Le concept conserve donc une **carte spatiale 3D fondée sur des entités**. Filtrage de catégories pour la consultation, bascule Local/Secteur, calcul des distances, interface de repères GPS, zoom manuel et commandes XR sont des adaptations proposées : leur présence dans la maquette ne prouve pas leur implémentation dans ELANCE. Aucun calcul d'itinéraire, évitement d'obstacles, pilote automatique, découverte de minerais ou accès multijoueur n'est établi par cet examen. Les données et recettes de la maquette sont fictives.

Pour la réalisation : isoler les fonctions de carte utiles dans un module VoidForge, adapter leur API à Unity 6000.4 et alimenter une caméra de carte dédiée vers une RenderTexture sur la dalle. Conserver les positions du monde séparées de l'échelle de visualisation, limiter les entités à celles connues du joueur et remplacer OnMouseDown par la sélection XR. Ces choix décrivent une piste d'intégration ; aucun code de l'archive n'a été copié et aucune scène Unity n'a été modifiée dans cette livraison.

## Références et contrôle

- `docs/ROADMAP.md` : boucle minage, raffinage, fabrication et exploration ; premier catalogue limité ; transferts explicites.
- `docs/concepts/2026-11-03-odyssey-v1/voidforge-odyssey-ecran-concept.png` : cyberdeck et langage initial de VoidForge.
- `meshy_output/voidforge-kit-20261103-v2/previews/cyberdeck.png` : boîtier et dalle vide.
- `D:/Projets/Trement/MyrinoxPrime/Assets/MyrinoxPrime/Runtime/UI/BuildHud.cs` et `BaseManagementCanvasSection.Hub.cs` : couleurs et organisation fonctionnelle consultées en lecture seule.
- `D:/Projets/Trement/MyrinoxPrime/docs/concepts/bridge-screens-2026-10-21/03-cockpit-commandement.png` : référence visuelle consultée en lecture seule.
- Index `G:/UnityAssets/asset_manager/store/_jarvis_index.json` consulté avant création. La présente livraison porte sur des maquettes originales adaptées à la dalle existante ; aucun pack tiers n'est ajouté.

Les cinq images ont été inspectées visuellement : libellés, outils, colonnes, carrousel, deux onglets de production et repères GPS. La galerie locale ajuste les images dans un cadre 21:9 sans les déformer. Il s'agit d'une galerie de concepts, avec navigation entre les vues ; les commandes dessinées ne pilotent aucun système de jeu. Les PNG restent des références raster aplaties, pas des textures HUD transparentes ni des composants Unity.

Git était indisponible pendant cette intervention : `fatal: cannot change to 'G:/Projets/SpaceEngineers'`. Aucun commit, push ni réparation du dépôt parent. Les fichiers préexistants ont été conservés.

## Graphiques séparés

Les 39 éléments originaux du HUD sont livrés dans [Art/UI/Cyberdeck/2026-11-03-v1](../../../Art/UI/Cyberdeck/2026-11-03-v1/README.md), avec [galerie](../../../Art/UI/Cyberdeck/2026-11-03-v1/index.html), minerais, produits de recettes, outils, blocs et pictogrammes. Les icônes industrielles ajoutent des blocs compacts de raffinerie, assembleur et soute. Les cinq maquettes ci-dessus restent les références de composition initiales.
