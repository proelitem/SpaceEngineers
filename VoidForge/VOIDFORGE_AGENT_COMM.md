# Points utiles à la reprise

- `Library/PackageCache` est une jonction autorisée vers `D:/Temp/VoidForge-PackageCache`.
  Les sauvegardes `Library/PackageCache.hdd-backup` et `Library/PackageCache.pending-backup`
  restent locales ; ne pas casser la jonction ni jeter les caches pour relancer un build.
- L'environnement global `GRADLE_USER_HOME` appartient à un autre projet. Fixer
  `Library/GradleUserHome` et `BEE_BUILD_THREADS=4` dans le processus. Le pipeline le fait
  aussi pour les builds par menu ; la concurrence native illimitée a déjà causé LLVM OOM.
- La scène active utilise `Resources/VoidForge/Survival/Settings`, les symboles locaux
  `VF_LOCAL_DIGGER`/`VF_LOCAL_ECONOMY` et les dépendances de `docs/SURVIE.md`.
  `Build/stage-survival.ps1` prépare les gants dérivés ; `SurvivalWorkshop.BuildBatch`
  génère les SDF Digger et Main. Ne pas remettre le présentoir ou la sphère d'essai.
- Les sources productrices Avatar/FinalIK/HVR et leurs `.meta` restent des entrées locales
  nécessaires, avec des fichiers encore non livrés par leur producteur. Ne pas committer
  les fichiers inconnus du dossier principal (Avatar/Packages/ProjectSettings/meshy_output)
  au titre de cette passe. Les originaux d'astronaute et de cyberdeck sont préservés.
  Les ressources sous licence et `Assets/csc.rsp` restent hors Git ; lire G:/UnityAssets
  uniquement pour importer les packs autorisés, jamais les fichiers Keen.
- Les ports publics `EconomyRuntime.RefreshWorldInstances`/`DetachInterface` et
  `CyberdeckRuntime.ReleaseInterface` assurent le passage à `SurvivalSession` et
  `SurvivalDeckAdapter`. Ne pas réactiver en parallèle les deux boucles de commandes.
  La sauvegarde commune est `survival-world-v1.json` ; le fichier économique précédent
  est conservé. Les recettes utilisent un chemin neuf sous Library/SurvivalValidation.
- La nouvelle opération Digger prépare les caches Terrain sur le thread principal avant
  les workers. Le repli historique MiningPrototype conserve son exception de sortie Play
  documentée dans TEST_J1 ; ne pas confondre sa recette avec celle de la scène Survie.
- Blender headless : isoler BLENDER_USER_RESOURCES et BLENDER_USER_EXTENSIONS ainsi que
  CONFIG/SCRIPTS/TEMP, selon Build/correct-astronaut-hands.ps1. Factory-startup seul
  n'isole pas le profil utilisateur ; voir l'incident documenté dans docs/OUTILS_ASSETS.md.
- DRILL-R reste dans le catalogue d'intégration local. Pilotage/minage de vaisseau,
  équipements réseau, réseaux électriques séparés et équivalence numérique exacte des
  veines avec Space Engineers restent hors de la tranche livrée. Les grilles sont statiques.
  Pour la recette portée de cette nouvelle version, suivre Hermes_Works/Jalons/INTEGRATION_03.md.

- Recette 04 : l APK 13 est installe ; generation et procedure canonique dans docs/RECETTE_04.md. Les dependances locales ignorees Arsenal/Reactor/HVR/FinalIK restent necessaires. Le worktree prive assets-mining-20261103 contient un ancien resultat core-validation.json modifie par nos tests mais volontairement exclu de la livraison ; utiliser Hermes_Works/Preuves/INTEGRATION_04/core-validation.json. Ne pas restaurer ni committer les suppressions inconnues de preuves INTEGRATION_03 du dossier principal.
