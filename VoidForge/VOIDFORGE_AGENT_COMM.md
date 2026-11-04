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
  et scie ecartees. Le script prepare UT-04-WELDER (tete sans poignee) et la soudeuse portative
  Toon Soldiers / weapon_welder (269 triangles, style cartoon, non choisie definitivement).
  MiningTool_01/MiningEmitter_02 et UT-02 sont des equipements de vaisseau.
  Suite autorisee : trois outils originaux produits avec GPT-Astra/Blender MCP ; voir
  docs/OUTILS_MAIN.md et Hermes_Works/Jalons/J1_OUTILS_MAIN.md. Sources Art/HandTools,
  exports Assets/VoidForge/Art/HandTools ; relectures Blender et import Unity isole reussis.
  Premiere integration URP/prises/colliders realisee, avec foreuse reliee au prototype Digger :
  docs/INTEGRATION.md et Hermes_Works/Jalons/INTEGRATION_01.md. Rendu et prises en casque
  restent a valider ; soudage/demontage et equipement reseau ne sont pas implementes.
- Blender headless : isoler BLENDER_USER_RESOURCES et BLENDER_USER_EXTENSIONS en plus de
  CONFIG/SCRIPTS (Library/BlenderToolReview). Le profil partiel a tente un nettoyage utilisateur ;
  impact non etabli, details dans docs/OUTILS_ASSETS.md. Ne pas relancer avec factory-startup seul.
- Scene native VF_HandTools_20261103 sauvegardee dans Art/HandTools/Source/VoidForge_HandTools.blend.
  Interruption native recuperee via scripts ; compilation des assemblages reference les materiaux
  originaux, pas les ID temporaires du depsgraph. Derniere capture MCP WinError 10054 mais
  get_scene_info a confirme les modeles presents ; apercus studio sauvegardes inspectes.
- Travaux graphiques concurrents dans docs/concepts/ et meshy_output/ : ne pas les inclure
  dans la livraison assets. Worktree de cette livraison : .worktrees/assets-mining-20261103,
  branche codex/voidforge-assets-mining-20261103 ; aucun cache Unity partagé avec lui.

- Integration 01 : APK code 9 installe ; liaison XR, 3 outils, 8 blocs/35 cellules et
  SELFTEST Digger verifies sur Quest, sans exception au demarrage. Voir le jalon INTEGRATION_01.
  Le corps/poignet local charge Resources/VoidForge/Integration/Equipment ; garder les
  dependances locales de l'astronaute/FinalIK et leurs .meta. Les fichiers producteurs
  encore inconnus (avatar, manifest/verrou, Art/UI, meshy_output, ROADMAP) restent hors
  du commit de cette passe. La recompilation initiale a pris 4056 s ; conserver Library/Bee.
  CyberdeckSurface expose la dalle/repere HUD ; Refinery/Assembler/Cargo restent indisponibles.
  Unity principal ferme. Prises/confort/rendu/performance de cette version a tester par Pierre.
