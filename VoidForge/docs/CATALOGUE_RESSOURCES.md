# Catalogue de ressources et recettes du Cyberdeck

Livraison du 3 novembre 2026 : [galerie et calculateur](../Art/UI/Cyberdeck/2026-11-03-v2-resources/index.html),
[inventaire détaillé, sources et règles](../Art/UI/Cyberdeck/2026-11-03-v2-resources/README.md),
[données JSON](../Art/UI/Cyberdeck/2026-11-03-v2-resources/catalogue.json).

Le profil `se1-classic-industrial-public-reference-20261103` couvre 11 matières brutes,
10 matières raffinées, 21 composants classiques et la ferraille. Les trois outils standards
et huit variantes SE de blocs déjà illustrés disposent aussi de coûts : 32 fabrications,
11 traitements de raffinage/recyclage, calculs d'assemblage ×1/×3/×10.
Le périmètre et les différences modernes non couvertes sont explicités dans l'inventaire.

36 PNG originaux ont été produits par 36 appels distincts au générateur ImageGen intégré.
Les 18 illustrations v1 réutilisées sont conservées sans modification. Prompts et provenance
figurent à côté des exports CSV ; le ZIP autonome contient les 54 illustrations utilisées.

Ce catalogue conserve la référence publique. Les composants, outils et rendements sont
maintenant transposés dans [l'économie jouable](ECONOMIE_CYBERDECK.md), avec ses propres
nomenclatures de blocs de 1 m. Les nomenclatures comparatives ci-dessus décrivent
explicitement Space Engineers grande grille 2,5 m à construction complète ; elles ne constituent
pas un équilibrage validé des blocs VoidForge de 1 m. Aucun fichier de jeu ou visuel Keen importé.

Les rendements sont ceux de la raffinerie complète sans module. Le calcul minéral crédite une
seule fois les coproduits de pierre, puis complète avec les minerais dédiés. Les quantités de
composants à souder ne sont jamais divisées par le rendement d'assemblage. Temps, énergie,
stocks existants et pertes de minage ne sont pas inclus.

Vérifications : 36 PNG RGBA et marges contrôlés ; 54 SHA-256 de catalogue et 36 copies sources
identiques ; calculs des trois profils et couverture des ingrédients vérifiés ; 8 contrôles
Chrome réussis, dont chargement des images, filtre, quantité/rendement, crédits de pierre,
décomposition d'un bloc, largeur mobile et absence d'exception JavaScript.
Captures des trois familles relues visuellement, avec contrôle séparé de la ferraille.
Les rapports JSON et captures sont dans le dossier de livraison. Aucune ouverture Unity,
modification de Resources ou intervention sur le build équipement pendant cette passe.

Le pack `VoidForge-Resources-v2.zip` contient 74 fichiers, dont les 54 illustrations
référencées. CRC de l'archive, chemins autonomes et SHA-256 des 54 images vérifiés.
Taille : 76 659 125 octets ; SHA-256 :
`3f23180552f3f7ce02a61e2115e6eb897f0f261bd590c6d715527fc4d88905b0`.
