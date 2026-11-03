# J1 — production visuelle des outils à main

Extension demandée par Pierre le 3 novembre 2026 : produire les trois outils
industriels futuristes dans Blender MCP, avec GPT-Astra et le contrat Modeler3D
de Jarvis3D. La livraison concerne les modèles et leurs échanges avec Unity.

Référence canonique : [OUTILS_MAIN.md](../../docs/OUTILS_MAIN.md).

| Vérification | Résultat |
|--------------|----------|
| Modèle et transport | GPT-6 Astra high dans Codex ; exécution native Blender MCP 5.2.1 LTS ; contrat Jarvis Modeler3D réutilisé |
| Trois outils complets | Foreuse, meuleuse à renvoi d'angle et soudeuse ; revue de silhouette et de plusieurs angles effectuée |
| Source et aperçus | Deux sources `.blend`, scripts reproductibles, galerie et trois vues individuelles sauvegardés |
| Géométrie | 5 520 / 5 064 / 4 920 triangles ; trois assemblages par outil, quatre matériaux PBR partagés |
| Six exports FBX/GLB | Réimportations indépendantes réussies ; dimensions concordantes, ancrages présents, zéro triangle dégénéré |
| Import Unity 6000.4.0f1 | Trois FBX importés et validateur compilé dans un projet isolé ; géométrie, matériaux et ancrages présents |
| Métadonnées | Onze `.meta` Unity conservés ; GUID distincts |
| Matériaux URP | À convertir : les matériaux FBX sont importés avec `Standard` ; rendu URP non vérifié |
| Prise XR et commandes des outils | À intégrer puis tester par Pierre |
| Mécaniques de minage/meulage/soudage | À implémenter ; voir les exigences de minage |
| Performances en casque | À tester par Pierre après intégration ; aucun FPS/draw call mesuré pour ces outils |
| APK et réseau de ces outils | Pas de nouvel APK ; ces modèles ne qualifient pas leurs futurs comportements réseau |

Preuves détaillées dans `Art/HandTools/modeler3d_report.json` et
`Art/HandTools/Validation/`. Journaux locaux : `Logs/hand-tool-readback.log`
et `Logs/hand-tool-unity.log`. L'incident natif et la récupération sont consignés
dans le document canonique ; les modèles et exports finaux sont sauvegardés.

DRILL-R demeure la ressource de vaisseau retenue. Les sources de G:/UnityAssets,
le backend Jarvis3D et les travaux graphiques/astronaute concurrents sont préservés.
Aucun achat, génération externe ou appel HTTP payant Jarvis n'a été déclenché.
