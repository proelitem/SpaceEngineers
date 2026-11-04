# Intégration 02 — corrections de la recette équipement

Retour de Pierre du 3 novembre 2026 sur l'APK d'intégration 01 : outils à placer à la
ceinture, doigts qui se ferment vers le dos de la main, cyberdeck caché derrière le bras
et sangles inutiles, disque de meuleuse qui tourne autour du mauvais axe.

## Corrections

- Trois étuis suivent `Hips`, poignées à hauteur de ceinture, pointes vers le bas.
  Foreuse à droite, meuleuse devant, soudeuse à gauche. Prise droite de proximité conservée.
- Boutons de rappel conservés par le contrat `EquipmentRig.Take(HandTool)` : attente
  du premier grip avant activation, puis relâchement pour ranger. Le HUD producteur
  reste un travail distinct ; aucun de ses fichiers inconnus n'est inclus dans cette passe.
- Axes de fermeture des trente phalanges recalibrés sur le côté anatomique de la paume
  dans la copie intégrée, sans modifier la source de l'astronaute en production.
- Cyberdeck sur la face intérieure de l'avant-bras gauche. Normale/UV et dégagement de
  l'armure recalculés. Deux sangles retirées de l'export dérivé, sans modifier les sources.
  Le premier contrôle après résolution VRIK échoue :
  `InvalidOperationException: Integration validation: screen must face the palm side after VRIK`.
  Le montage statique ne suit pas la torsion distribuée par le solveur. Un montage
  `WristDisplayMount` s'exécute après VRIK (10010 après 9998) ; la normale de paume
  projetée et l'axe réel de l'avant-bras définissent le repère final de consultation.
- Axe local du disque déduit de son épaisseur ; pivot recentré sur son centre géométrique.
  Le FBX de l'outil reste intact, la correction vit dans le montage généré.
- L'excavation lit `HandTool.IsOperating` lorsque l'équipement est présent, pour que
  la gâchette de sélection UI ne déclenche pas le creusage avant le premier grip.

## Vérification

`IntegrationPreview.BuildAndReviewBatch` termine avec **code 0** le 3 novembre à
21 h 03 (`Logs/integration02-review-3b.log`). Compilation éditeur réussie avec Unity
**6000.4.0f1** et dépendances locales de l'astronaute/FinalIK.

- Trois poignées à hauteur de hanches, distance au bassin inférieure à 42 cm et pointes
  au moins 10 cm plus bas que les poignées. Prises, rangement et transitions simulées
  de tracking/focus passent ; rappel main ouverte puis grip/relâchement vérifié.
- Huit doigts longs : déplacement de la troisième phalange vers la paume supérieur à
  10 mm à 65 % de fermeture, sur les deux mains. Les pouces restent à régler visuellement
  sur chaque poignée en casque ; la calibration porte sur les trente articulations.
- Disque en main : quart de tour, normale conservée (produit scalaire > 0,9999) et
  déplacement du centre inférieur à 0,1 mm. L'origine et le FBX source restent intacts.
- Dalle côté paume après VRIK, posture de lecture tournée vers les yeux vérifiée.
  Écart à l'axe du bras **0,1241188 m**, marge comprise. La mesure ne retient que les
  sommets influencés par les os de la manche gauche : un montage intermédiaire trop
  éloigné incluait le torse dans la mesure et a été remplacé avant livraison.
- Export Cyberdeck : deux sangles omises, SHA-256 du GLB source inchangé ; les sept
  autres entrées du catalogue de conversion sont identiques. Aucun fichier source supprimé.
- Véritable VRIK, recentrage, matériaux URP et grille de blocs précédents passent encore.

Preuve : [contrôle Unity](../Preuves/INTEGRATION_02/unity-validation.json).
Aperçus locaux examinés dans `Builds/IntegrationPreview/` : prise intérieure,
`wrist-from-eyes.png`, `left-wrist.png`, `grinder-disc-0.png` et `grinder-disc-90.png`.
Ils contrôlent la géométrie ; le HUD est vérifié séparément par sa livraison producteur.

Le HUD producteur a ensuite passé ses 36 contrôles visuels et ses 10 assertions Play
sur le montage final (rapport local `docs/validation/cyberdeck-2026-11-03/game-validation.json`,
21 h 08). Ce contrôle ne certifie pas l'ensemble du journal : à la sortie Play,
`Logs/cyberdeck-game-1.log` relève aussi
`UnityException: get_internalHeightmapResolution can only be called from the main thread.`
La pile passe par `TerrainCutter.GetCreateHoles`, `VoxelChunk.UpdateVoxelsOnSurface` et
`DiggerSystem.Modify`. Cette exception du prototype Digger reste ouverte ; aucune
modification du fournisseur ni contournement masquant l'erreur dans cette passe.
Les fichiers du HUD restent la livraison productrice distincte, préservée hors de ce commit.

## APK et contrôle Quest

- Unity et Gradle réussissent, **code 0**. Durée rapportée par Unity : **3 727 s**,
  dont une longue construction native ; caches conservés et concurrence native limitée à 4.
- APK : `Builds/VoidForge-Quest3-integration02-dev.apk`, **124 641 831 octets**,
  version **1.0 / code 10**, **arm64-v8a**, minSdk **32**, targetSdk **36**.
  Le résumé Unity de 1 437 Mo inclut d'autres sorties ; la taille ci-dessus est celle
  du fichier APK mesuré directement.
- SHA-256 : `D038F6AE6956E53C2C259B7C42BD17046B9F1D06EF99AF30E8602E87FBDDD4F6`.
- Installation USB : **Success** ; version 10 confirmée par `dumpsys package`.
  Lancement à froid **COLD / Status: ok**, PID **30442** encore présent après la capture.
- Capture du 3 novembre à **22 h 22**, durée hôte **33,973 s**, 15 intervalles de 2 s
  avec réveils ADB périodiques. Journaux live et tampon final examinés ensemble :
  **0 exception / E-F Unity / AndroidRuntime / XR_ERROR** dans cette fenêtre.
- Initialisation IL2CPP confirmée : astronaute/poignet/3 outils raccordés, huit blocs
  affichés sur **35 cellules**, cinq vues HUD attachées au poignet, auto-test Digger
  réussi avec recul du collider **0,649 m**. Aucun port du casque ni activation physique
  des outils ne sont déduits de ces marqueurs.
- Avertissements : Profiler non connecté puis arrêté après **134 234 112 octets** tamponnés
  pour un plafond de **134 217 728 octets** ; métriques Meta avant session prête et
  `Orientation Listener cannot detect orientation`. Aucune exception Digger observée
  dans cette capture Quest ; l'exception de sortie Play décrite plus haut reste ouverte.

Preuve : [contrôle Quest](../Preuves/INTEGRATION_02/quest-validation.json).
Journaux locaux : `Logs/build-quest3.log`,
`Logs/quest-integration02-20261103-222235-live.log` et `...-unity.log`.
APK, journaux bruts, symboles et caches restent hors Git. Aucun réglage permanent
du casque n'est modifié par les réveils périodiques.

## Recette restante par Pierre

- Ceinture accessible assis/debout, y compris la soudeuse à gauche avec la main droite.
- Fermeture des doigts et position des pouces sur les trois poignées.
- Cyberdeck visible et lisible dans la posture de consultation réelle, sans gêne ni occlusion.
- Disque de meuleuse stable dans son carter pendant l'activation.
- Confort et performances de cette version ; réseau à plusieurs appareils toujours distinct.

Les approbations propriétaire restent réservées aux décisions engageantes du coffre.
Ces corrections techniques sont déjà autorisées par le retour de recette de Pierre.
