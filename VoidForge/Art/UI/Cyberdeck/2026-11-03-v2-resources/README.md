# Catalogue industriel — VoidForge Odyssey

Profil public vérifié le 2026-11-03. **Référence de conception, pas économie active du jeu.**

## Inventaire des manques

Avant : 3 minerais (fer, nickel, silicium), fer raffiné, plaques, tubes génériques et circuits génériques. Les trois images de composants existantes sont réaffectées explicitement à plaque d’acier / petit tube / ordinateur, sans modifier les fichiers v1.

| Famille | Images à produire |
|---|---|
| minerais (8) | Minerai de cobalt, Minerai de magnésium, Minerai d’argent, Minerai d’or, Minerai de platine, Minerai d’uranium, Pierre brute, Glace |
| produits (9) | Lingot de nickel, Lingot de cobalt, Plaquettes de silicium, Poudre de magnésium, Lingot d’argent, Lingot d’or, Lingot de platine, Pastilles d’uranium, Gravier |
| recyclage (1) | Ferraille |
| recettes (18) | Éléments de construction, Plaque intérieure, Grand tube, Grille métallique, Moteur électrique, Écran, Poutrelle, Verre renforcé, Composant de réacteur, Composant de propulsion, Composant gravitationnel, Composant médical, Composant radio, Composant de détection, Charge explosive, Cellule solaire, Cellule énergétique, Supraconducteur |

Après ce lot : **11 matières brutes, 10 raffinées, 21 composants, 1 ferraille**, avec 3 outils et 8 variantes de blocs de référence ; 54 entrées et 36 images nouvelles.

## Coûts et réglages

