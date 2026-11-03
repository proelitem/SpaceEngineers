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
            var mining = Resources.Load<GameObject>("VoidForge/LocalMiningPrototype");
            if (mining != null)
            {
                Instantiate(mining);
                Debug.Log("[VoidForge] Prototype voxel local Digger chargé.");
            }
        }
    }
}
