# Points utiles à la reprise

- Library/PackageCache est une jonction autorisée vers D:/Temp/VoidForge-PackageCache.
  Les sauvegardes dans Library/PackageCache.hdd-backup et Library/PackageCache.pending-backup
  restent locales et ignorées ; ne pas casser cette jonction avant de relancer Unity.
- L'environnement global GRADLE_USER_HOME pointe vers un autre projet. Utiliser les scripts
  de VoidForge ou fixer Library/GradleUserHome et BEE_BUILD_THREADS=4 dans le processus.
  La concurrence native non limitée a causé LLVM out of memory ; conserver les caches.
- Exception toujours ouverte dans la recette Play : UnityEditor.Search.SearchInit.IndexationOnStartup,
  ArgumentOutOfRangeException. Recherches arrêtées ; ne pas déclarer ce test réussi.
  Le build et le démarrage sur Quest sont vérifiés séparément ; voir docs/TEST_J1.md.
- Attendre la recette en casque de Pierre (locomotion, confort, rendu/performance et réseau)
  avant de démarrer J2. L'APK final et les logs détaillés restent locaux, non versionnés.
