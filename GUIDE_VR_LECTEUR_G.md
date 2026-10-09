# Guide : compiler Space Engineers (code Keen 2017, avec prototype VR) sur le lecteur G:

Ce guide porte sur la branche `devin/1791514358-sync-upstream-vr`. Elle est identique au code officiel `KeenSoftwareHouse/SpaceEngineers` (commit `54f2f0f3`, dernière révision de code « Automatic Revision: 92003 », janvier 2017). La version du jeu correspondante est **01.169.002** (`Sources/SpaceEngineers.Game/SpaceEngineersGame.cs:32`).

> **Licence (EULA.txt)** : ce code ne sert qu'à faire fonctionner Space Engineers pour toi, avec ta propre copie Steam. Pas de redistribution, pas d'usage commercial, et aucune réutilisation dans MyrinoxPrime.

Légende : **[VÉRIFIÉ]** = constaté dans le code ou le dépôt. **[À TESTER]** = hypothèse non vérifiée sur une vraie machine Windows.

---

## 1. Prérequis

| Élément | Détail |
|---|---|
| Windows 10/11 64 bits | Le build utilise DirectX 11 et des jonctions NTFS |
| **Lecteur G: formaté en NTFS** | **[VÉRIFIÉ]** Le build crée des jonctions `mklink /J` (`Sources/SpaceEngineers/SpaceEngineers.csproj` l.493-503). Elles échouent sur exFAT/FAT32. Vérifie avec `fsutil fsinfo volumeinfo G:`. |
| Visual Studio | Le README officiel demande VS2013 Update 4. Les projets sont en `ToolsVersion 12.0` et ciblent **.NET 4.6.1** (19 projets). **[À TESTER]** VS2015 ou VS2017 avec le *targeting pack* .NET 4.6.1, plus le composant « Text Template Transformation » (requis par `3rd\Utils\RunTemplate.bat`). |
| Steam + Space Engineers | Copie possédée. Steam doit tourner pendant le lancement (AppId 244850). |
| VR | SteamVR installé. Quest 2/3 relié au PC par Link (câble) ou Air Link. Le jeu ne tourne **pas** en autonome sur le casque. |

## 2. Récupérer le code sur G:

```bat
git clone -b devin/1791514358-sync-upstream-vr https://github.com/proelitem/SpaceEngineers.git G:\SpaceEngineers
```

Garde un chemin court et sans espaces : les commandes `mklink` du build sont sensibles aux chemins.

## 3. Le contenu du jeu (le point bloquant le plus probable)

Le dépôt ne contient **ni modèles, ni textures, ni sons**. Le build relie le dossier `Content` d'une installation Steam (`SpaceEngineers.csproj` l.500-503), mais Data et Shaders viennent du dépôt (`DataFromSources` / `EffectsFromSources` = `true`).

