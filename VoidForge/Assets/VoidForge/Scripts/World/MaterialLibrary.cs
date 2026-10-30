using UnityEngine;

namespace VoidForge.World
{
    /// <summary>
    /// Accès aux matériaux du projet. Les matériaux sont créés par l'outil d'éditeur dans
    /// Assets/VoidForge/Resources/VoidForge/ (donc toujours inclus dans l'APK). Si un matériau
    /// manque (scène construite au runtime sans passer par l'éditeur), on en fabrique un de secours.
    /// </summary>
    public static class MaterialLibrary
    {
        public const string ResourceFolder = "VoidForge";

        private static Material _hand, _rock, _star, _avatar;

        public static Material Hand   => _hand   ??= Load("Mat_Hand",   new Color(0.85f, 0.85f, 0.9f));
        public static Material Rock   => _rock   ??= Load("Mat_Rock",   new Color(0.35f, 0.33f, 0.3f));
        public static Material Star   => _star   ??= Load("Mat_Star",   Color.white, unlit: true);
        public static Material Avatar => _avatar ??= Load("Mat_Avatar", new Color(0.2f, 0.6f, 1f));

        private static Material Load(string name, Color fallbackColor, bool unlit = false)
        {
            var mat = Resources.Load<Material>(ResourceFolder + "/" + name);
            if (mat != null) return mat;
            return CreateFallback(name, fallbackColor, unlit);
        }

        /// <summary>Crée un matériau en mémoire avec le shader URP disponible (ou un shader intégré).</summary>
        public static Material CreateFallback(string name, Color color, bool unlit)
        {
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }
    }
}
