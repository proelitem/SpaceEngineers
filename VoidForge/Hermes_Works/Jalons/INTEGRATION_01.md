# Intégration 01 — astronaute, poignet, outils et blocs

Demande de Pierre du 3 novembre 2026, après validation du J1 et production des ressources.
Périmètre : première intégration locale des ressources disponibles ; dépendances en
production préservées. Aucune réutilisation du dépôt Keen.

Spécification et reproduction canoniques : [INTEGRATION.md](../../docs/INTEGRATION.md).

## Résultats

- Unity **6000.4.0f1**, vraie dépendance FinalIK locale. `IntegrationWorkshop.BuildBatch`
  et `IntegrationPreview.BuildAndReviewBatch` terminent avec code **0** après corrections.
- Trois outils : alignement position/rotation, direction physique vers l'avant,
  activation et retour à l'emplacement validés. Partage du grip et scénarios simulés
  de perte/reprise de tracking et de focus validés ; aucun tracking matériel simulé
  n'est présenté comme une recette en casque.
- VRIK : erreur de position de la main droite **2,68 × 10⁻⁷ m**, tête
  **2,67 × 10⁻⁷ m** dans la pose contrôlée. Cibles conservées après déplacement/rotation
  du référentiel. Ces résidus ne sont pas une mesure de précision du tracking Quest.
- Cyberdeck fixé à l'avant-bras gauche : dalle séparée, repère HUD calibré et ratio 21:9.
  Distance mesurée de dalle à l'axe du bras **0,137 m**, armure et marge comprises.
  Aperçu final sans intersection de la plaque d'armure avec la dalle dans la pose contrôlée.
- Catalogue : **8** blocs disponibles, **3** entrées futures indisponibles. Grille 1 m,
  occupation de deux cellules après rotation, rejet de chevauchement et rotation à 45°
  vérifiés. Colliders d'armures convexes et enveloppes de blocs présentes.
- Matériaux de l'équipement contrôlés URP. Aperçus Unity/D3D11 examinés : prises extérieure
  et intérieure de la foreuse, corps local et poignet. Images locales dans
  `Builds/IntegrationPreview/`, non versionnées.

Journaux locaux : `Logs/integration-build-1.log`, `Logs/integration-review-2.log`,
`Logs/integration-review-4.log`, `Logs/integration-review-5.log`.
Rapport structuré : `Logs/integration-validation.json`.

## Corrections issues de la recette

- Les empties FBX ne définissaient pas les axes Unity attendus. Calibration à partir du
  pivot de tête et de la pointe, conservation des ancrages source. Contrôle ajouté sur
  la direction de la géométrie réelle, et base de poignet adaptée à une poignée.
  Échec observé puis corrigé :
  `InvalidOperationException: Integration validation: visible tool geometry does not extend forward: MiningDrill`.
- Orientation du cyberdeck déduite de sa normale et de ses UV ; distance à l'avant-bras
  mesurée sur le skinning pour dégager l'armure. Aucun modèle source modifié.
- Masque de vue locale : **2 978 triangles** de tête/visière retirés d'un maillage dérivé
  local ; source et avatar de production conservés. La ressource dérivée reste hors Git.
- Le rendu URP a enregistré l'éclairage linéaire/température de couleur et MSAA 4 dans
  les réglages générés. Aucun changement de fréquence ou mesure de performance déduit.

## APK et démarrage Quest

- Build Unity terminé avec **code 0**, Gradle réussi. Temps rapporté par BuildReport :
  **4 056 s** (première construction avec les nouvelles dépendances). Aucun cache supprimé.
- APK local : `Builds/VoidForge-Quest3-integration-dev.apk`, **118 731 913 octets**,
  version **1.0 / code 9**, ABI **arm64-v8a**, minSdk **32**, targetSdk **36**.
  Le résumé Unity affiche **1 404 Mo** ; la taille retenue ici est celle du fichier APK
  mesuré directement, distincte de cette valeur de résumé.
- SHA-256 : `B2D64BC4999AC739BC6C972A3DAFCE3926F50ABD306C9F92E2C63D3227E6E48F`.
- Installation `adb install -r` : **Success** ; version 9 confirmée sur Quest 3.
  Lancement à froid : **Status: ok / COLD**, activité UnityPlayerGameActivity.
- Capture du 3 novembre à **19 h 57** : **au moins 30 s** (15 intervalles de 2 s),
  fenêtre hôte **32,91 s** entre création du logger et récupération du tampon.
  Journaux live et tampon dépouillés ensemble : **0 exception**, **0 E/F Unity**,
  **0 erreur AndroidRuntime**, **0 XR_ERROR** ; PID **21920** encore présent.
- La première capture avait atteint OpenXR puis la veille avant le démarrage de scène.
  La seconde utilise des réveils `KEYCODE_WAKEUP` périodiques, sans changement permanent
  des réglages du casque. Ce protocole ne valide pas une session en casque porté.
- Initialisation réelle confirmée dans le Player IL2CPP : équipement XR raccordé
  (astronaute, poignet gauche, trois outils), affichage de **35 cellules occupées**
  par les huit blocs, OpenXR **FOCUSED**. Auto-test Digger : recul du collider
  de **0,649 m**, excavation **54 ms** (une opération, pas une mesure de FPS).

Preuve structurée : [recette Quest](../Preuves/INTEGRATION_01/quest-validation.json).
Journaux locaux : `Logs/build-quest3.log`,
`Logs/quest-integration-20261103-195714-live.log` et `...-unity.log`.
Les APK, journaux bruts et bibliothèques natives restent hors Git.

Avertissements constatés : Profiler sans éditeur connecté (timeout, puis tampon de
128 Mio saturé), `XR_META_performance_metrics: Attempted to stop before session ready`
et `Orientation Listener cannot detect orientation`. Aucun arrêt du processus observé.
L'avertissement éditeur XRI concerne le dossier optionnel `Assets/Samples` absent.

Les sources et réglages locaux de la livraison astronaute restent hors de ce commit :
ils appartiennent au travail producteur encore en cours. Unity a normalisé le verrou
de packages correspondant aux modules animation/director/imgui de cette livraison ;
manifest et verrou sont conservés localement, sans inclure ces changements inconnus.

## Recette restante par Pierre

- Portée des emplacements d'outils assis/debout, précision de la prise et fermeture des doigts.
- Taille/alignement de l'avatar, suivi des épaules et mains, absence d'occlusion locale.
- Orientation et confort du cyberdeck au poignet gauche, puis lisibilité des interfaces finales.
- Prise, gâchette de foreuse à proximité du volume voxel, frein gauche avec un outil tenu.
- Tracking perdu/récupéré et recentrage sur appareil porté ; aucune activation involontaire.
- Rendu des blocs, colliders pente/coin et mesures CPU/GPU/FPS/mémoire.
- Réseau et synchronisation de l'équipement restent à implémenter et à tester séparément.
