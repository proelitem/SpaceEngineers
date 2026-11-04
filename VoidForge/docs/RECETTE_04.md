# Recette 04 — équipement, construction et modules

Extension explicitement demandée par Pierre le 4 novembre 2026. Cette livraison reste une
recette solo du prototype : elle ne clôt pas les jalons complets J2/J3 ni le réseau.
La grille canonique est dans `docs/TEST_J1.md` ; les résultats mesurés et livrables dans
`Hermes_Works/Jalons/INTEGRATION_04.md`.

## Équipement

- Les anciens volumes de plaques des gants étaient décalés par rapport à la peau après
  correction de la latéralité. Le dérivé local reconstruit leur dessous à partir des faces
  dorsales de cette peau : extrusion de 1,5 mm, mêmes poids et mêmes joints. Les doigts
  gardent leur sens de flexion. La source Blender/HVR reste intacte, avec SHA-256 contrôlé.
- `LocalAstronaut.controllerPalmEuler` compense le tangage de prise des Touch de −45°.
  Le repère de poignée des outils reçoit la même correction. Le pointeur XR garde son
  repère de visée ; les axes des doigts ne sont pas inversés.
- Une semelle de boîtier conformée à 77 points de la manche skinnée ferme le dessous du
  cyberdeck. Elle suit VRIK après le montage du poignet, sans sangle visible.
- L’ancre HUD est recalée sur le centre réel de la dalle. `WristHudAttachment` suit sa pose
  en mètres du monde, sans hériter de l’échelle des os : écart d’affichage 0,15 mm. Le mode
  agrandi reste devant les yeux ; le repli revient sur la dalle.

## Modèles et construction

Cargo, Refinery et Assembler sont des créations originales GPT-Astra / Blender MCP d’après
les trois références 2D locales. Sources et aperçus : `Art/Modules/2026-11-04`. Leurs FBX sont
sous `Assets/VoidForge/Art/Modules/2026-11-04`. La conversion FBX exige une rotation Y de 180°
à l’import du prefab, vérifiée sur les bornes Unity :

| Bloc | Cellules de 1 m | Triangles source |
|---|---|---:|
| Soute | 1 × 1 × 1 | 5 084 |
| Raffinerie | 1 × 2 × 1, hauteur 2 m | 11 376 |
| Assembleur | 2 × 1 × 1 | 13 844 |

`Recette04Workshop` remplace les trois entrées du catalogue économique. Placement, plan bleu
et bloc terminé instancient la même entrée ; aucune substitution par une armure. Une seconde
sélection du même bloc désélectionne son aperçu. Les contrôles de collision restent actifs.

Chaque plan sauvegarde ses composants réellement installés. La soudeuse les prélève unité
par unité dans le sac, puis dans la soute accessible du même module. Un composant manquant
arrête l’avancement. Seule une nomenclature complète devient un bloc opérationnel et affiche
ses matériaux normaux. Le panneau de travail affiche pourcentage et compte installé/requis.

La meuleuse retire progressivement ces composants et les rend uniquement au sac personnel.
Un sac plein suspend la récupération. À zéro, le bloc ou le plan disparaît ; un plan vide
peut donc être effacé. Le démontage d’une soute encore remplie ou d’un assembleur ayant des
réservations est refusé pour préserver les ressources. Les colliders sont désactivés dès le
retrait logique, avant la destruction Unity différée. Un emplacement de module vide se réutilise.

## Module, systèmes, énergie et masse

« Module » remplace « Vaisseau » dans la navigation du cyberdeck. Son pictogramme original
ImageGen est conservé dans `Art/UI/Cyberdeck/2026-11-04-module`. L’onglet Systèmes expose les
blocs non structurels entièrement construits, avec commande ON/OFF, état et puissance.
Un clavier AZERTY utilisable au pointeur permet de renommer le module regardé, proche ou en
construction. L’identifiant du module édité est conservé pendant la saisie ; son nom est
sauvegardé et propagé au GPS.

Chaque grille possède son registre de soute et sa file de production. Tous ces registres
partagent le même sac personnel ; ils ne partagent pas leurs réserves énergétiques. Le module
initial conserve la soute et la file de la sauvegarde précédente.

Profil original du kit compact, sans prétention d’équivalence chiffrée à SE :

- Batterie : capacité du catalogue, charge conservée lorsqu’elle est coupée ; sa charge
  devient alors indisponible au réseau du module. Une batterie nouvellement soudée est vide.
- Solaire : 40 kW au maximum par panneau, multipliés par le cosinus d’exposition au soleil
  et trois sondes d’occultation. Un obstacle englobant l’origine d’une sonde est aussi détecté.
- Réacteur : 300 kW pour 0,0001 kg d’uranium par seconde, pris dans la soute du même module.
  Arrêt ou manque de combustible : aucune production. Le boîtier réutilise
  `Sci-Fi_base/Prefabs/Generator_01.prefab`, normalisé dans une cellule et adapté URP.
- Raffinerie : 1 kW de veille et 120 kW supplémentaires en traitement ; assembleur :
  0,5 kW de veille et 50 kW en travail. Un système coupé ne consomme pas et ne produit pas.
