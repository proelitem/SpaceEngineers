# Scène de départ et recette corrective — intégration 03

Extension demandée par Pierre le 4 novembre 2026. Cette tranche remplace les présentoirs
de démonstration par une boucle solo de minage, atelier et construction. La validation
J1 des contrôles ne certifie pas l'ergonomie des nouveaux modèles. Les résultats observés
et les points restant à porter en casque sont dans
[le jalon INTEGRATION_03](../Hermes_Works/Jalons/INTEGRATION_03.md).

## Parcours

1. Dans l'inventaire du Cyberdeck, sélectionner foreuse, soudeuse ou meuleuse. Un seul
   outil apparaît dans le holster de la cuisse droite. Approcher la main à moins de
   18 cm de sa poignée puis maintenir le grip droit pour prendre. Relâcher le grip
   le remet au holster. La sélection ne téléporte plus l'outil dans la main.
2. Approcher un astéroïde et maintenir la gâchette de la foreuse, pointe à moins de
   1 m. La roche et les minerais sont retirés du volume Digger. Le matériau réellement
   extrait rejoint le sac ; le surplus devient un ramassage persistant.
3. Revenir à moins de 8 m de l'atelier pour transférer les stocks et utiliser le
   raffinage/assemblage depuis le Cyberdeck. La base comporte quatre armures en carré
   2 × 2, une batterie chargée, une raffinerie, un assembleur et une soute. Les stocks
   initiaux et les recettes proviennent du registre économique existant.
4. Dans **Construction**, sélectionner un bloc. **A** pose son plan bleu, **X** parcourt
   les 24 orientations. Un premier plan définit une nouvelle grille, partout où son
   volume ne rencontre aucun obstacle. Les suivants doivent toucher cette grille.
5. Prendre la soudeuse et maintenir sa gâchette sur un plan à moins de 1 m pendant
   deux secondes. Le besoin complet en composants doit être disponible dans le sac
   ou la soute accessible. La consommation intervient une fois à la fin du soudage.
   Un plan sans composants reste bleu. La meuleuse restitue les composants d'un bloc
   terminé et peut annuler un plan ; elle préserve la connexion et les stocks protégés.
6. La commande **Cibler** du GPS affiche un cadre autour de l'entité sélectionnée.
   Un chevron périphérique donne sa direction lorsqu'elle sort de la vue.
7. Un cockpit construit conserve **A** pour entrer/sortir hors de la page Construction.
   Le cockpit reste statique ; aucun pilotage physique de vaisseau n'est livré ici.

Deux manettes Quest suivies sont le mode de recette. Le pointeur natif conserve ses
entrées souris/clavier pour le HUD dans l'éditeur. Le jeu complet sans manettes et
le suivi des mains nues ne sont pas implémentés.

## Mains, holster, poignet et vue

La livraison d'astronaute reste intacte. `Build/correct-astronaut-hands.ps1` produit une
copie locale sous `Assets/ThirdParty/VoidForgeIntegration/CorrectedAstronaut` : la peau
HVR et le repos des phalanges sont replacés du bon côté de la paume ; les protections
dorsales restent en place. Les cinq doigts, 54 os, poids et noms sont conservés.
La commande de flexion garde la règle du producteur. Le critère de contrôle initial
sur le pouce a été corrigé : en pose de repos paumes vers l'avant, le pouce est latéral.
La recette vérifie aussi la fermeture des huit doigts longs, les prises et le rendu.

Les holsters suivent `RightUpLeg` après VRIK, depuis un parent d'échelle unitaire.
Ils n'héritent plus de l'échelle du squelette importé. `HandTool` mémorise et restaure
son échelle locale avec sa position et sa rotation. Les prises répétées sont contrôlées.

L'export dérivé du Cyberdeck omet les deux sangles et les deux pieds de selle qui
créaient 21 mm d'écartement sous le boîtier. Le GLB source reste intact. Des points
de surface issus du skinning, y compris à l'intérieur des triangles de la manche,
permettent de recaler l'appui après FinalIK. Le plan d'appui est assis de 1 mm dans
la manche, sans marge d'air ajoutée. La proximité visuelle reste à confirmer en casque.

Le repère visuel est mesuré à 5 mm devant la visière. L'avatar et la cible de sa tête
s'alignent sur ce repère ; la pose suivie de la caméra XR et son IPD restent pilotées
par OpenXR. Le masque local du casque évite les triangles dans la vue.

## Astéroïdes et minerais

Les 64 astéroïdes réutilisent les douze formes Dark Anvil de `G:/UnityAssets`.
Le voxeliseur natif Digger calcule leurs SDF depuis les maillages LOD0 : 41³ échantillons
par variante, avec un pas normalisé de 1/32. Les données dérivées restent locales sous
`Assets/ThirdParty/VoidForgeSurvival`, avec leurs textures sous licence.

