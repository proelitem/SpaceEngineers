# VoidForge — consignes de projet

Le protocole commun de coordination Codex reste applicable.

- Périmètre autorisé : ce dossier `VoidForge/` uniquement. Projet Unity indépendant MIT.
  Ne jamais importer, réutiliser ni compiler le code ou les ressources Keen du dépôt parent.
- Branche de livraison J1 : `claude/relaxed-darwin-9f11j0`. Jamais de push sur `master`.
- Éditeur retenu par Pierre le 2 novembre 2026 : Unity `6000.4.0f1`.
  Utiliser son module Android Unity Hub (SDK, NDK, OpenJDK), sans SDK externe.
- Documentation canonique : `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/TEST_J1.md`.
  `Hermes_Works/README.md` en est l'index de travail ; les preuves du J1 sont dans
  `Hermes_Works/Jalons/J1.md`. Ne pas dupliquer les documents dans Obsidian.
- Décisions propriétaire : `G:/hermes/Vault/Jarvis3D/Projets/VoidForge/APPROBATIONS.md`,
  selon le format du protocole commun. Ne pas y envoyer les décisions techniques.
  Le périmètre de cette intervention interdit de créer ou modifier des fichiers hors `VoidForge/`.
- Exception de cache explicitement autorisée par Pierre : `Library/PackageCache` peut être
  une jonction vers `D:/Temp/VoidForge-PackageCache`. Ce dossier ne contient que les
  dépendances Unity jetables ; les sources, réglages et livrables restent dans `VoidForge/`.
- Pour le build Android, fixer `GRADLE_USER_HOME` à `VoidForge/Library/GradleUserHome`
  dans le processus lancé. Ne pas hériter d'un cache Gradle appartenant à un autre projet.
- Limiter aussi la compilation native à `BEE_BUILD_THREADS=4` dans le processus lancé :
  vingt compilateurs simultanés ont épuisé la mémoire disponible (LLVM out of memory).
- Coordination active : `VOIDFORGE_AGENT_COMM.md` dans ce dossier ; aucun secret,
  aucune question propriétaire, aucune archive des points résolus.
- Conserver les `.meta`, réglages XR, scène, matériaux et prefab générés dans Git.
  Ne pas versionner `Library/`, `Logs/`, `Builds/`, les APK ou les caches.
- J1 : s'arrêter après la livraison technique et les notes de recette. Locomotion,
  confort, rendu en casque et réseau à plusieurs appareils restent à valider par Pierre.
- Si une même erreur bloque plus de 20 minutes, arrêter et consigner son message exact.
