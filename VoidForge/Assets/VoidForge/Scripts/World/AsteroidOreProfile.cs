using System;
using UnityEngine;

namespace VoidForge.World
{
    // Original, versioned prototype tuning. Public SE sources establish ore families and
    // buried deposits, not their exact probabilities or dimensions. Never read Keen files.
    public static class AsteroidOreProfile
    {
        public const string Version = "voidforge-asteroid-veins-v1";
        public static readonly string[] Items = { "ore-stone", "ore-iron", "ore-nickel", "ore-silicon", "ore-cobalt", "ore-magnesium", "ore-silver", "ore-gold", "ore-platinum", "ore-uranium", "ore-ice" };
        public static readonly Color[] Colors = { new Color(.24f,.25f,.27f), new Color(.5f,.25f,.12f),
            new Color(.45f,.48f,.37f), new Color(.62f,.65f,.70f), new Color(.15f,.30f,.58f),
            new Color(.8f,.78f,.65f), new Color(.68f,.72f,.78f), new Color(.82f,.57f,.16f),
            new Color(.5f,.65f,.7f), new Color(.3f,.36f,.18f), new Color(.5f,.85f,.95f) };

        [Serializable] public struct Vein
        {
            public Vector3 a, b;
            public float radius;
            public int material;
        }
        public static Vein[] Generate(int seed, int index)
        {
            var random = new System.Random(seed + index * 7919);
            // Keep a few stone-only asteroids; every ore family remains reachable.
            if (index % 9 == 8) return Array.Empty<Vein>();
            int first = 1 + index % 10;
            int second = 1 + random.Next(10);
            if (second == first) second = 1 + second % 10;
            var veins = new Vein[2];
            for (int i = 0; i < veins.Length; i++)
            {
                Vector3 centre = new Vector3(Range(random,-.20f,.20f),Range(random,-.20f,.20f),Range(random,-.20f,.20f));
                Vector3 direction = new Vector3(Range(random,-1,1),Range(random,-1,1),Range(random,-1,1)).normalized;
                if (direction.sqrMagnitude < .5f) direction = Vector3.up;
                veins[i] = new Vein { a = centre - direction * .23f, b = centre + direction * .23f,
                    radius = Range(random,.055f,.095f), material = i == 0 ? first : second };
            }
            return veins;
        }
        static float Range(System.Random random,float a,float b) => a + (float)random.NextDouble() * (b-a);
        public static int MaterialAt(Vector3 local, Vein[] veins)
        {
            foreach (var vein in veins)
            {
                Vector3 axis = vein.b-vein.a;
                float t = Mathf.Clamp01(Vector3.Dot(local-vein.a,axis)/axis.sqrMagnitude);
                if ((local-vein.a-axis*t).sqrMagnitude <= vein.radius*vein.radius) return vein.material;
            }
            return 0;
        }
    }
}
