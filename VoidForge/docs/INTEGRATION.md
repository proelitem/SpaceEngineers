# Première intégration des ressources — 3 novembre 2026

Extension demandée par Pierre après la recette J1 : astronaute FinalIK, cyberdeck au
poignet, outils à main et premiers blocs. Cette passe prépare une scène locale jouable.
La recette J1 précédente ne vaut pas validation en casque de ces nouveaux éléments.

## Contrats entre livraisons

| Ressource | Entrée locale | Raccordement |
| --- | --- | --- |
| Astronaute | `Assets/VoidForge/Prefabs/Avatars/VoidForgeAstronaut.prefab` | `LocalAstronaut`, trois cibles XR, véritable solveur VRIK |
| Cyberdeck v2 | `meshy_output/voidforge-kit-20261103-v2/exports/cyberdeck_clean_blank.glb` | Avant-bras gauche, écran séparé et `CyberdeckSurface.hudAnchor` |
| Outils originaux | `Assets/VoidForge/Art/HandTools/` | Prise primaire, pointe et pièce mobile nommées |
| Armures et gyro v2 | `meshy_output/voidforge-kit-20261103-v2/exports/` | Préfabriqués sur grille 1 m |
| Cockpit, propulseur, batterie v1 | `meshy_output/voidforge-kit-20261103/fitted/` | Enveloppes et pivots normalisés ; cockpit extérieur uniquement |
| DRILL-R | `Assets/ThirdParty/ToolAssets/Ship/DRILL-R/Drill-R.prefab` | Trois LOD conservés, enveloppe de réservation 2 × 2 × 4 cellules |
| Raffinerie, assembleur, soute | Ressources en production | Identifiants `Refinery`, `Assembler`, `Cargo`, indisponibles tant que le prefab manque |
| Interface cyberdeck | Ressources en production | Texture/RenderTexture via `CyberdeckSurface.SetDisplay`, sans image de maquette imposée |

Les scripts et ressources de l'astronaute sont une dépendance d'une autre livraison
encore en cours. Ne pas committer ses fichiers inconnus au titre de cette intégration.
Les copies hybrides HVR, FinalIK et les maillages dérivés restent locaux sous
`Assets/ThirdParty/`, hors licence MIT du code VoidForge. Les adaptateurs utilisent
la réflexion uniquement lors du raccordement de l'avatar, sans appel réfléchi par image.
Sans cette dépendance locale, le bootstrap conserve le rig J1.

Le rig J1 utilise ses propres actions OpenXR et ne comporte pas encore d'interacteurs XRI.
Cette passe réutilise ces actions pour une prise de proximité bornée, avec seuils de
prise/relâchement et rangement automatique. Elle n'importe pas un second contrôleur de
joueur HVR. Une prise secondaire ou une manipulation physique plus générale demandera
un raccordement supplémentaire ; les ancrages livrés restent disponibles.

## Reproduction locale

Utiliser Unity 6000.4.0f1 et son module Android. Préparer l'astronaute avec les scripts
de sa livraison (`docs/ASTRONAUTE.md`) et DRILL-R avec `Build/import-drill-r.ps1`.
Puis, depuis le projet :

```powershell
# Facultatif : reconvertir si les GLB de production locaux sont disponibles.
./Build/stage-integration-kit.ps1
& 'D:/Unity/6000.4.0f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath . -buildTarget Android -executeMethod VoidForge.Editor.IntegrationWorkshop.BuildBatch -logFile Logs/integration-build.log
./Build/build-quest3.ps1 -UnityPath 'D:/Unity/6000.4.0f1/Editor/Unity.exe' -Dev -IntegrateAssets -Output 'Builds/VoidForge-Quest3-integration-dev.apk'
```

Les FBX du kit sont déjà versionnés ; leur reconversion n'est pas nécessaire au build.
`-IntegrateAssets` régénère et vérifie les prefabs avant de construire l'APK dans le
même processus Unity. Le build J1 habituel reste disponible sans ce commutateur.

Le convertisseur lit les huit GLB livrés et exporte des FBX/PNG dans un répertoire
distinct, `Assets/VoidForge/Art/Kit/`, versionné avec ses métadonnées et sa provenance.
Il travaille avec un Blender de fond et un profil entièrement isolé.
Il ne modifie ni les GLB, ni les ateliers Blender ouverts, ni la bibliothèque d'assets.
Il ne lance aucune génération payante. Les géométries sont réunies en conservant leurs
matériaux ; l'écran et son repère restent séparés.

