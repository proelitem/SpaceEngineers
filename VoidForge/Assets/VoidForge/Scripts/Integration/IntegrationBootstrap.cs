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
            var catalogue = Resources.Load<BlockCatalogue>("VoidForge/Integration/BlockCatalogue");
            if (catalogue == null) return;
            var grid = new GameObject("IntegrationBlockGrid").AddComponent<BlockGrid>();
            grid.catalogue = catalogue;
            grid.transform.position = new Vector3(3f, -1f, 5f);
            grid.TryPlace("ArmorCube", Vector3Int.zero, Quaternion.identity, out _);
            grid.TryPlace("ArmorSlope", Vector3Int.right, Quaternion.identity, out _);
            grid.TryPlace("ArmorCorner", new Vector3Int(2, 0, 0), Quaternion.identity, out _);
            grid.TryPlace("Gyroscope", new Vector3Int(0, 0, 2), Quaternion.identity, out _);
            grid.TryPlace("Battery", new Vector3Int(2, 0, 2), Quaternion.identity, out _);
            grid.TryPlace("Thruster", new Vector3Int(4, 0, 2), Quaternion.identity, out _);
            grid.TryPlace("Cockpit", new Vector3Int(6, 0, 2), Quaternion.identity, out _);
            grid.TryPlace("DrillR", new Vector3Int(-8, 0, 0), Quaternion.identity, out _);
            Debug.Log($"[VoidForge Integration] Local block display: {grid.OccupiedCount} occupied cells; refinery/assembler/cargo pending.");
        }
    }
}
