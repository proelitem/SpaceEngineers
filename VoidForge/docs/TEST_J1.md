# Jalon J1 : recette technique et tests en casque

Recette locale des **2 et 3 novembre 2026** (America/Toronto), avec **Unity 6000.4.0f1**,
version explicitement retenue par Pierre. Le prototype initial du 30 octobre n'avait
jamais été compilé. Les résultats ci-dessous remplacent cette revue sur sources.

## Résultats constatés

Retour de Pierre du **3 novembre 2026** après essai J1 en casque : tous les contrôles
ont été testés et fonctionnent ; confort, locomotion et performances jugés bons.
Cette validation utilisateur ne fournit pas de mesures FPS/draw calls ni la version
de l'APK testé. Le test réseau à plusieurs appareils reste à réaliser.
Retour complémentaire du même jour : Pierre confirme explicitement le creusage fonctionnel
et des performances élevées en casque sur le prototype de creusage.

- Première ouverture Unity terminée avec code 0 (`Logs/resolve-j1-Dcache.log`).
- Packages résolus et verrouillés dans `Packages/packages-lock.json` ; URP aligné
  explicitement sur 17.4.0. OpenXR 1.15.1 et XRI 3.1.2 sont disponibles et installés.
- `ConfigureBatch` et `MainSceneBuilder.BuildBatch` terminés avec code 0.
- SDK, NDK 27.2.12479018, OpenJDK et adb utilisés depuis le module Android Unity Hub.
- Quest 3 USB reconnu et autorisé par adb.
- Build final terminé avec code 0, installation `Success` et lancement à froid `Status: ok`.
- Capture Unity d'au moins 30 s : zéro exception, zéro message E/Unity, zéro XR_ERROR.
  Processus toujours présent à la fin ; aucune erreur AndroidRuntime pour ce processus.
- Périmètre : projet MIT `VoidForge/` uniquement ; aucun code ni asset Keen réutilisé ou compilé.

## Grille de recette

| À tester | Comment | Résultat attendu | Résultat |
|----------|---------|------------------|----------|
| Compilation | Unity 6000.4.0f1, batch Android | Zéro erreur finale C# | OK : assemblies éditeur et runtime compilés ; compilation C# du Player réussie |
| Versions de packages | Package Manager et verrou | Toutes résolues | OK : packages-lock.json généré ; URP 17.4.0, OpenXR 1.15.1, Netcode 2.4.0, XRI 3.1.2 |
| Configuration | ConfigureBatch et assets XR enregistrés | ARM64, IL2CPP, Vulkan, OpenXR Meta Quest + Oculus Touch | OK : code 0 ; réglages enregistrés, init XR au démarrage, single-pass instancié et minSdk 32 vérifiés |
| Scène | MainSceneBuilder.BuildBatch puis test Play | Rig, étoiles, astéroïdes gris, HUD bleu | Génération OK ; Play : caméra/rig, 2500 étoiles orientées vers le centre, 40 astéroïdes avec colliders et HUD actualisé vérifiés. Recette Play globale KO : exception interne Search de l'éditeur (détails ci-dessous). Aspect visuel à tester par Pierre |
| APK | BuildBatch -vfDev, puis adb install -r | APK installable et application lancée | OK : build code 0, APK final 86 986 405 octets, ARM64, version 1.0/code 6 ; installation Success, lancement à froid Status: ok |
| Logcat | Lancement puis adb logcat -s Unity pendant 30 s | Aucune exception Unity au démarrage | OK : capture 30,19 s le 3 novembre à 07 h 22, PID 27496 encore présent, zéro exception/E Unity/XR_ERROR et zéro erreur AndroidRuntime. OpenXR atteint FOCUSED puis veille ; session continue non vérifiée |
| Ensemble des contrôles J1 | Essai en casque | Tous les contrôles fonctionnent | OK : tous testés et fonctionnels selon Pierre, le 3 novembre 2026 |
| Locomotion | Stick gauche | Direction de la main gauche, inertie, plafond 12 m/s | OK : contrôles et locomotion validés par Pierre ; plafond non mesuré |
| Freinage | Grip | Arrêt progressif | OK : inclus dans les contrôles J1 testés et fonctionnels selon Pierre |
| Rotation et confort | Stick droit gauche/droite | Cran de 30°, sans nausée | OK : contrôles et confort validés par Pierre |
| Collisions | Foncer dans un astéroïde | Rebond mou, pas de traversée | À tester par Pierre |
| Réseau, 2 appareils | Menu 1,5 s sur l'hôte ; IP de l'hôte puis B 1,5 s sur le client | Tête et mains de l'autre joueur synchronisées | À tester par Pierre |
| Stéréo et performance | Casque, build développement et Profiler | Image stéréo, 72 images/s stables, moins de 100 draw calls | Performance jugée bonne par Pierre ; stéréo détaillée et seuils FPS/draw calls non consignés |