`IntegrationWorkshop` génère `Assets/Resources/VoidForge/Integration/` : équipement,
catalogue et huit prefabs de blocs. `GameBootstrap` les charge au démarrage de Main.
Les quatre matériaux des outils sont convertis en URP Lit à partir des facteurs PBR
de `Art/HandTools/modeler3d_report.json`. Le kit récupère facteurs et albédo disponibles.
Les cartes PBR supplémentaires du kit v1 restent à qualifier.

## Comportement de cette passe

- **Avatar local** : tête et deux poignets pilotent VRIK. Le corps suit la position et
  l'orientation horizontale de la tête, sans imposer de mouvement à la caméra. Un masque
  de triangles dérivé du skinning retire la tête/visière locale afin d'éviter l'occlusion
  de la caméra ; le modèle source reste intact. La taille reste celle du modèle livré.
  Le corps est masqué si la tête ou l'une des deux manettes perd son suivi.
- **Prise** : main droite à moins de 18 cm d'une poignée, puis grip supérieur à 0,7.
  Un repère calibré reprend la position de l'ancrage primaire ; son axe est déduit du
  pivot de la tête et de la pointe, car les axes des empties FBX diffèrent du repère Unity.
  Ce repère est aligné sur la pose de prise manette. Les doigts pointent vers l'avant,
  le dos de la main vers l'extérieur, avec un décalage initial du poignet de 25 mm vers
  l'extérieur, 15 mm vers le bas et 70 mm en arrière de la prise.
  Relâcher sous 0,25 range l'outil à son emplacement. Pas de lancer physique en apesanteur.
  Les outils rangés suivent la tête et son orientation horizontale pour rester accessibles.
- **Entrées** : grip droit consommé uniquement pendant une prise. Le grip gauche permet
  toujours de freiner ; sans outil, les deux grips freinent comme au J1. Perte de suivi
  de la tête/main droite ou perte de focus : arrêt et rangement ; il faut relâcher avant
  de reprendre. La recette vise deux manettes suivies, sans mode à une seule manette.
- **Doigts** : pose initiale de fermeture adaptée à une poignée industrielle et index
  animé par la gâchette. Les repères secondaires sont conservés, sans prise à deux mains
  dans cette passe. Le placement exact des doigts reste à régler en casque.
- **Foreuse** : la gâchette droite commande le prototype Digger depuis la pointe réelle,
  à 1 m maximum. Les deux autres outils ne creusent pas. Sans équipement intégré, le
  contrôle de creusage J1 à 8 m reste disponible. Le prototype demeure solo et sans
  persistance ; la forme voxel issue des FBX et les veines restent des travaux distincts.
- **Meuleuse et soudeuse** : prise et signal d'activation initial ; aucune récupération
  de composants, réparation ou recette simulée. La foreuse et le disque ont une rotation ;
  le soudage reste indiqué comme indisponible dans les consignes d'essai.
- **Blocs** : assemblage de démonstration chargé près de la zone voxel, à droite ; DRILL-R
  est présenté à gauche. La grille refuse chevauchements, rotations hors multiples de
  90 degrés et ressources absentes. Les armures ont des colliders convexes de leur forme ;
  les blocs techniques réservent leur enveloppe avec un BoxCollider.

La grille est un socle local, sans menu de construction, sauvegarde, autorité réseau,
masse de vaisseau ou fonctionnement des blocs techniques. Le cockpit n'est pas pilotable.
L'avatar et l'équipement ne remplacent pas encore l'avatar réseau J1 tête/mains.

Le cyberdeck est fixé sur le dessus de l'avant-bras gauche près du poignet. Dalle active
216 × 92,571 mm, zone sûre 200 × 85,714 mm, ratio 21:9. La pose et la lisibilité doivent
être vérifiées avec les futures interfaces en casque ; cette livraison laisse la dalle vide.
Le montage utilise la normale réelle du maillage d'écran et son axe UV horizontal,
sans supposer que le root du FBX possède déjà les axes d'une interface Unity.
La distance à l'axe de l'avant-bras est mesurée sur le maillage skinné dans l'emprise
du boîtier, puis augmentée de 40 mm pour le support et la déformation de la manche.
Le repère HUD est lui aussi calibré : avant = normale sortante, droite = U croissant.

## Vérification

`IntegrationWorkshop.BuildBatch` appelle le contrôle `IntegrationValidation.Run` :
alignement des trois poignées et pointes, activation/rangement, transitions simulées de
perte/reprise du tracking et de focus, partage du grip avec le freinage, politique de colliders,
matériaux URP, support au poignet, véritable résolution VRIK et cibles après recentrage,
catalogue et occupation d'une pièce de plusieurs cellules tournée de 90 degrés.
Rapport local : `Logs/integration-validation.json`.

Les résultats effectivement observés, l'APK et la recette USB sont consignés dans
`Hermes_Works/Jalons/INTEGRATION_01.md`. Les limites casque restent explicitement ouvertes.
