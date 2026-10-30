# Construire et installer sur Meta Quest 3

## 1. Prérequis (une fois)

1. **Unity Hub** puis **Unity 6.3 LTS** (6000.3.x) avec les modules :
   *Android Build Support*, *Android SDK & NDK Tools*, *OpenJDK*.
   Si la version exacte de `ProjectSettings/ProjectVersion.txt` n'est pas installée,
   Unity Hub propose d'ouvrir le projet avec une autre version 6000.3.x : accepte.
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
UNITY_PATH=~/Unity/Hub/Editor/6000.3.0f1/Editor/Unity ./VoidForge/Build/build-quest3.sh
```

Windows :

```powershell
.\VoidForge\Build\build-quest3.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe"
```

Ajoute `--dev` (ou `-Dev`) pour un build développement. Le journal est dans `VoidForge/Logs/build-quest3.log`.

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
