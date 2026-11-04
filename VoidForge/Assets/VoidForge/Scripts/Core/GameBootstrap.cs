using UnityEngine;
using VoidForge.Player;

namespace VoidForge.Core
{
    /// <summary>
    /// Point d'entrée de la scène. Garantit qu'un rig joueur existe (secours si la scène n'en a pas)
    /// et fixe quelques réglages globaux.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;

        private void Awake()
        {
            // En VR, la fréquence est imposée par le casque ; on évite seulement toute limite logicielle.
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;

            if (FindFirstObjectByType<PlayerInputProvider>() == null)
            {
                Debug.Log("[VoidForge] Aucun PlayerRig dans la scène : création au runtime.");
                PlayerRigFactory.Create(spawnPosition);
            }
        }

        private void Start()
        {
            VoidForge.Integration.IntegrationBootstrap.Create(FindFirstObjectByType<PlayerInputProvider>());
            var survival = Resources.Load<VoidForge.Integration.SurvivalSettings>(VoidForge.Integration.SurvivalSettings.Resource);
            if (survival != null && survival.asteroidField != null && VoidForge.Integration.SurvivalSettings.Supported)
            {
                var decoration = FindFirstObjectByType<VoidForge.World.AsteroidField>();
                if (decoration != null) decoration.gameObject.SetActive(false);
                if (survival.asteroidField != null) Instantiate(survival.asteroidField);
                return;
            }
            if (FindFirstObjectByType<VoidForge.World.AsteroidField>() == null)
                new GameObject("AsteroidField").AddComponent<VoidForge.World.AsteroidField>();
            var mining = Resources.Load<GameObject>("VoidForge/LocalMiningPrototype");
            if (mining != null)
            {
                Instantiate(mining);
                Debug.Log("[VoidForge] Prototype voxel local Digger chargé.");
            }
        }
    }
}
