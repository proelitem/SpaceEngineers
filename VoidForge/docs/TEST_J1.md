# Jalon J1 : notes de test et limites connues

Date : 30 octobre 2026. Livré par l'agent Fable 5.1 depuis un conteneur cloud **sans éditeur Unity,
sans compilateur C# et sans casque**. Tout ce qui suit sépare ce qui est vérifié de ce qui ne l'est pas.

## Vérifié (FAIT)

- Le dossier `VoidForge/` ne contient aucun fichier provenant du code source ou des assets de Keen.
- Les scripts sont relus pour la syntaxe C# et les API Unity 6 / XR / Netcode telles que connues de l'agent.
- Les packages visés : OpenXR 1.15.1 (version recommandée par Meta pour Unity 6, mai 2026), Unity 6.3 LTS.

## Non vérifié (À TESTER par Pierre)

| À tester | Comment | Résultat attendu |
|----------|---------|------------------|
| Compilation | Ouvrir le projet dans Unity 6.3 LTS | Zéro erreur dans la console. Les éventuelles erreurs d'API sont à corriger : les signatures de package peuvent avoir bougé |
| Versions de packages | Package Manager | Toutes résolues ; sinon prendre la version la plus proche |
| Configuration | VoidForge > 1 | Journal « Projet configuré pour Quest 3 », OpenXR avec Meta Quest Support et Oculus Touch cochés |
| Scène | VoidForge > 2 puis Play dans l'éditeur (avec Link) ou sans casque | Étoiles, astéroïdes gris, HUD bleu « i/s, m/s, réseau » |
| APK | VoidForge > 3, `adb install` | L'application se lance, image stéréo, 72 Hz |
| Locomotion | Stick gauche | Déplacement dans la direction de la main gauche, inertie, plafond 12 m/s |
| Freinage | Grip | Arrêt progressif |
| Rotation | Stick droit gauche/droite | Cran de 30°, sans nausée (pas de rotation continue) |
| Collisions | Foncer dans un astéroïde | Rebond mou, pas de traversée |
| Réseau, 2 appareils | Casque A : Menu 1,5 s. Casque B ou éditeur : adresse de A dans NetworkLauncher, puis B 1,5 s | Sur chaque appareil, une tête et deux mains bleues bougent avec l'autre joueur |
| Performance | Build 3b + Profiler | 72 images/s stables, moins de 100 draw calls |

Merci de noter pour chaque ligne : OK / KO, et le message d'erreur exact si KO.

## Limites connues du J1

- Pas de modèle de main ni de manette : sphères et cylindres.
- Pas de vignette de confort pendant l'accélération ; à ajouter si inconfort.
- Le freinage et la poussée sont des valeurs arbitraires (4 m/s², 12 m/s max) à régler en casque.
- Réseau : adresse IP de l'hôte fixée dans la scène avant build ; pas de découverte automatique,
  pas de relais Internet. Le rig local n'est pas encore un objet réseau (jalon M1).
- Pas de son.
- Le profil de fréquence (72 / 90 / 120 Hz) est laissé au défaut du runtime.
- `ProjectSettings/` ne contient que la version : Unity génère le reste à la première ouverture,
  et l'outil VoidForge > 1 applique les réglages importants. À commiter après la première ouverture.

## Risques identifiés sur le code (à surveiller à la compilation)

1. `Quest3ProjectConfigurator.ConfigureXr` : `CreateDefaultManagerSettingsForBuildTarget` et
   `OpenXRSettings.GetSettingsForBuildTargetGroup` sont des API d'éditeur dont la visibilité a changé
   selon les versions. En cas d'erreur, faire ces deux réglages à la main dans *Project Settings* et
   commenter le bloc.
2. `MainSceneBuilder.BuildNetwork` : `NetworkConfig.Prefabs.Add` ; si l'API diffère, laisser seulement
   `PlayerPrefab`, Netcode l'enregistre automatiquement.
3. `UniversalRenderPipelineAsset.Create(rendererData)` : si la signature a changé, créer l'asset URP par
   *Create > Rendering > URP Asset (with Universal Renderer)* et l'assigner dans *Graphics*.