## Corrections et compatibilité

1. `ProjectVersion.txt` aligné sur `6000.4.0f1` (`8cf496087c8f`).
2. `MetaQuestFeature` référencé dans son vrai namespace
   `UnityEngine.XR.OpenXR.Features.MetaQuestSupport` ; configurateur compilé et exécuté.
3. URP 17.3.0 initialement demandé remplacé par 17.4.0, version intégrée de l'éditeur.
   Les mises à niveau transitives imposées par Unity sont conservées dans le verrou.
4. Les deux erreurs GUID initiales de Shader Graph ont été corrigées automatiquement
   par l'API Updater Unity dans le cache (8 fichiers migrés), puis recompilées avec succès.
   Aucun correctif manuel ni package tiers embarqué pour ce point.
5. Input System seul (`activeInputHandler = 1`) remplace Both, signalé comme non supporté
   par Unity sur Android ; configuration et scène régénérées après ce changement.
6. Cache Gradle isolé dans `Library/GradleUserHome` et concurrence limitée à quatre workers.
   Une tentative initiale a échoué sur `java.util.concurrent.TimeoutException` ; la reprise
   avec cette limite et le cache conservé a réussi. Le rôle respectif du cache et du nombre
   de workers dans cette réussite n'a pas été isolé.
7. Ordre des sommets des étoiles inversé : leurs faces visibles regardent maintenant la
   caméra au centre, conformément au culling du matériau. Aucun changement du gameplay.
8. Compilation native limitée à `BEE_BUILD_THREADS=4`, dans les scripts et le pipeline.
   La reprise à vingt compilateurs a échoué sur trois objets avec `LLVM ERROR: out of memory`
   et `Allocation failed` (code secondaire `0xC000001D`). Les objets réussis sont conservés.

Les trois risques de la revue initiale sont levés pour cet environnement :
`CreateDefaultManagerSettingsForBuildTarget` et `OpenXRSettings.GetSettingsForBuildTargetGroup`
sont publics et ont été exécutés ; `NetworkConfig.Prefabs.Add` et
`UniversalRenderPipelineAsset.Create(rendererData)` ont fonctionné.

La validation OpenXR du build émet trois recommandations facultatives : nouveau
PoseControl, nouveau StickControl et latence Prioritize Input Polling. Elles n'ont pas
bloqué la construction ; leur traitement éventuel reste distinct de la recette en casque.

### Exception dans l'éditeur, distincte du test sur Quest

`J1SmokeTest.RunBatch` termine avec code 1 :
`ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection.`
La pile pointe vers `UnityEditor.Search.SearchInit.IndexationOnStartup` puis
`UnityEditor.EditorApplication.Internal_CallDelayFunctions`, sans frame runtime VoidForge.
Les vérifications de scène ont passé avant le contrôle final des erreurs.
Un processus neuf, la désactivation temporaire du reload de domaine et un délai de
démarrage n'ont pas supprimé l'exception. Les recherches sur ce problème d'éditeur
ont été arrêtées ; aucune modification du moteur Unity ni exclusion d'erreur dans le test.
Journal : `Logs/smoke-j1-startup.log`. L'initialisation XR Standalone est restaurée.
Les réglages livrés réactivent le rechargement normal en Play (options 0/0) ; Android
reste configuré pour initialiser OpenXR au démarrage.

### Avertissements du lancement sur Quest

Le build développement essaie de joindre le Profiler, sans éditeur connecté :
`Direct connection to host time out`, puis arrêt du Profiler lorsque son tampon atteint
128 Mio. Le journal contient aussi `XR_META_performance_metrics: Attempted to stop before
session ready`, `Orientation Listener cannot detect orientation` et un événement Android
`APP_CMD_LOW_MEMORY` au passage en arrière-plan. Aucune exception ni arrêt du processus
observé pendant la capture. Aucun budget de mémoire ou de fréquence n'est validé par ce test.

## Limites connues du J1

- Mains et manettes représentées par des primitives ; aucun son.
- Pas de vignette de confort. Poussée 4 m/s² et plafond 12 m/s à régler en casque.
- Réseau local par IP fixe ; ni découverte automatique ni relais Internet.
  Le rig local n'est pas encore un objet réseau (M1).