Les quantités d’assemblage du wiki officiel correspondent au réglage réaliste ×1. À ×3 ou ×10, diviser les matières raffinées par ce multiplicateur. Les nombres de composants d’un bloc restent entiers et inchangés. Les masses de produits finis ne se déduisent pas des ingrédients. [Réglages officiels](https://spaceengineers.wiki.gg/wiki/World_Settings).

Raffinerie complète sans modules de rendement : Fe 70 %, Ni 40 %, Co 30 %, Si 70 %, Mg 0,7 %, Ag 10 %, Au 1 %, Pt 0,5 %, U 1 %. Pour 1000 kg de pierre : 14 kg de gravier, 30 kg Fe, 2,4 kg Ni, 4 kg Si. Ferraille : 80 % Fe. La glace se traite dans un générateur O₂/H₂, pas en lingots. [Raffinerie](https://spaceengineers.wiki.gg/wiki/Refinery), [minerais](https://spaceengineers.wiki.gg/wiki/Ore).

L’équivalent minerai est un parcours calculé : minerais dédiés pour les métaux, pierre pour le gravier. Les coproduits Fe/Ni/Si de cette pierre sont crédités une seule fois ; leurs surplus sont explicités dans JSON. Il ne s’agit pas d’une optimisation de tous les parcours possibles ni d’un coût avec stocks/récupération. Énergie, temps et pertes de minage exclus.

## Recettes de composants — une unité, ×1

| Produit | Matières raffinées (kg) | Minerais bruts équivalents (kg) |
|---|---|---|
| Plaque d’acier | Lingots de fer 21 | Minerai de fer 30 |
| Éléments de construction | Lingots de fer 8 | Minerai de fer 11.429 |
| Plaque intérieure | Lingots de fer 3 | Minerai de fer 4.286 |
| Petit tube | Lingots de fer 5 | Minerai de fer 7.143 |
| Grand tube | Lingots de fer 30 | Minerai de fer 42.857 |
| Grille métallique | Lingots de fer 12, Lingot de nickel 5, Lingot de cobalt 3 | Minerai de fer 17.143, Minerai de nickel 12.5, Minerai de cobalt 10 |
| Moteur électrique | Lingots de fer 20, Lingot de nickel 5 | Minerai de fer 28.571, Minerai de nickel 12.5 |
| Ordinateur | Lingots de fer 0.5, Plaquettes de silicium 0.2 | Minerai de fer 0.714, Minerai de silicium 0.286 |
| Écran | Lingots de fer 1, Plaquettes de silicium 5 | Minerai de fer 1.429, Minerai de silicium 7.143 |
| Poutrelle | Lingots de fer 6 | Minerai de fer 8.571 |
| Verre renforcé | Plaquettes de silicium 15 | Minerai de silicium 21.429 |
| Composant de réacteur | Lingots de fer 15, Lingot d’argent 5, Gravier 20 | Pierre brute 1428.571, Minerai d’argent 50 |
| Composant de propulsion | Lingots de fer 30, Lingot de cobalt 10, Lingot d’or 1, Lingot de platine 0.4 | Minerai de fer 42.857, Minerai de cobalt 33.333, Minerai d’or 100, Minerai de platine 80 |
| Composant gravitationnel | Lingots de fer 600, Lingot de cobalt 220, Lingot d’argent 5, Lingot d’or 10 | Minerai de fer 857.143, Minerai de cobalt 733.333, Minerai d’argent 50, Minerai d’or 1000 |
| Composant médical | Lingots de fer 60, Lingot de nickel 70, Lingot d’argent 20 | Minerai de fer 85.714, Minerai de nickel 175, Minerai d’argent 200 |
| Composant radio | Lingots de fer 8, Plaquettes de silicium 1 | Minerai de fer 11.429, Minerai de silicium 1.429 |
| Composant de détection | Lingots de fer 5, Lingot de nickel 15 | Minerai de fer 7.143, Minerai de nickel 37.5 |
| Charge explosive | Plaquettes de silicium 0.5, Poudre de magnésium 2 | Minerai de silicium 0.714, Minerai de magnésium 285.714 |
| Cellule solaire | Lingot de nickel 3, Plaquettes de silicium 6 | Minerai de nickel 7.5, Minerai de silicium 8.571 |
| Cellule énergétique | Lingots de fer 10, Lingot de nickel 2, Plaquettes de silicium 1 | Minerai de fer 14.286, Minerai de nickel 5, Minerai de silicium 1.429 |
| Supraconducteur | Lingots de fer 10, Lingot d’or 2 | Minerai de fer 14.286, Minerai d’or 200 |

Source des ingrédients : [table des composants](https://spaceengineers.wiki.gg/wiki/Component). Les valeurs sont des faits de jeu transcrits dans un schéma original ; aucun fichier de jeu, code ou visuel Keen importé. Les pages directes ont renvoyé HTTP 403 : vérification via leurs extraits indexés, consultés avec recherche web.

## Blocs et périmètre

Les 8 recettes de blocs sont des comparaisons **SE grande grille 2,5 m, variante nommée, construction complète**. Les couches dites optional dans le wiki sont incluses pour atteindre 100 %. Les blocs VoidForge sont de tailles différentes : ces coûts ne sont pas adoptés automatiquement. Les totaux sont recalculés depuis la nomenclature et les coûts unitaires ; les résumés de masses de certains articles du wiki ne concordent pas avec leurs propres tableaux (notamment l’assembleur).

Le périmètre est la chaîne industrielle classique des interfaces actuelles, cohérente avec la référence autour de 2016 de docs/ROADMAP.md. Les chiffres vérifiés proviennent du wiki actuel ; **aucune certification d’un état exact du jeu en 2016**.

Différences modernes inventoriées mais hors de ce lot :

- Prototech : Prototech Scrap, Prototech Frame, Prototech Cooling Unit, Prototech Circuitry, Prototech Machinery, Prototech Propulsion Unit, Prototech Capacitor, Prototech Panel. Branche industrielle avancée postérieure au socle classique de la feuille de route ; non couverte par ce lot. Frame non fabricable.

- Autres fabrications SE : Canvas, Datapad, Oxygen Bottle, Hydrogen Bottle, Medkit, Powerkit, outils améliorés, armes et munitions, alimentation et agriculture. Équipement, survie, combat et extensions hors des interfaces et de la chaîne industrielle classique demandées ici.

- Objets non fabricables : Zone Chip, Space Credits, plushies, consommables commerciaux. Pas de recette minérale d’assemblage à attribuer.

## Images et utilisation

36 appels distincts au générateur intégré `image_gen.imagegen`, mode built-in, fond transparent demandé ; pas de spritesheet ni de découpage, pas de conversion de pixels. PNG originaux conservés. `generation-prompts.json` contient les prompts ; `selected-sources.json` les sorties sélectionnées ; `manifest.json` leurs tailles, alpha et SHA-256.

Galerie et calculateur : `index.html`, sans serveur ni dépendance réseau. Export : `catalogue.json`, `recipes.csv`, `refining.csv`. Les références v1 sont relatives ; le ZIP final contient les images v1 utilisées et leur copie locale avec références autonomes.

Bibliothèque locale consultée : index Jarvis et packs `2D_Icons_-_Space_Rank` (grades et emblèmes) / `2000_Fantasy_Icons` (fantasy). Inadaptés à ces matières industrielles et au style original demandé ; aucune ressource tierce copiée.

Aucune modification des Assets/Resources, de l’économie, de la scène ou des fichiers réservés au build équipement. Les recettes illustratives historiques du HUD restent identifiées comme telles jusqu’au branchement de l’économie.
