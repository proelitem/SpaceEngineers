# Recette du HUD cyberdeck

[Galerie des cinq vues et superposition aux concepts](index.html) ·
[Rendu sur le cyberdeck 3D](device-preview.png) ·
[Intégration, commandes et limites](../../CYBERDECK_HUD.md)

- **36 contrôles natifs uGUI réussis**, incluant la présence effective des panneaux dans
  les pixels rendus. [Rapport Unity](unity-validation.json).
- **10 assertions du HUD dans la scène du jeu réussies**, montage Equipment final,
  199,5829 × 85,7143 mm, orientation extérieure, trois outils et 40 repères réels.
  [Rapport Play ciblé](game-validation.json).
- Cinq vues à 1914 × 822 : inventaire, construction, raffinerie, assemblage, GPS/carte.
  Les données illustratives sont marquées ; les interfaces du jeu restent déconnectées
  en l'absence des systèmes de soute/cockpit/production.
- États capturés : cockpit déconnecté, soute déconnectée et composants insuffisants.
- Le shader ne présente aucune erreur détectée. Les 39 PNG d'Assets sont identiques
  aux fichiers ImageGen du catalogue original. Police DejaVu et notices conservées.
- Galerie rendue et inspectée avec Chrome, profil isolé ; les cinq vues natives et la
  capture du modèle ont aussi été inspectées directement.

**La cible de fidélité de 98 % n'est pas certifiée.** La comparaison RGB dans
[visual-comparison.json](visual-comparison.json) est un diagnostic de différence entre
images, pas un pourcentage de fidélité perceptuelle. Les nouveaux objets graphiques,
la typographie et la carte à coordonnées réelles créent des écarts aux illustrations.

Le Play comporte une exception Digger à la sortie, `get_internalHeightmapResolution can
only be called from the main thread`, ainsi qu'un avertissement de deux allocations
persistantes. Leurs causes ne sont pas établies par cette passe et le code du minage n'a
pas été modifié. Voir le journal local `Logs/cyberdeck-game-1.log` et la documentation
d'intégration ; ne pas présenter les assertions ciblées comme un test global sans erreur.

Aucune recette du nouveau HUD en casque n'a été effectuée. Le build APK appartient à
la passe équipement en cours ; les interfaces Unity sont prêtes dans le projet.