- Fréquence de rendu laissée au runtime ; 72/90/120 Hz non mesurés ici.
- Les .meta, réglages XR, scène, matériaux et prefab générés sont livrés dans Git.
  APK et caches restent locaux et ignorés.

Le cache `Library/PackageCache` est une jonction vers `D:/Temp/VoidForge-PackageCache`,
explicitement autorisée par Pierre après mesure de fortes attentes disque sur G:.
Cela ne change pas les chemins des sources ni des livrables.

Preuves détaillées et journaux : [rapport Hermes J1](../Hermes_Works/Jalons/J1.md).

## Extension du 3 novembre : assets et creusage

La recette du J1 initial ci-dessus reste historique. Les modèles de `G:/UnityAssets`
et le prototype Digger sont suivis dans [le rapport de cette extension](../Hermes_Works/Jalons/J1_ASSETS.md).

| À tester | Résultat |
|----------|----------|
| Packages et compilation C# Android avec Digger | OK : AI Navigation 2.0.11, Terrain, Terrain Physics et Unity Web Request résolus ; compilation Player réussie |
| Catalogue d'astéroïdes | OK : 12 variantes, 36 meshes, 4 matériaux URP, 40 LODGroup et colliders ; textures 1024 ASTC6x6 |
| Excavation et collision dans l'éditeur | OK : volume isolé créé par Add, puis Dig ; recul du collider de 0,649 m sur données clonées |
| APK et logcat du prototype | OK : versionCode 8, 96 860 383 octets, Unity code 0 ; installation Success et lancement COLD Status: ok ; 30,38 s sans exception/E Unity/XR_ERROR ni message AndroidRuntime |
| Excavation et collision sur Quest | OK : SELFTEST à 12:34:01, recul collider 0,649 m, excavation 89 ms ; une mesure, aucun budget de performance validé |
| Creusage et performances ressenties en casque | OK : creusage fonctionnel et performances élevées confirmés par Pierre le 3 novembre 2026 ; mesures chiffrées non fournies |
| Contrôles, locomotion et confort J1 | OK : validés par Pierre dans son retour J1 ; rendu détaillé et mesures instrumentées à consigner |
| Réseau à plusieurs appareils | À tester par Pierre ; creusage voxel désactivé en réseau, réplication non implémentée |

## Première intégration : avatar, poignet, outils et blocs

Extension demandée après le J1, recette du 3 novembre 2026. La validation en casque du
J1 précédent ne valide pas automatiquement les nouveaux modèles et prises.
Procédure et preuves : [INTEGRATION.md](INTEGRATION.md),
[rapport Hermes](../Hermes_Works/Jalons/INTEGRATION_01.md).

| À tester | Résultat |
| --- | --- |
| Compilation | OK : éditeur et Player Android compilés avec Unity 6000.4.0f1 ; dépendances locales de l'astronaute et FinalIK |
| Prises et rangement | OK en scénario contrôlé : 3 ancrages calibrés, axes physiques, activation, rangement, grip rendu au freinage ; pertes/reprises de tracking et focus simulées |
| Avatar et recentrage | OK dans l'éditeur : véritable VRIK, résidus tête/main inférieurs à 1 µm dans la pose contrôlée, cibles maintenues après déplacement/rotation du référentiel |
| Cyberdeck et matériaux | OK : avant-bras gauche, normale/UV et repère HUD calibrés, dalle dégagée de l'armure dans l'aperçu contrôlé ; shaders URP de l'équipement vérifiés |
| Blocs | OK : 8 prefabs avec colliders, 3 entrées futures indisponibles, rotation de pièce multi-cellules, rejet du chevauchement et des rotations hors grille |
| APK et lancement Quest | OK : Unity code 0 ; version 9, 118 731 913 octets, ARM64, minSdk 32/target 36 ; adb install Success, lancement COLD Status: ok |
| Logcat et initialisation Player | OK : capture au moins 30 s, fenêtre hôte 32,91 s, 0 exception/E Unity/AndroidRuntime/XR_ERROR ; raccordement XR, 35 cellules et SELFTEST collider +0,649 m confirmés ; réveils ADB périodiques, port du casque non vérifié |
| Portée, doigts, prise et nouvelle commande de freinage | À tester par Pierre sur cette version : grip droit pour tenir, gauche pour freiner pendant une prise |
| Avatar et cyberdeck en casque | À tester par Pierre : proportions, confort, occlusion, tracking, recentrage, orientation et future lisibilité |
| Nouvelle intégration et performance | À tester par Pierre ; aucune mesure CPU/GPU/FPS ou mémoire sur casque porté pour cette passe |
| Réseau et fonctions des blocs/outils | À tester par Pierre pour le réseau J1 ; équipement non répliqué. Soudage, démontage, menus de construction et physique de vaisseau non implémentés |