À distance, les trois LOD des assets sont conservés. À moins de 8 m de leur enveloppe,
les astéroïdes sont convertis successivement en meshes/colliders Digger à pas de 0,25 m.
Le visuel et le collider de présentation sont alors désactivés. Tous les astéroïdes
disposent de cette conversion ; aucune sphère d'essai isolée n'est chargée.

`AsteroidOreProfile` versionne la graine et les veines continues, avec pierre, fer,
nickel, silicium, cobalt, magnésium, argent, or, platine, uranium et glace. Les indices
de matériau des voxels commandent à la fois le rendu et le minerai crédité. Le comptage
évite les échantillons de bord partagés entre chunks. La calibration de collecte est
de **2,5 kg par unité de variation SDF**, héritée du prototype économique.

Le profil numérique est un réglage original VoidForge. Les sources publiques décrivent
la pierre et les gisements de minerais dans les astéroïdes :
[Asteroid](https://spaceengineers.wiki.gg/wiki/Asteroid),
[Ore](https://spaceengineers.wiki.gg/wiki/Ore).
**L'équivalence exacte des probabilités, profondeurs, dimensions et masses avec Space
Engineers n'est pas certifiée.** Aucun code ni fichier de ressources Keen n'est lu,
importé ou compilé pour cette génération.

## Données et limites

`SurvivalSession` reprend la boucle automatique via les ports publics de
`EconomyRuntime` et `CyberdeckRuntime`. Le registre `EconomyWorld` garde l'autorité sur
les stocks, recettes et énergie ; `SurvivalDeckAdapter` publie le HUD existant.
`ConstructionSystem` conserve les plans et les repères physiques des grilles.

La sauvegarde atomique `survival-world-v1.json` regroupe le registre, les poses des
grilles, les plans inachevés, le choix d'outil et les excavations identifiées. Le fichier
économique précédent est conservé. Les trous sont rejoués sans nouveau crédit.
Dans l'éditeur, la sauvegarde normale est dans `Library/SurvivalPlaytest` ; les recettes
utilisent exclusivement un nouveau chemin sous `Library/SurvivalValidation`.

Cette tranche reste solo, avec grilles statiques. Les commandes et la production sont
suspendues en réseau. Le registre des machines et de l'énergie est global au prototype ;
il ne simule pas les réseaux électriques ou inventaires séparés de plusieurs vaisseaux.
Limites actuelles : 512 blocs/plans, 12 grilles enregistrées, coordonnées locales ±24 cellules
et 8192 excavations. Le chargement rejoue les excavations ; sa montée en charge et les
temps de trame Quest doivent être mesurés. DRILL-R reste dans le catalogue d'intégration
local ; le minage piloté depuis un vaisseau est un travail distinct.

## Reproduction

Utiliser Unity 6000.4.0f1 et son module Android. Fournir les dépendances locales décrites
dans [UNITY_ASSETS](UNITY_ASSETS.md), [ASTRONAUTE](ASTRONAUTE.md) et
[ECONOMIE_CYBERDECK](ECONOMIE_CYBERDECK.md). La livraison d'astronaute/HVR et FinalIK reste
une entrée locale nécessaire ; les maillages dérivés ne sont pas redistribués en MIT.

```powershell
./Build/stage-survival.ps1
# Génération ; ne lancer aucun autre éditeur sur ce projet.
& 'D:/Unity/6000.4.0f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -buildTarget Android -executeMethod VoidForge.Editor.SurvivalWorkshop.BuildBatch -logFile Logs/survival-staging.log
# Recette graphique + deux chargements ; chemin neuf sous Library/SurvivalValidation.
& 'D:/Unity/6000.4.0f1/Editor/Unity.exe' -batchmode -force-d3d11 -projectPath . -buildTarget Android -executeMethod VoidForge.Editor.SurvivalValidation.RunBatch -vfSurvivalSave Library/SurvivalValidation/recette-neuve.json -logFile Logs/survival-play.log
```

Fixer `GRADLE_USER_HOME` à `Library/GradleUserHome` et `BEE_BUILD_THREADS=4` avant le
build Android. Les `.meta`, la scène, l'équipement et les réglages de cette tranche
sont conservés ; `Library`, `Logs`, `Builds` et les dépendances sous licence restent locaux.

Build et recette USB : [BUILD_QUEST3.md](BUILD_QUEST3.md#recette-de-la-scène-survie--intégration-03).
L'APK 12 est installé sur Quest. Deux captures de plus de 35 s passent sans défaut
Unity/AndroidRuntime/XR_ERROR relevé ; les huit blocs et les stocks sont conservés.
Le rapport [quest-validation.json](../Hermes_Works/Preuves/INTEGRATION_03/quest-validation.json)
conserve la portée exacte du contrôle, sans casque porté.
