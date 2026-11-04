using System.Collections.Generic;
using UnityEngine;

namespace VoidForge.Integration
{
    // One-metre grid. First-cell pivot; all occupied cells are rotated around it.
    // Local assembly proof only: no recipes, integrity, ship physics or network authority.
    public sealed class BlockGrid : MonoBehaviour
    {
        public BlockCatalogue catalogue;
        readonly Dictionary<Vector3Int, GameObject> occupied = new Dictionary<Vector3Int, GameObject>();
        public int OccupiedCount => occupied.Count;

        public bool TryPlace(string id, Vector3Int cell, Quaternion rotation, out GameObject instance)
        {
            instance = null;
            var entry = catalogue.Find(id);
            if (entry == null || !entry.Available) return false;
            foreach (var axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
            {
                Vector3 v = rotation * axis;
                if ((v - (Vector3)Vector3Int.RoundToInt(v)).sqrMagnitude > .000001f) return false;
            }
            var cells = new List<Vector3Int>();
            for (int x = 0; x < entry.size.x; x++)
            for (int y = 0; y < entry.size.y; y++)
            for (int z = 0; z < entry.size.z; z++)
            {
                var target = cell + Vector3Int.RoundToInt(rotation * new Vector3(x, y, z));
                if (occupied.ContainsKey(target)) return false;
                cells.Add(target);
            }
            instance = Instantiate(entry.prefab, transform);
            instance.transform.SetLocalPositionAndRotation(cell, rotation);
            foreach (var c in cells) occupied.Add(c, instance);
            return true;
        }
    }
}