## Recette corrective de l'équipement

Pierre signale sur l'intégration 01 : emplacement souhaité à la ceinture, fermeture des
doigts inversée, cyberdeck derrière le bras et sangles inutiles, mauvais axe du disque.
Ces défauts visuels ne sont pas couverts par le succès de compilation/démarrage précédent.
Suivi et preuves : [Intégration 02](../Hermes_Works/Jalons/INTEGRATION_02.md).

| À tester | Résultat |
| --- | --- |
| Fixation et rangement à la ceinture | OK dans Unity : 3 étuis sur Hips, poignées à moins de 42 cm du bassin, pointes vers le bas ; prise/rangement contrôlés |
| Fermeture des doigts vers la paume | OK dans Unity : 8 doigts longs se ferment vers la paume (> 10 mm à 65 %) ; 30 axes recalibrés. Pouces et contact exact aux poignées à tester par Pierre |
| Cyberdeck côté paume, sans sangles | OK après vrai VRIK et dans la posture de lecture ; décalage de dalle 0,1241188 m depuis l'axe du bras, écran visible dans l'aperçu depuis les yeux ; deux sangles omises du dérivé |
| Axe et centre du disque en rotation | OK dans Unity : quart de tour en main, plan conservé et centre stable à moins de 0,1 mm ; aperçus examinés |
| Rappel cyberdeck, confirmation grip et rangement | OK pour le contrat partagé : rappel main ouverte sans activation, premier grip puis relâchement pour ranger ; interface producteur distincte |
| HUD en Play et journal global | 10 assertions HUD réussies sur le montage final ; journal non exempt d'erreurs : exception Digger à la sortie Play, `get_internalHeightmapResolution can only be called from the main thread` ; voir le jalon correctif |
| Compilation, APK et démarrage | OK : Unity code 0, APK 10 / 124 641 831 octets, ARM64 minSdk 32/target 36 ; installation Success et lancement COLD Status: ok |
| Logcat de cette correction | OK : 33,973 s, live + tampon examinés, 0 exception/E-F Unity/AndroidRuntime/XR_ERROR ; équipement, cinq vues HUD, 35 cellules et SELFTEST collider +0,649 m confirmés. Réveils ADB périodiques, casque porté non vérifié |
| Portée assis/debout et prises sur les trois outils | À tester par Pierre après correction |
| Visibilité/lisibilité du cyberdeck et confort | À tester par Pierre après correction |
| Performance de cette version et réseau | À tester par Pierre ; aucun résultat instrumenté ajouté |

## Recette INTEGRATION_03 — 4 novembre 2026

Le retour de Pierre invalide le montage des mains et du cyberdeck de la passe précédente,
ainsi que le rangement des outils en usage réel. Les nouveaux résultats ci-dessous ont
une portée géométrique/automatisée ; la validation J1 portée du 3 novembre reste distincte.
Procédure : [SURVIE.md](SURVIE.md), preuves : [jalon 03](../Hermes_Works/Jalons/INTEGRATION_03.md).

| À tester | Résultat |
|---|---|
| Compilation et ressources | OK : génération Unity sortie 0 ; douze SDF de maillages et nouvelle scène Main |
| Mains, prises et échelle | OK géométrique : vraie résolution VRIK, huit doigts longs fermant vers la paume, huit cycles de prise/rangement par outil sans dérive ; rendu relu |
| Sélection Cyberdeck → cuisse droite | OK dans Main : seul outil sélectionné visible, aucune prise distante ; portée réelle à tester par Pierre |
| Cyberdeck collé à la manche | OK géométrique : appui après skinning à -1 mm ; deux sangles et deux pieds omis du dérivé ; rendu relu, à tester par Pierre en casque |
| Vue extérieure de visière | Repère et recentrage vérifiés ; absence de casque dans l'aperçu ; à tester par Pierre en casque |
| Scène nettoyée et atelier | OK en Play : absence de sphère et présentoir, 64 astéroïdes, quatre plateformes et quatre machines sans chevauchement |
| Plans bleus et soudage | OK en Play : fondation libre, grille adjacente, collision/occupation refusées, aucun coût à la pose, coût réel au soudage, refus si composants manquants |
| Minage, minerais, collider | OK en Play : deux excavations, vraie collecte par matériau, collider creusé de 0,624759 m |
| Sauvegarde/rechargement | OK : second Play, stocks/plans/trous restaurés sans collecte en double ; sur Quest, huit identifiants de blocs et stocks sac/soute conservés après redémarrage |
| Registre économique | OK : 55 contrôles .NET ; 35 contrôles du nouveau Main, sans exception pendant les deux chargements et leur sortie |
| GPS cadre et chevron | Liaison et directions hors champ vérifiées ; cadre, lisibilité et stéréo à tester par Pierre |
| APK et logcat 03 | OK : build Success, APK 12 / 134 893 741 octets, ARM64 minSdk 32/target 36 ; installation Success, deux lancements COLD ; captures 35,428 s et 35,363 s sans défaut Unity/AndroidRuntime/XR_ERROR, cinq marqueurs prêts aux deux lancements ; casque porté non vérifié |
| Contrôles et locomotion de cette tranche | À tester par Pierre |
| Confort et performance de cette tranche | À tester par Pierre ; aucun FPS Quest certifié par le Play éditeur |
| Réseau à plusieurs appareils | À tester par Pierre ; économie et excavations de cette tranche suspendues en session réseau |
| Équivalence numérique des veines avec SE | Non certifiée ; profil original documenté, aucun fichier Keen utilisé |

