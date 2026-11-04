# INTEGRATION_04 — recette autorisée du 4 novembre 2026

Équipement, construction et modules : [spécification et reproduction](../../docs/RECETTE_04.md).
Cette tranche est une extension solo autorisée ; elle ne clôt pas J2/J3 ou le réseau.

## Vérifié

- Unity `6000.4.0f1`, compilation et génération réussies. Le module intégré
  `com.unity.modules.particlesystem@1.0.0` est ajouté pour Sci-Fi Arsenal.
- Trois modèles originaux GPT-Astra produits via Blender MCP, références 2D relues :
  soute 1 × 1 × 1 / 5 084 triangles, raffinerie 1 × 2 × 1 / 11 376, assembleur
  2 × 1 × 1 / 13 844. Les bornes des prefabs Unity sont vérifiées après conversion FBX.
- 62 contrôles .NET du registre économique ; 120 assertions Unity dans Main, avec deux
  chargements du même monde de test et sans exception capturée.
- Les douze blueprints utilisent les mêmes meshes que leur véritable entrée de catalogue.
  Sélection répétée, coûts installés, soudage partiel, bloc complet, broyage jusqu’à zéro,
  récupération au sac et refus en cas de stock à préserver sont vérifiés.
- ON/OFF batterie conserve sa charge ; production coupée arrête les machines ; le solaire
  dépend de l’orientation et de l’occultation, y compris si la sonde est dans un obstacle.
  Le réacteur consomme l’uranium réel de sa soute et s’arrête sans consommation lorsqu’il est OFF.
- Physique Unity réelle : à force identique, doubler la masse divise par deux accélération
  et freinage. La masse publiée comprend soute et réservations de production.
- Trois outils : gâchette maintenue, particules Arsenal bouclées, émission arrêtée au
  relâchement ; la géométrie des prises conserve son échelle.
- Shader stable : albédo source et repère par astéroïde partagés entre LOD source et chunks
  creusés, shader compilé ; deux excavations modifient le collider et restituent le vrai minerai.
- Contact des gants : dessous reconstruits sur la peau skinnée, extrusion 1,5 mm, rendus
  Blender ouverts/fermés et rendu Unity relus. VRIK et correction de prise de 45° contrôlés.
- Cyberdeck : semelle conformée à 77 points de peau ; HUD à 0,15 mm du centre de dalle en
  mètres du monde. Rendus depuis le côté et les yeux relus. Aucun contrôle ne certifie
  un contact parfait dans toutes les postures possibles du casque.
- Nouveau pictogramme Module ImageGen, onglet Systèmes, renommage au clavier virtuel,
  énergie et masses rendus dans le HUD natif.
- Copie locale du monde Quest : les 8 blocs d’origine, 6 plans, 58 excavations et les stocks
  sont conservés par migration puis par un second chargement. La batterie étant presque
  vide et la base sans générateur, un panneau solaire de départ est provisionné une seule
  fois : 9 blocs après migration. Le marqueur durable empêche les doublons ; aucune
  recharge de batterie ni modification des stocks d’origine n’est appliquée.

Preuves : [registre](../Preuves/INTEGRATION_04/core-validation.json),
[Play Main](../Preuves/INTEGRATION_04/play-validation.json),
[migration](../Preuves/INTEGRATION_04/migration-validation.json),
[équipement](../Preuves/INTEGRATION_04/equipment-validation.json),
[contact des gants](../Preuves/INTEGRATION_04/hand-contact-validation.json).

## Livraison Android et Git

Build Unity **Success**, 1 767 s. APK local :
`Builds/VoidForge-Quest3-integration04-dev.apk`, version 1.0/code **13**, ARM64,
minSdk 32, targetSdk 36, **144 899 137 octets**.
SHA-256 : `A7DBF00DD9010CA17FEB2E5D61E1EEDFAA04CCC9BC4F48DF482A54C02E959BE4`.
L’APK, les symboles, les caches et les journaux restent locaux et exclus de Git.

Installation `adb install -r` : **Success**. Deux lancements COLD, deux captures de
**35,356 s et 35,972 s**, application toujours vivante, six marqueurs prêts et aucune
exception/E-F Unity/AndroidRuntime/XR_ERROR. Le tampon logcat est restauré de 4 Mo à 256 Ko.
Le port du casque et les interactions manuelles ne sont pas certifiés par cette capture.

Sur Quest : format 2, **9 blocs, 6 plans, 58 excavations**, deux modules ; identifiants
conservés au redémarrage et un seul panneau de migration. Les stocks évoluent entre les
captures parce que la production tourne : ces deux instantanés ne sont pas annoncés égaux.
La charge de batterie positive et la file d’assemblage terminée confirment la reprise de
production avec le solaire de démarrage.

Preuves : [APK](../Preuves/INTEGRATION_04/build-validation.json),
[Quest](../Preuves/INTEGRATION_04/quest-validation.json),
[métadonnées](../Preuves/INTEGRATION_04/meta-validation.json) : 1 051 GUID uniques,
aucune métadonnée manquante pour les fichiers de cette passe. Sources et résultats sont
livrés uniquement sur `claude/relaxed-darwin-9f11j0` ; aucun push sur master.

## À tester par Pierre

- Contact et orientation des mains/cyberdeck sur ses postures réelles ; prise à la cuisse,
  confort du décalage de prise et lisibilité du HUD en stéréo.
- Ressenti des effets, gestes de soudage/broyage continus et visibilité de l’avancement.
- Pilotage en cockpit, réponse chargé/à vide, freinage et confort.
- Performance CPU/GPU/FPS sur casque porté et réseau à plusieurs appareils.

Les validations de locomotion et de creusage du J1 précédent restent historiques ; elles
ne certifient pas automatiquement cette nouvelle tranche. Aucun code ni asset Keen n’est
utilisé. Les ressources sous licence restent locales et ignorées ; seuls les modèles
originaux, adaptateurs, métadonnées et preuves propres à cette passe sont livrés.
