# Feuille de route

Chaque jalon se termine par : code poussé, APK si possible, notes de test, limites connues.
On s'arrête à chaque jalon pour validation sur le Quest 3 avant de continuer.

## Jalons gameplay (repris de la fiche 1 du rapport)

| Jalon | Contenu | État |
|-------|---------|------|
| **J1** Squelette | Projet Unity, scène VR, locomotion en apesanteur (poussée à la main + rotation par crans), pipeline APK, base réseau (avatars tête/mains) | **APK livré ; contrôles, locomotion, confort et performances validés par Pierre le 3 novembre 2026. Réseau à deux appareils et mesures chiffrées à consigner ; exception Search du test éditeur documentée** |
| Extension J1 assets et creusage | Astéroïdes de G:/UnityAssets, LOD et volume solo Digger sur support terrain masqué | APK code 8 livré ; creusage/collider vérifiés, logcat sans exception ; [creusage et performances élevées confirmés en casque par Pierre](../Hermes_Works/Jalons/J1_ASSETS.md) |
| Suite minage | Formes des assets d'astéroïdes, veines selon les paramètres Space Engineers, outils à main et sur vaisseau | Exigences confirmées par Pierre ; [spécification et recette](MINAGE.md), implémentation à faire |
| J2 Construction | Grille de blocs (armure, cockpit, propulseur, gyroscope, batterie), placer/retirer au contrôleur, rotation, aperçu fantôme | À faire |
| J3 Vaisseau | Grille → corps rigide, masse et centre de masse, s'asseoir au cockpit, pilotage 6 DDL, HUD (vitesse, énergie) | À faire |
| J4 Persistance et perf | Sauvegarde/chargement JSON, fusion des maillages (200+ blocs), profilage documenté | À faire |
| J5 Finitions | Menu au poignet, sélection des blocs, astéroïde décoratif, sons libres, guide des contrôles | À faire |

## Jalons multijoueur (ajoutés à la demande de Pierre)

| Jalon | Contenu | Dépend de |
|-------|---------|-----------|
| M0 Base | NetworkManager + Unity Transport, hôte/client par IP locale, avatars tête/mains | J1 (sources livrées ; test à deux appareils par Pierre en attente) |
| M1 Joueurs | Rig joueur = objet réseau à autorité propriétaire, noms/couleurs, déconnexion propre | J1 |
| M2 Construction partagée | Grille répliquée : placements validés par le serveur, diffusion aux clients, arrivée tardive (état complet) | J2 |
| M3 Vaisseaux partagés | Pilotage avec prédiction côté pilote, réconciliation serveur, passagers | J3 |
| M4 Hors réseau local | Relais (Unity Relay ou serveur dédié Linux), code de session, voix (optionnel) | M3 |

## Ordre conseillé

J1 → test en casque → M1 (court) → J2 → M2 → J3 → M3 → J4 → J5 → M4.
Le multijoueur est intégré au fil des jalons plutôt qu'à la fin, pour éviter de refaire la
construction et le vaisseau en version réseau après coup.

## Extension autorisée le 4 novembre : INTEGRATION_03

Recette corrective des mains/du poignet/des outils et nouvelle scène solo : atelier
sur quatre blocs, astéroïdes issus des assets, veines voxel, GPS et plans bleus soudables.
Le Play de Main valide 35 contrôles, dont collecte et restauration sans duplication.
Cette tranche prépare la construction ; elle ne clôt pas J2 ni les jalons réseau.
APK 12 installé sur Quest : deux captures de plus de 35 s sans défaut relevé et
persistance des huit blocs/stocks vérifiée. L'essai porté de cette nouvelle tranche
reste à suivre dans [INTEGRATION_03](../Hermes_Works/Jalons/INTEGRATION_03.md).