## Recette INTEGRATION_04 — 4 novembre 2026

Le retour de Pierre invalide les anciennes conclusions de contact sur les plaques et le
HUD. Cette nouvelle passe distingue les vérifications automatiques de la recette portée.
Voir [RECETTE_04.md](RECETTE_04.md) et [jalon 04](../Hermes_Works/Jalons/INTEGRATION_04.md).

| À tester | Résultat |
|---|---|
| Compilation et packages | OK : Unity 6000.4.0f1 ; module Particle System intégré ajouté ; génération sortie 0 |
| Trois machines selon références 2D | OK : GPT-Astra / Blender MCP ; meshes relus, URP et bornes Unity vérifiés ; raffinerie 1 × 2 × 1 |
| Bon asset pour chaque blueprint | OK : douze entrées, identité des meshes comparée au prefab réel, aucun repli armure |
| Sélection / désélection du même bloc | OK dans le vrai HUD de Main ; second clic annule la sélection |
| Soudage, composants et bloc opérationnel | OK : prélèvement progressif, panneau installé/requis, fin à 100 %, prefab normal seulement une fois complet |
| Broyage, disparition et récupération | OK : composants réellement installés rendus au sac, bloc/plan à zéro supprimé, doublon de récupération refusé ; soute pleine et file réservée protégées |
| Plaques des mains et compensation manettes | Géométrie et rendus ouverts/fermés vérifiés ; contact et angle réel à tester par Pierre |
| Cyberdeck et HUD collés | Semelle conformée et HUD à 0,15 mm vérifiés dans Unity ; rendu relu ; postures réelles à tester par Pierre |
| Textures stables à l’approche | OK pour l’identité et le shader des surfaces avec géométrie ; projection source/Digger commune ; perception en casque à tester par Pierre |
| Renommer un module | OK : module proche/en construction, nom sauvegardé et rechargé ; clavier natif fourni |
| Module / Systèmes / énergie | OK : pictogramme ImageGen, systèmes non structurels, ON/OFF, charge conservée, solaire orienté/occulté et combustible réel du réacteur |
| Masse et freinage | OK : masses de composants/soute/réservations, vérification physique native du rapport force/masse ; pilotage et ressenti à tester par Pierre |
| Effets et gâchette maintenue | OK : trois outils, boucles Sci-Fi Arsenal et arrêt d’émission au relâchement ; rendu et ressenti en casque à tester par Pierre |
| Registre, Main et persistance | OK : 62 contrôles .NET et 120 assertions Unity, deux chargements et aucune exception capturée |
| Migration du monde existant | OK sur copie Quest : 8 blocs d’origine, 6 plans, 58 excavations et stocks conservés ; ajout unique d’un solaire de démarrage, 9 blocs après migration |
| APK, installation et logcat 04 | OK : APK 13, 144 899 137 octets, ARM64 min 32/cible 36 ; installation Success, deux démarrages COLD, captures 35,356 s et 35,972 s sans exception/E-F Unity/AndroidRuntime/XR_ERROR ; format 2, 9 blocs/6 plans/58 excavations conservés. Casque porté non vérifié |
| Locomotion et confort | À tester par Pierre sur cette version |
| Performance en casque porté | À tester par Pierre ; aucun FPS certifié par les tests éditeur |
| Réseau à plusieurs appareils | À tester par Pierre ; cette économie solo reste suspendue en réseau |
