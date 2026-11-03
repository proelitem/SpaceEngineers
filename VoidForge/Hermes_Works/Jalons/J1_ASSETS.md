# Extension J1 — assets locaux et creusage

État : **recette technique réussie sur Quest 3 ; recette en casque par Pierre en attente**.
Demande de Pierre du 3 novembre 2026,
incluant explicitement un prototype de creusage jouable.

L'APK J1 initial (versionCode 6) reste dans `Builds/VoidForge-Quest3-dev.apk`.
La nouvelle version est dans `Builds/VoidForge-Quest3-assets-dev.apk` (version 1.0/code 8,
96 860 383 octets, ARM64, minSdk 32, targetSdk 36).
SHA-256 : `9EB3C17C430D0FA730C8B96AA83929617148814DAE47E847475177266E4BBD36`.

Référence canonique : [bibliothèque et parcours jouable](../../docs/UNITY_ASSETS.md).

| Vérification | Résultat |
|--------------|----------|
| Catalogue et sources | Asteroid Pack 01 1.0.0, Digger PRO 8.1, bibliothèque G:/UnityAssets identifiés |
| Licences | Ressources et code tiers locaux, exclus du dépôt MIT ; provenance documentée |
| Import URP et compilation | Import des modèles réussi ; Terrain Physics ajouté après CS1069 initial. Logs/assets-mining-import.log |
| 12 variantes / 3 LOD / 40 obstacles | OK : validateur d'import, 36 meshes, 4 matériaux URP, textures 1024 ASTC6x6 ; plafond théorique 80 000 triangles pour 40 LOD0 |
| Prototype Digger | Prefab généré ; volume isolé par Add puis Dig. Test éditeur OK : recul collider de 0,649 m sur TerrainData cloné ; prefab intact. Logs/assets-mining-prototype.log |
| Compilation Player Android | OK après ajout du module intégré Unity Web Request ; CS1069 initial dans le lecteur Android Digger corrigé sans modifier son code |
| APK / installation Quest | OK : Unity code 0, installation Success, lancement COLD Status: ok ; Logs/build-assets-mining-2.log et Logs/install-assets-mining.log |
| Creusage et collision sur Quest | OK à 12:34:01 le 3 novembre : Add puis Dig, SELFTEST OK, recul collider de 0,649 m ; excavation de 89 ms (une seule mesure) |
| Logcat | OK : 30,38 s, PID 10367 présent à la fin, zéro exception, zéro E/Unity, zéro XR_ERROR, zéro message AndroidRuntime |
| Visuel, confort, contrôle à la manette et performances soutenues | À tester par Pierre |
| Réplication réseau et persistance des voxels | Non implémentées ; prototype solo réinitialisé au lancement |

Les changements portent exclusivement sur VoidForge. Aucun fichier suivi supprimé,
aucun code ni asset Keen importé. Aucun fichier de la bibliothèque source modifié.

Correction de génération : la première passe ne pouvait pas ouvrir une scène additive
avec une scène de démarrage sans nom (`Cannot create a new scene additively with an
untitled scene unsaved`). En batch, cette scène temporaire est sauvegardée dans le cache
d'import ignoré ; en usage interactif, une scène non enregistrée doit d'abord être sauvée.
Le second lancement réutilise le catalogue d'astéroïdes au lieu de réimporter les FBX.

Le prototype utilise les opérations existantes Digger Add/Dig pour un volume isolé.
Le Terrain Unity reste un support masqué ; les FBX du pack restent décoratifs. La preuve
du collider est obtenue sur données clonées dans l'éditeur, puis dans le Player IL2CPP sur Quest.

La fermeture de cette recette éditeur a signalé trois allocations persistantes ; un
Dispose explicite du pool Digger a été ajouté au nettoyage du test, selon le même
mécanisme que son préprocesseur de build. Ce message n'est plus présent dans le build final.
La fermeture de Unity émet toutefois l'avertissement distinct
`~StackAllocator(ALLOC_TEMP_MAIN) m_LastAlloc not NULL. Did you forget to call FreeAllStackAllocations()?`.
Il ne bloque pas le build ; sa cause n'a pas été isolée. Aucune exception n'a été observée
sur l'appareil pendant la capture.

Le build utilise les outils Android du module Unity Hub, le cache Gradle du projet et
quatre workers natifs/Gradle. Le script PowerShell attend désormais explicitement la fin
de Unity.exe ; sa syntaxe a été vérifiée. Les 21 sources C# préparées pour livraison sont
identiques aux sources compilées (hors fins de ligne).

Journaux locaux : `Logs/logcat-assets-mining-unity.log`,
`Logs/logcat-assets-mining-androidruntime.log`, `Logs/device-assets-mining.json` et
`Logs/launch-assets-mining.log`. Le casque a reçu six événements de réveil pendant la
capture ; OpenXR atteint FOCUSED puis repasse en veille. Cela ne valide pas une session
continue ni le rendu perçu. Les avertissements Profiler sans éditeur connecté (tampon
128 Mio), métriques XR avant session prête et Orientation Listener restent présents.

La livraison a été préparée dans un worktree limité à VoidForge pour protéger les travaux
graphiques concurrents (`docs/concepts/` et `meshy_output/`), qui ne font pas partie de ce changement.