- **[VÉRIFIÉ]** Le code attend le contenu de la version 01.169.002 (début 2017).
- **[À TESTER]** Le Space Engineers actuel de Steam a plusieurs années d'écart. Il faut s'attendre à des modèles et formats incompatibles. Le README officiel prévient qu'un décalage, même faible, provoque des asserts « unable to find … file » en Debug ; en Release, les fichiers manquants ne font pas planter le jeu.
- Option 1 : essayer d'abord avec le contenu Steam actuel, en **Release**, pour voir jusqu'où on va.
- Option 2 : récupérer le contenu de la version de janvier 2017 (par exemple via les manifests de dépôt Steam d'un jeu que tu possèdes). **[À TESTER]** Je n'ai vérifié ni la disponibilité de ces anciennes versions ni les conditions d'utilisation Steam. Vérifie-les avant.

Pour mettre la bibliothèque Steam sur G: : *Steam → Paramètres → Stockage → Ajouter un lecteur → G:*, puis installe ou déplace Space Engineers dessus.

### `user.props` (à créer dans `G:\SpaceEngineers\user.props`)
`global.props` l'importe après sa propre valeur, donc il la remplace. **[VÉRIFIÉ]** `global.props` est lui-même importé en fin de `SpaceEngineers.csproj` (l.484).

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ContentPath>G:\SteamLibrary\steamapps\common\SpaceEngineers\Content</ContentPath>
  </PropertyGroup>
</Project>
```

Adapte le chemin à ton installation. Ne versionne pas ce fichier : il est propre à ta machine.

## 4. Premier build sans VR (x86) : valider la chaîne

1. Ouvre `G:\SpaceEngineers\SpaceEngineers.sln`.
2. Choisis la configuration **Release | x86** et le projet de démarrage `SpaceEngineers`.
3. Lance Steam, puis F5.

**[VÉRIFIÉ]** Les bibliothèques tierces fournies dans `3rd\` n'existent **qu'en x86** : `HavokWrapper_SE\release\x86`, `SteamSDK\{debug,release}\x86`, `RakNet\…\x86`, `VRage.Native\…\x86`. Le README le confirme : « We're unable to provide a 64-bit version of all 3rd party libraries because of licensing ».

## 5. Activer la VR (prototype)

### 5.1 Obstacle principal : il faut du 64 bits
- **[VÉRIFIÉ]** Le `Sources/VRage.OpenVRWrapper/openvr_api.dll` fourni est **x64** (PE32+). Il est copié dans le dossier de sortie (`VRage.OpenVRWrapper.csproj` l.155-156). Un build x86 ne pourra pas le charger : `MyOpenVR.cs` l.198-204 lève alors « Failed to load library (ErrorCode: 193) », et le commentaire de la l.199 cite déjà cette erreur 193.
- Un build **x64** cherche `..\..\3rd\HavokWrapper_SE\Release\x64\HavokWrapper.dll` (`Sandbox.Game.csproj` l.127), et de même pour `SteamSDK` et `RakNet` (l.176, l.180). **Ces DLL x64 ne sont pas dans le dépôt.**
- **[À TESTER]** La seule source réaliste est le dossier `Bin64` d'une installation du jeu de la **même version** (01.169.002). Les DLL du jeu actuel ont très probablement une interface différente.

### 5.2 Activer le rendu stéréo
Par défaut, `UseStereoRendering = false` (`Sources/VRage.Render/MyRenderSettings.cs` l.38). Deux façons de l'activer :

- **Simple (pour tester)** : dans `Sources/VRage/MyCompilationSymbols.cs` l.40, passe `DX11ForceStereo` à `true`. Effets **[VÉRIFIÉ]** :
  - la stéréo est forcée (`MyVideoSettingsManager.cs` l.210-211) ;
  - un échec d'initialisation d'OpenVR est toléré, avec un masque stéréo par défaut (`MyRender-Content.cs` l.67-80).
- **Plus propre** : dans `Sources/SpaceEngineers.Game/SpaceEngineersGame.cs` l.148-156, décommente `MyPerGameSettings.DefaultRenderDeviceSettings` et ajoute `UseStereoRendering = true`. Le réglage est repris à la l.204-207 de `MyVideoSettingsManager.cs`.

Si la création du device stéréo échoue, le moteur se rabat sur un rendu normal (`MyRender-Device.cs` l.191-196). Lis le log pour le savoir : « CreateDevice: Attempt to create stereo renderer ».

### 5.3 Lancement
1. Démarre SteamVR, avec le Quest connecté via Link.
2. Lance le jeu en **x64**. **[VÉRIFIÉ]** `MyOpenVR` appelle `OpenVR.Init(... VRApplication_Scene)` (`MyOpenVR.cs` l.208), puis affiche le compositeur et la fenêtre miroir (l.222-228).
3. Écran de debug « VR » (`MyVRDebugInputComponent.cs`) : timings, IPD, boutons des manettes. Raccourcis :
   - Ctrl + `+` : journaliser les timings ;
   - Ctrl + `-` : figer les timings ;
   - Ctrl + `*` : LMU ;
   - Ctrl + `/` : image 2D ;
   - Ctrl + `.` du pavé numérique : synchronisation.

### 5.4 Options expérimentales
`Sources/Sandbox.Game/Engine/Utils/MyFakes_Default.cs` l.684-690 : `ENABLE_VR_BUILDING`, `ENABLE_VR_DRONE_COLLISIONS`, `ENABLE_VR_FORCE_BLOCK_DESTRUCTIBLE`, etc. Toutes sont à `false` par défaut.

## 6. Limites connues
- **[VÉRIFIÉ]** Le personnage VR `MyGhostCharacter` (casque + une arme par main) existe dans le code. Je n'ai trouvé ni code qui le fasse apparaître ni définition associée dans les `.sbc` du dépôt. Le mode de jeu VR complet n'est donc probablement **pas** inclus.
- **[À TESTER]** Le `openvr_api.dll` date de 2016 et utilise l'ancien système d'entrées des manettes (avant SteamVR Input). SteamVR propose en principe une compatibilité « legacy » pour les manettes Touch.
- C'était un prototype interne : attends-toi à des plantages et des réglages au cas par cas.

## 7. Dépannage rapide
| Symptôme | Piste |
|---|---|
| `RunTemplate.bat … exited with code 1` | Composant Text Templating absent, ou version de VS trop ancienne (README) |
| Asserts « unable to find audio/model/texture file » | Contenu Steam différent de la version 01.169.002 : passe en Release |
| `Failed to load library (ErrorCode: 193)` | Build x86 avec `openvr_api.dll` x64 : il faut un build x64 |
| Référence `HavokWrapper` introuvable en x64 | DLL x64 absentes de `3rd\…\x64` (voir 5.1) |
| Erreurs `mklink` | G: n'est pas en NTFS, ou le chemin contient des espaces |
