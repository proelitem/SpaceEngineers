# Minage — exigences et suite du prototype J1

Décisions de Pierre du 3 novembre 2026. Ce document décrit la cible à réaliser ;
le prototype J1 valide seulement le creusage solo d'un volume isolé avec Digger.
Pierre confirme que le creusage fonctionne et que les performances ressenties sont élevées.
Les mesures instrumentées restent à relever.

Modèles repérés et copie de DRILL-R : [sélection des ressources d'outils](OUTILS_ASSETS.md).

## Exigences retenues

| Sujet | Comportement cible | État |
|-------|--------------------|------|
| Forme des astéroïdes | Les volumes creusables reprennent les formes des assets d'astéroïdes de G:/UnityAssets | À réaliser ; les FBX sont actuellement décoratifs et le volume de test est sphérique |
| Minerais | Des veines internes suivent un profil reprenant les paramètres de Space Engineers | À spécifier depuis des références publiques, puis à implémenter |
| Outil à main | Un appareil équipé et utilisé par le joueur permet de miner et de récupérer le minerai | À réaliser ; la gâchette du J1 commande directement la brosse de test |
| Outil de vaisseau | Un outil monté sur le vaisseau permet de miner et d'acheminer le minerai vers son stockage | À réaliser ; dépend des fondations de construction et de vaisseau |

## Formes et volumes

La silhouette extérieure de chaque astéroïde creusable doit suivre son asset source.
La conversion du maillage en volume éditable reste à prototyper et à vérifier avec
la résolution voxel, les matériaux, les colliders et les budgets du Quest 3.
Le succès du creusage sphérique J1 ne valide pas encore cette conversion.
Consulter les outils disponibles dans G:/UnityAssets avant toute nouvelle implémentation.

## Profil de minerais de référence

Conserver un profil de données identifiable et versionné. Avant de renseigner ses valeurs,
documenter la version de Space Engineers prise comme référence et les sources publiques.
Le relevé doit couvrir la composition en minerais, leur abondance/rareté, la profondeur,
la taille et la distribution des veines, ainsi que les rendements et réglages de collecte
nécessaires à l'équivalence demandée. Aucun nombre n'est présenté comme vérifié à ce stade.

La génération doit être reproductible à partir de la graine de l'astéroïde et du profil.
Les minerais occupent le même volume 3D que la roche : creuser révèle les veines et retire
la matière correspondante. Le rendu du minerai et la quantité récupérée doivent se fonder
sur les mêmes données. Le code et les ressources du dépôt Keen restent exclus.

## Outils et extraction

Les deux familles d'outils utilisent le même traitement : vérifier l'outil actif et sa
portée, retirer la matière, identifier le minerai, puis créditer l'inventaire destinataire.
Leurs paramètres de travail (portée, zone d'action, cadence et capacité de collecte)
restent configurables et seront réglés pendant la recette en casque.
Le minage final exige un outil ; la brosse directe du J1 reste un moyen de test.

La production des modèles originaux de foreuse portative, meuleuse et soudeuse
est suivie dans [OUTILS_MAIN.md](OUTILS_MAIN.md). Les modèles préparent les prises,
points de contact et assemblages mobiles ; les mécanismes décrits ici restent à réaliser.

Préparer l'autorité réseau et la sauvegarde dès la conception : une quantité extraite ne
peut être créditée qu'une fois, les clients doivent retrouver les mêmes veines et trous,
et la sauvegarde doit conserver le résultat. Ces mécanismes ne sont pas livrés au J1 ;
le prototype reste solo et se réinitialise au lancement.

## Recette à atteindre

1. Comparer l'astéroïde creusable à la silhouette de son asset, puis creuser et traverser
   la zone excavée pour contrôler le maillage et les collisions.
2. Vérifier les valeurs du profil de minerais contre les références publiques retenues,
   puis retrouver les mêmes veines avec la même graine.
3. Extraire avec un outil à main puis avec un outil de vaisseau ; vérifier que la matière
   retirée correspond au minerai crédité, sans duplication.
4. Vérifier la cohérence après sauvegarde/rechargement et avec deux appareils.
5. Mesurer le temps de trame et les pointes pendant le creusage sur Quest 3.

Suivi : [feuille de route](ROADMAP.md), [architecture](ARCHITECTURE.md),
[recette J1](TEST_J1.md) et [preuves du prototype](../Hermes_Works/Jalons/J1_ASSETS.md).
