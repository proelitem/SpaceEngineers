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
- Recette J1 : contrôles, locomotion, confort et performances validés par Pierre le 3 novembre.
  Réseau à deux appareils, collisions et mesures chiffrées restent à consigner dans docs/TEST_J1.md.
  Pierre a ensuite confirmé explicitement le creusage fonctionnel et des performances élevées.
  L'APK final et les logs détaillés restent locaux, non versionnés ; aucun J2 engagé ici.

- Extension assets et creusage : APK code 8 et test collider validés sur Quest (+0,649 m).
  Voir Hermes_Works/Jalons/J1_ASSETS.md ; Unity est fermé, logs et APK restent locaux.
  G:/UnityAssets est lu uniquement. Digger/ThirdParty/DiggerData et csc.rsp locaux ignorés.
  Astéroïdes décoratifs avec 3 LOD ; volume Digger isolé, pas destruction des FBX.
- Suite minage : exigences dans docs/MINAGE.md. Les formes voxel issues des assets,
  veines de minerais et outils à main/vaisseau ne sont pas encore implémentés ; les valeurs
  exactes du profil Space Engineers doivent être sourcées publiquement avant codage.
- Les ressources tierces et Assets/csc.rsp ne sont pas dans Git : lancer
  Build/import-local-assets.ps1 avant Unity pour activer VF_LOCAL_DIGGER sur une autre machine.
- Outils : DRILL-R copie sous Assets/ThirdParty/ToolAssets/Ship/DRILL-R ; reproduction par
  Build/import-drill-r.ps1. Candidats a main et limites dans docs/OUTILS_ASSETS.md ; aucune
  vraie meuleuse portative identifiee. Pierre exige des outils industriels futuristes : pioche
  et scie ecartees ; le script ne prepare plus que UT-04-WELDER (tete sans poignee).
  MiningTool_01/MiningEmitter_02 et UT-02 sont des equipements de vaisseau, pas des outils a main
  prets a jouer. Minage portatif et vraie meuleuse restent a trouver. Aucun outil equipe au joueur.
- Blender headless : isoler BLENDER_USER_RESOURCES et BLENDER_USER_EXTENSIONS en plus de
  CONFIG/SCRIPTS (Library/BlenderToolReview). Le profil partiel a tente un nettoyage utilisateur ;
  impact non etabli, details dans docs/OUTILS_ASSETS.md. Ne pas relancer avec factory-startup seul.
- Travaux graphiques concurrents dans docs/concepts/ et meshy_output/ : ne pas les inclure
  dans la livraison assets. Worktree de cette livraison : .worktrees/assets-mining-20261103,
  branche codex/voidforge-assets-mining-20261103 ; aucun cache Unity partagé avec lui.
