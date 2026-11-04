# Économie jouable et Cyberdeck

Extension demandée par Pierre le 3 novembre 2026 : intégrer les ressources et recettes
à la mécanique existante. Le profil actif est `voidforge-economy-1m-v1` dans
`Assets/Resources/VoidForge/Economy/Catalogue.json`.

## Parcours de jeu

1. Prendre la foreuse à la ceinture ou la préparer dans le Cyberdeck, puis maintenir
   le grip droit. Approcher sa pointe à moins de 1 m d'un gisement et utiliser la gâchette.
   Le minerai rejoint le sac ; le gisement diminue et reste épuisé après rechargement.
2. Revenir à moins de 8 m de l'atelier. Dans **Inventaire**, sélectionner le stock
   **Sac personnel**, une ligne, puis **Déposer en soute**. La source **Atelier · Soute**
   permet l'opération inverse. Le transfert respecte la place disponible ; un outil tenu
   reste dans le sac. Les deux filtres distinguent matières brutes et matériaux/composants.
3. Approcher le cockpit et appuyer sur **A** (ou **E** dans l'éditeur) pour s'asseoir.
   **Vaisseau → Raffinerie** affiche les vrais stocks, le traitement et ses coproduits.
   La flèche permet de prioriser un autre minerai présent en soute ; marche/arrêt et pause
   agissent sur la machine. La glace est stockable, mais n'est pas un minerai à raffiner.
4. **Assemblage** donne accès aux 21 composants et aux trois outils standards.
   Les flèches parcourent les recettes. Les quantités et ingrédients sont vérifiés ensemble ;
   ajouter une fabrication réserve ses matières en soute. Le résultat rejoint cette soute.
   La croix annule le travail restant et restitue ses ingrédients si la soute a de la place.
5. **Construction** sélectionne un des 11 blocs. **Coût · Détails** donne le besoin
   complet en composants et le stock accessible. Sortir du cockpit, prendre la soudeuse,
   viser une face de la grille : le contour cyan indique un emplacement valide et abordable.
   La gâchette construit ; **X** (ou **R**) parcourt les 24 orientations. Les cellules occupées
   ou non adjacentes sont refusées sans consommation.
6. La meuleuse démonte un bloc visé et restitue son coût réel en composants. Elle ne peut
   pas couper la grille en deux, supprimer le dernier stockage rempli ni supprimer une
   capacité d'assemblage nécessaire aux matières déjà réservées.
7. **A/E** ou **Sortir du cockpit** rend la locomotion. **Y** agrandit le Cyberdeck pour
   consulter les ingrédients détaillés ; les interactions restent celles du HUD natif.

## Données et règles

- 46 objets de stock : 11 matières brutes, 10 raffinées, 21 composants, trois outils et
  la ferraille ; 75 graphiques originaux dans le HUD, dont les 36 PNG du second lot.
- 24 recettes d'assemblage et 11 recettes de raffinage/recyclage. Les coûts des composants
  reprennent le profil public classique documenté dans [le catalogue](CATALOGUE_RESSOURCES.md),
  au réglage d'assemblage **×1**. Le fichier de données peut aussi sélectionner ×3 ou ×10.
- Les 11 nomenclatures de blocs de **1 m** sont des paramètres originaux de prototype,
  distincts des comparaisons SE de 2,5 m : trois armures, gyro, batterie, propulseur, cockpit,
  raffinerie, assembleur, soute et panneau solaire. Leurs dimensions reprennent les prefabs
  livrés ; les trois blocs industriels et le solaire disposent de géométries natives originales.
- Sac : **400 L** ; soute : **1000 L par bloc de 1 m³**. Un assembleur réserve au maximum
  **1000 L** de matières en cours. Les volumes d'objets sont des paramètres de prototype :
  0,37 L/kg brut, 0,12 L/kg raffiné, 2 L/composant et 25 L/outil.
- Une batterie stocke **4 kWh**, un panneau fournit **40 kW**. Raffinerie : **120 kW**,
  lot de 10 kg en 2 s ; assembleur : **50 kW**, composant en 2 s, outil en 5 s.
  Ces valeurs sont proposées pour VoidForge, sans prétendre reproduire les machines SE.
  Une nouvelle batterie est vide : la construire et la démonter ne crée pas d'énergie.
- Un nouvel atelier fournit une fois sa grille, une batterie chargée, le solaire, les trois
  outils, 16 plaques et 12 éléments de construction dans le sac ; 100 kg de fer raffiné,
  20 kg de nickel, 20 kg de silicium et 5 kg de cobalt sont dans la soute.
  Recharger une sauvegarde ne réapplique jamais ce contenu initial.

Les quantités du moteur sont décimales, indépendantes de l'arrondi d'affichage. Les coûts
à ×3/×10 sont arrondis vers le haut au micro-kilogramme par unité, puis multipliés par le
nombre d'unités ; scinder une file ne réduit pas son coût. Les composants restent entiers.
Un échec de capacité, recette, placement ou stock n'applique aucune demi-transaction.

## Minage et limites du prototype

Les douze gisements repérés sur la carte sont des stocks finis et persistants, avec les
meshes d'astéroïdes locaux réutilisés lorsqu'ils sont disponibles et des inclusions colorées.
Ce sont des gisements de jeu discrets ; ils ne constituent pas encore des veines voxel
continues reproduisant la profondeur ou la distribution exacte de Space Engineers.

Le volume Digger existant produit de la **pierre** depuis son véritable callback
`ModificationResult.RemovedMatterQuantity`. L'adaptateur attend la création et l'autotest
avant de reprendre la commande de la foreuse ; il ne change pas les sources Digger ni
`MiningPrototype`. Sa calibration est explicitement de **2,5 kg par unité de variation SDF
rapportée**, pas une mesure certifiée de masse volumique. Les opérations réussies sont
identifiées et créditées une seule fois, sauvegardées, puis rejouées sans crédit au chargement.
Si le sac est plein, la pierre restante devient un ramassage physique persistant sur place.

La production et les commandes de cette extension suivent la boucle **solo** actuelle.
Elles sont suspendues pendant une session réseau, comme le creusage J1, pour éviter des
stocks et trous divergents. La réplication des ressources, des blocs et excavations n'est
pas implémentée par cette passe. Les blocs gyro/propulseur sont constructibles et récupérables ;
le pilotage physique d'un vaisseau et la conversion des astéroïdes entiers restent des jalons
distincts. Les recettes de gaz, armes, alimentation et Prototech restent hors du profil classique.

## Architecture et reproduction

`EconomyModel` est le registre sans dépendance Unity : stocks, réservations, production,
énergie, gisements, grille, transactions et restauration validée. `EconomyRuntime` raccorde
les données, prefabs, outils, cockpit et Cyberdeck. `EconomyMiningAdapter` utilise l'API locale
Digger sous `VF_LOCAL_DIGGER`. `CyberdeckSession` reste la frontière commandes/instantanés.
Un survol maintenu du HUD suspend son rafraîchissement afin de conserver le bouton pressé ;
les mises à jour ne reconstruisent pas le menu à chaque image.

Les sauvegardes versionnées sont écrites via un fichier temporaire remplacé atomiquement,
avec une copie `.bak`. Dans l'éditeur : `Library/EconomyPlaytest/world-v1.json` ; sur Quest :
`Application.persistentDataPath/economy-world-v1.json`. Un fichier incompatible ou invalide
est conservé et empêche le chargement ; il n'est pas remplacé par un nouvel inventaire.
Modifier les coûts ou le schéma nécessite de versionner le profil et de prévoir une migration.
Les recettes en attente conservent leur réservation ; aucun rattrapage de production hors ligne.

```powershell
python Build/stage-economy.py
./Build/test-economy.ps1
# Unity 6000.4, sans -quit : l'outil termine après les contrôles.
& 'D:/Unity/6000.4.0f1/Editor/Unity.exe' -batchmode -projectPath . -buildTarget Android -executeMethod VoidForge.Editor.EconomyWorkshop.RunBatch -logFile Logs/economy-native.log
```

`EconomyWorkshop` génère les quatre prefabs manquants et le catalogue fonctionnel en
réutilisant sept prefabs de l'intégration initiale. Les `.meta` existants sont conservés.
Les tests Play utilisent un emplacement de sauvegarde isolé sous `Library/EconomyGameValidation`.
Rapports et rendus : `docs/validation/economy-2026-11-03/`. La recette en casque et les mesures
de performances de cette extension doivent être distinguées des tests automatisés.

Le contrôle technique Android utilise le SDK Unity Hub, un numéro de série explicite et
le package `com.proelitem.voidforge` :

```powershell
python Build/check-economy-quest.py --serial NUMERO_QUEST --apk Builds/VoidForge-Quest3-economy-dev.apk --install --restart
```

`--install` met à jour l'application avec `adb install -r` ; aucune donnée n'est effacée.
`--restart` arrête puis relance cette application après son initialisation pour comparer
les stocks, blocs et gisements sauvegardés. Faire ce contrôle hors d'une session de jeu active.
Les journaux capturés sont filtrés sur son processus ; le rapport est `quest-validation.json`
dans le dossier de recette. Ce contrôle ne simule pas les actions des manettes.

Contrôles observés : 55 assertions du registre d'économie, 27 contrôles Unity de la boucle
et des cinq rendus, puis 10 contrôles du Play de Main. Le Play a confirmé une excavation
Digger réellement créditée (28,129 kg) et sauvegardée, les trois outils, 53 repères et une
dalle lisible de 199,583 × 85,714 mm. Son journal ne contient pas l'exception Digger observée
pendant l'arrêt prématuré de la recette précédente. Ces résultats ne certifient pas un
playtest humain en casque ni une fréquence d'images sur Quest.

Le 4 novembre, Pierre a demandé de réserver la stabilisation et la production d'APK
à son autre processus. Le build Android de cette passe a donc été interrompu après
la compilation native, pendant Gradle ; aucun APK n'a été installé et le contrôle Quest
ci-dessus n'a pas été exécuté. Le créneau Unity principal est libéré. Les caches sont
conservés pour la suite ; le build interrompu a avancé `AndroidBundleVersionCode` à 11.

Bibliothèque consultée avant écriture : index Jarvis, Opsive Ultimate Inventory System et
Survival Engine Online. Ils introduisent respectivement un autre modèle d'inventaire et
NetcodePlus/objets de joueur supplémentaires ; ils ne sont pas substitués au rig et au HUD
existants pour cette boucle décimale. Aucun code de ces packs ou du dépôt Keen n'a été copié.
