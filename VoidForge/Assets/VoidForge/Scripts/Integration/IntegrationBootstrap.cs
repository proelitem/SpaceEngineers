using UnityEngine;
using VoidForge.Player;

namespace VoidForge.Integration
{
    public static class IntegrationBootstrap
    {
        public static void Create(PlayerInputProvider player)
        {
            var equipment = Resources.Load<GameObject>("VoidForge/Integration/Equipment");
            if (equipment == null) return; // Local producer dependencies may be absent in a clean MIT clone.
            var rig = equipment.GetComponent<EquipmentRig>();
            if (rig == null || rig.astronaut == null || !rig.astronaut.CanBind)
            {
                Debug.LogWarning("[VoidForge Integration] Local avatar/FinalIK resources missing; stage dependencies and regenerate integration. J1 rig retained.");
                return;
            }
            var instance = Object.Instantiate(equipment, player.transform);
            instance.GetComponent<EquipmentRig>().Bind(player);
        }
    }
}
