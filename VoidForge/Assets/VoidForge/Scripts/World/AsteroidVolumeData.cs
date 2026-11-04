using System;
using UnityEngine;

namespace VoidForge.World
{
    // Generated from the owned local meshes; the data asset belongs under ignored ThirdParty.
    public sealed class AsteroidVolumeData : ScriptableObject
    {
        [Serializable] public sealed class Variant { public float[] signedDistance; }
        public int resolution = 41;
        public float sampleStep = 1f / 32;
        public Variant[] variants;
    }
}
