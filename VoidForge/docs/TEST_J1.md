# Jalon J1 : recette technique et tests en casque

Recette locale des **2 et 3 novembre 2026** (America/Toronto), avec **Unity 6000.4.0f1**,
version explicitement retenue par Pierre. Le prototype initial du 30 octobre n'avait
jamais été compilé. Les résultats ci-dessous remplacent cette revue sur sources.

## Résultats constatés

Retour de Pierre du **3 novembre 2026** après essai J1 en casque : tous les contrôles
ont été testés et fonctionnent ; confort, locomotion et performances jugés bons.
Cette validation utilisateur ne fournit pas de mesures FPS/draw calls ni la version
de l'APK testé. Le test réseau à plusieurs appareils reste à réaliser.

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
| Contrôle de creusage, rendu et performances en casque | À tester par Pierre |
| Locomotion et confort après ajout du prototype | À tester par Pierre |
| Réseau à plusieurs appareils | À tester par Pierre ; creusage voxel désactivé en réseau, réplication non implémentée |
