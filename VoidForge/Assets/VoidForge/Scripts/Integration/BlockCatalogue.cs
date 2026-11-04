using System;
using UnityEngine;

namespace VoidForge.Integration
{
    [CreateAssetMenu(menuName = "VoidForge/Block catalogue")]
    public sealed class BlockCatalogue : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string id;
            public GameObject prefab;
            public Vector3Int size = Vector3Int.one;
            public bool Available => prefab != null;
        }
        public Entry[] entries;
        public Entry Find(string id) => Array.Find(entries, e => e.id == id);
    }
}
