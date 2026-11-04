# Construire et installer sur Meta Quest 3

## 1. Prérequis (une fois)

1. **Unity Hub** puis **Unity 6000.4.0f1** (version retenue pour le J1) avec les modules :
   *Android Build Support*, *Android SDK & NDK Tools*, *OpenJDK*.
   Installation locale : `D:\Unity\6000.4.0f1\Editor\Unity.exe`.
   Utiliser la version de `ProjectSettings/ProjectVersion.txt` pour conserver un build reproductible.
2. **Quest 3 en mode développeur** : application Meta Horizon sur le téléphone →
   casque → Réglages → Mode développeur. Puis brancher le casque en USB-C et accepter
   « Autoriser le débogage USB » dans le casque.
3. **adb** : fourni avec le SDK Android de Unity
   (`<Unity>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools`), ou via Meta Quest Developer Hub.

## 2. Première ouverture du projet

1. Unity Hub → *Add* → dossier `VoidForge/`. Les packages de `Packages/manifest.json` se téléchargent.
   Si une version de package n'existe pas (le dépôt n'a pas pu être vérifié depuis le conteneur de l'agent),
   Package Manager l'indique : prends la version la plus proche dans *Window > Package Manager*.
2. Menu **VoidForge > 1. Configurer le projet pour Quest 3**. Unity peut demander un
   redémarrage pour activer le nouveau système d'entrée : accepte et relance l'étape 1.
3. Menu **VoidForge > 2. Construire la scène principale** : crée `Assets/Scenes/Main.unity`,
   les matériaux, le prefab d'avatar réseau.
4. Menu **VoidForge > 3. Construire l'APK Quest 3** (ou 3b pour un build développement avec profileur).
   L'APK sort dans `Builds/`.

Raccourci : **VoidForge > 0. Tout faire** enchaîne les trois étapes.

Après cette première ouverture, Unity a généré les fichiers `.meta` et `ProjectSettings/` :
**commite-les** pour que les GUID restent stables.

## 3. Build en ligne de commande

Linux / macOS :

```bash
UNITY_PATH=~/Unity/Hub/Editor/6000.4.0f1/Editor/Unity ./VoidForge/Build/build-quest3.sh
```

Windows :

```powershell
.\VoidForge\Build\build-quest3.ps1 -UnityPath "D:\Unity\6000.4.0f1\Editor\Unity.exe"
```

Ajoute `--dev` (ou `-Dev`) pour un build développement. Le journal est dans `VoidForge/Logs/build-quest3.log`.

Les scripts et le pipeline isolent `GRADLE_USER_HOME` dans `Library/GradleUserHome`.
Ils fixent aussi `BEE_BUILD_THREADS=4` pour limiter la compilation C++ native : une
tentative à vingt compilateurs a échoué avec `LLVM ERROR: out of memory`.
Le post-traitement Gradle limite les workers à 4 : la première tentative sur cette
machine avait échoué avec `java.util.concurrent.TimeoutException` pendant le traitement
des ressources Android ; une reprise avec le cache conservé et cette limite a réussi.
Le projet utilise uniquement le nouveau Input System (le mode Both est signalé comme
non supporté par Unity pour Android).

Recette de scène sans casque (sans `-quit`, le test termine lui-même Unity) :

```powershell
& "D:\Unity\6000.4.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath "G:\Projets\SpaceEngineers\VoidForge" -executeMethod VoidForge.Editor.J1SmokeTest.RunBatch -logFile "G:\Projets\SpaceEngineers\VoidForge\Logs\smoke-j1.log"
```

Ce test mesure huit secondes de Play, vérifie le rig, le maillage des étoiles orienté
vers l'intérieur, les astéroïdes et leurs colliders, ainsi que l'actualisation du HUD.
Il restaure les options Play et le démarrage XR Standalone après la mesure.

## 4. Installer et lancer

```bash
./VoidForge/Build/install-quest3.sh
```

ou à la main :

```bash
adb install -r VoidForge/Builds/VoidForge-Quest3.apk
```

Dans le casque : *Bibliothèque → Sources inconnues → VoidForge VR*.

Journal en direct pendant le test :

```bash
adb logcat -s Unity
```

## 5. Contrôles (J1)

| Action | Commande |
|--------|----------|
| Poussée avant/arrière/latérale | Stick gauche, dans la direction où pointe la main gauche |
| Poussée haut/bas | Stick droit vertical (repère de la tête) |
| Rotation par crans (30°) | Stick droit gauche/droite |
| Freiner | Grip gauche ou droit (proportionnel) |
| Héberger une partie | Bouton Menu (manette gauche) maintenu 1,5 s |
| Rejoindre l'hôte | Bouton B (manette droite) maintenu 1,5 s |
| Dans l'éditeur | H héberger, J rejoindre, K quitter |

L'adresse IP de l'hôte se règle sur le composant `NetworkLauncher` de l'objet `NetworkManager`
dans la scène (champ *Address*), avant le build. Le menu au poignet du J5 remplacera ce réglage.

## 6. Dépannage

- **« Meta Quest Support » introuvable** : vérifie que `com.unity.xr.openxr` est bien installé,
  ouvre *Project Settings > XR Plug-in Management > OpenXR* une fois, relance l'étape 1.
- **Écran noir dans le casque** : vérifie que le chargeur OpenXR est coché pour Android
  (*XR Plug-in Management*, onglet Android) et que *Initialize XR on Startup* est actif.
- **Manettes immobiles** : profil d'interaction *Oculus Touch Controller Profile* non activé.
- **Build Gradle en échec** : installe les modules Android par Unity Hub plutôt qu'un SDK externe.
- **Images/s basses** : passe en build 3b, branche le *Profiler* (`ConnectWithProfiler` est activé),
  vérifie que le mode de rendu est *Single Pass Instanced* et que le MSAA est 4x.

## Recette de la scène Survie — intégration 03

Préparer les dépendances locales et régénérer la scène selon [SURVIE.md](SURVIE.md).
Ne pas ouvrir un second éditeur sur le même projet. Depuis le dossier VoidForge :

```powershell
$env:GRADLE_USER_HOME = Join-Path $PWD 'Library/GradleUserHome'
$env:BEE_BUILD_THREADS = '4'
& 'D:/Unity/6000.4.0f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath $PWD -buildTarget Android -executeMethod VoidForge.Editor.QuestBuildPipeline.BuildBatch -vfDev -vfNoProfiler -vfOutput (Join-Path $PWD 'Builds/VoidForge-Quest3-integration03-dev.apk') -logFile (Join-Path $PWD 'Logs/survival-quest3.log')
./Build/check-survival-quest.ps1 -Serial 2G0YC5ZF7H060B -Install -Restart
```

`-vfNoProfiler` conserve le build développement et retire seulement l'autoconnexion
Profiler. Le contrôle USB utilise le SDK du module Android Unity, installe l'APK,
capture au moins 35 secondes de journaux après chacun des deux lancements et compare
le registre sauvegardé lorsqu'il est lisible. Son rapport est
`Hermes_Works/Preuves/INTEGRATION_03/quest-validation.json`.
La recette avec `-Restart` exige cette comparaison. La lecture passe directement
par le shell ADB ; le tampon main est agrandi temporairement à 4 Mio et sa taille
initiale est rétablie à la fin, y compris en cas d'échec.
Ce contrôle sans casque porté ne certifie pas l'ergonomie ni les performances VR.
