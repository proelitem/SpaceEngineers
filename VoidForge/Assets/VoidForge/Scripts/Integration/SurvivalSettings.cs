using UnityEngine;

namespace VoidForge.Integration
{
    public sealed class SurvivalSettings : ScriptableObject
    {
        public const string Resource = "VoidForge/Survival/Settings";
        public GameObject asteroidField;
        public Material hologram;
        public int seed = 421103;
        public static bool Supported
        {
            get
            {
#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
                return true;
#else
                return false;
#endif
            }
        }
    }
}
