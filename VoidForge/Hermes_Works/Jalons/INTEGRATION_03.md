# INTEGRATION_03 — scène de départ, minage et construction

Extension autorisée par Pierre le **4 novembre 2026** après son retour sur les mains,
le cyberdeck, les outils et la scène. Projet MIT `VoidForge/` uniquement, Unity
`6000.4.0f1`, livraison sur `claude/relaxed-darwin-9f11j0`.

Contrat et procédure : [SURVIE.md](../../docs/SURVIE.md).
Architecture : [ARCHITECTURE.md](../../docs/ARCHITECTURE.md).
Les décisions de géométrie, d'entrées et de données sont techniques ; aucune dépense
ou nouvelle décision propriétaire n'est engagée par cette passe.

## État des preuves

| Contrôle | Résultat observé |
|---|---|
| Compilation et génération | Génération Unity sortie 0, `Logs/survival-staging-6.log` |
| Mains et FinalIK | Pouces latéraux au repos, huit doigts longs fermant vers la paume ; résidus main 0,000000485 m et tête 0,000000267 m |
| Outils | Trois prises alignées ; taille constante après huit cycles prise/rangement par outil ; un outil sélectionné à la cuisse droite |
| Cyberdeck | Support recalé après skinning/VRIK ; jeu calculé -0,001 m ; sangles et pieds de selle omis du dérivé |
| Vue | Repère extérieur de visière et recentrage géométriques vérifiés ; caméra suivie par XR |
| Formes | Douze maillages LOD0 voxelisés par Digger, 68 921 échantillons par variante |
| Scène | Nouvelle scène Main générée ; 64 astéroïdes enregistrés, ancienne sphère et présentoir absents du Play |
| Construction et sauvegarde | PASS : plan libre sans consommation, soudage, refus collision/occupation/non-adjacence, manque de composants, démontage unique et second chargement |
| Minage et collecte de minerais | PASS : deux vraies excavations, pierre et minerais crédités, collider +0,624759 m ; trous et stocks restaurés sans double crédit |
| Ciblage GPS | Raccordement Main et directions hors champ vérifiés ; lisibilité et cadre en stéréo à retester par Pierre |
| APK et USB | PASS : APK 12, 134 893 741 octets, ARM64 minSdk 32/target 36 ; installation Success et deux lancements COLD Status: ok sur Quest `2G0YC5ZF7H060B` |
| Logcat Quest | PASS : 35,428 s puis 35,363 s ; aucun défaut Unity/AndroidRuntime/XR_ERROR capturé ; équipement, HUD, 64 astéroïdes, atelier et collider voxel prêts aux deux lancements |
| Persistance Quest | PASS : sauvegarde lisible ; huit identifiants de blocs et stocks sac/soute identiques après redémarrage ; profil de minerais attendu |
| Ergonomie et performances en casque | À retester par Pierre pour cette tranche |
| Réseau | À tester par Pierre ; économie/minage de cette tranche suspendus pendant une session réseau |

Le Play compte **35 contrôles réussis**, avec zéro exception pendant les deux chargements
et leur sortie (`Logs/survival-play-2.log`). Les rendus des deux faces de la prise,
du holster, du poignet et de la vue extérieure ont été relus dans l'éditeur.
Le registre économique compte aussi **55 contrôles réussis** dans cette passe.
Les preuves retenues sont dans `Hermes_Works/Preuves/INTEGRATION_03/` ; les journaux bruts
et l'APK restent locaux. La recette J1 précédemment validée par Pierre garde sa portée
initiale et ne vaut pas validation de ces nouveaux éléments.

Preuves machine : [build](../Preuves/INTEGRATION_03/build-validation.json),
[Quest](../Preuves/INTEGRATION_03/quest-validation.json),
[Play](../Preuves/INTEGRATION_03/play-validation.json) et
[registre](../Preuves/INTEGRATION_03/economy-validation.json).
APK local : `Builds/VoidForge-Quest3-integration03-dev.apk`.
SHA-256 : `8C76AE7857F71916BAE1000EBB94F731C81682244D05636BEC60C53CFC1D7103`.
Le build Unity signale Success, avec 3 146 s rapportées. Son total de 1 486 Mo inclut
les sorties auxiliaires ; le fichier APK réel mesure 134 893 741 octets.

Aperçus relus : [prise côté paume](../Preuves/INTEGRATION_03/right-grip-inside.png),
[prise côté dos](../Preuves/INTEGRATION_03/right-grip.png),
[cuisse droite](../Preuves/INTEGRATION_03/right-thigh.png),
[contact du boîtier](../Preuves/INTEGRATION_03/wrist-mount-side.png),
[poignet depuis les yeux](../Preuves/INTEGRATION_03/wrist-from-eyes.png) et
[vue extérieure de visière](../Preuves/INTEGRATION_03/visor-view.png).

## Corrections de recette

- Remise en place de la peau HVR et des phalanges dans une copie locale, protections
  dorsales conservées. Le premier critère sur le pouce était erroné et a été corrigé.
- Restauration de l'échelle au rangement et suppression de l'héritage de l'échelle
  des os importés pour les holsters. La sélection du Cyberdeck prépare la cuisse.
- Suppression des deux pieds de selle qui créaient 21 mm de vide sous le boîtier.
  Échantillonnage des faces skinnées de la manche pour l'appui après FinalIK.
- Suppression du présentoir de blocs et de la sphère d'essai du parcours actif.
  Grilles de plans bleus et atelier sur quatre blocs pour la nouvelle boucle.
- Le premier rechargement a détecté un chevauchement de la soute avec l'assembleur
  de deux cellules. La soute est placée au-dessus de la batterie ; les empreintes
  réelles des machines sont vérifiées avant la première sauvegarde.
- Recette USB : lecture du journal ouverte en partage pour éviter le verrou Windows ;
  lecture de la sauvegarde externe par le shell ADB, car `run-as` reçoit Permission
  denied sur le montage FUSE du Quest. Tampon main agrandi temporairement de 256 Kio
  à 4 Mio pour conserver les diagnostics de démarrage, puis rétabli à 256 Kio.
  Les deux premières tentatives de recette restent dans Logs ; la preuve finale passe.
- Import non bloquant : timeout IPC UnityShaderCompiler 0x80000008 sur le shader
  de test URP `TraceRays.urtshader`, suivi de la poursuite du build jusqu'à Success.

## Limites à conserver visibles

Le profil de veines est original et reproductible ; son équivalence numérique exacte
avec Space Engineers reste non certifiée. Aucun fichier Keen n'est utilisé. Les formes
SDF, Digger, FinalIK et les mains HVR nécessitent les dépendances locales sous licence.
La tranche est solo, avec grilles statiques et registre économique global au prototype.