- Les surplus chargent les batteries actives ; une production instantanée disponible peut
  aussi alimenter le bus sans batterie. Aucun rattrapage de production hors ligne.

La masse sèche vient des composants installés et de leur nomenclature d’ingrédients. La
masse chargée ajoute le contenu réel de la soute et les matières réservées en fabrication.
La plateforme initiale reste ancrée. Les nouvelles constructions sont des corps rigides sans
gravité. Au cockpit actif : stick gauche translation, gâchettes montée/descente, stick droit
rotation avec gyroscope, grip gauche freinage avec propulseurs. Les forces sont constantes :
une masse supérieure diminue accélération et freinage. La sortie du siège suit le déplacement
du module. Ce pilotage agrégé ne certifie pas la fragmentation des grilles, les propulseurs
individuels ni un système multijoueur de vaisseaux.

## Astéroïdes et effets

Le matériau commun de roche Digger provoquait le changement visible à l’approche. Un shader
triplanaire commun aux meshes distants et aux surfaces creusées conserve maintenant, par
astéroïde, l’albédo de son variant source et le même repère spatial. Les veines utilisent les
mêmes capsules que les voxels. Un chunk Digger contenant plusieurs astéroïdes répartit ses
triangles en sous-maillages par astéroïde. Les colliders et le comptage des minerais restent
ceux de Digger. Les UV originaux ne sont pas conservés : la projection est stable des deux côtés.

Les outils réutilisent `SparksYellow` et `SparksBlue` de Sci-Fi Arsenal. Les dérivés locaux
convertissent leurs matériaux en URP et bouclent tant que la gâchette est maintenue et que la
pointe touche une surface. Les particules sont réutilisées, plafonnées et arrêtent leur émission
au relâchement. La rotation, le soudage, le démontage et le creusage lisent un état maintenu,
avec la cadence de jobs Digger pour le creusage.

## Reproduction

1. Préparer les dépendances de `docs/SURVIE.md`, puis exécuter
   `python Build/stage-tool-effects.py` et `python Build/stage-module-reactor.py`.
2. Régénérer le dérivé de gants avec `Build/correct-astronaut-hands.ps1`, source locale intacte.
3. Unity 6000.4.0f1 : `-batchmode -nographics -quit -projectPath ... -executeMethod
   VoidForge.Editor.Recette04Workshop.BuildBatch -logFile ...`.
4. `Build/test-economy.ps1` ; puis `Recette04Workshop.ValidateBatch` sans `-quit` ni
   `-nographics`, avec un NOUVEAU `-vfSurvivalSave Library/SurvivalValidation/<nom>.json`.
5. La validation de migration utilise seulement la copie locale
   `Library/Recette04Publication/QuestBefore/survival-world-v1.json`, avec
   `Recette04MigrationValidation.RunBatch` et un autre chemin de test neuf.
6. Construire via `QuestBuildPipeline.BuildBatch -vfDev -vfNoProfiler -vfOutput
   Builds/VoidForge-Quest3-integration04-dev.apk`. Utiliser exclusivement le SDK/NDK/JDK Unity ;
   `GRADLE_USER_HOME=Library/GradleUserHome`, `BEE_BUILD_THREADS=4`.

La sauvegarde enveloppe passe au format 2. Les anciens plans de format 1 n’avaient payé aucun
composant avant leur terminaison : leur avancement non payé revient à zéro, sans perte de
stock ni création de composants payés. Une exception de provision de départ est explicite :
si une ancienne sauvegarde de format 1 a moins de 0,01 kWh et aucune source solaire/réacteur
sur la base, un panneau solaire complet est posé une seule fois sur une cellule adjacente libre.
Le marqueur `starterPowerProvisioned` et l’UID dédié empêchent toute répétition. Les composants
et la charge de batterie d’origine restent inchangés. On peut couper la raffinerie pendant la
recharge ou pour réserver le solaire à l’assembleur.
Une sauvegarde illisible ou incompatible est conservée et
interrompt la restauration ; aucun nouveau monde ne doit la remplacer.

## Provenance et limites

L’index `_jarvis_index.json` et les packs concernés ont été consultés. Space Sim Framework et
Ingame Spaceship Builder apportent des contrôleurs et inventaires autonomes pour jeux de
bureau ; leur import aurait créé une seconde autorité de stocks et remplacé les entrées XR.
Leurs systèmes ne sont donc pas repris. Les particules et le boîtier compatibles sont réutilisés.

Arsenal, Sci-Fi Base, Digger, HVR et FinalIK restent locaux et exclus du dépôt MIT. Le stage du
réacteur retire seulement un ancien objet éditeur de version de matériau URP introuvable et
une référence « No Name » de FBX de collision. Les textures et modèles sources sont conservés.
Les manifestes locaux consignent les fichiers et leurs SHA-256. Aucun fichier de la banque
n’est modifié, aucun code ou asset Keen n’est lu, importé ou compilé.

Les résultats géométriques et les tests automatisés ne remplacent pas une nouvelle recette
portée : attaches, angle des mains, prise des outils, lisibilité, confort, performance et réseau
restent à tester par Pierre sur cette version.
