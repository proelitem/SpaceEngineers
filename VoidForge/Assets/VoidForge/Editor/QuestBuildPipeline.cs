using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VoidForge.Editor
{
    /// <summary>
    /// Build de l'APK Quest 3.
    /// Menu : VoidForge > 3. Construire l'APK Quest 3 (ou la variante développement).
    /// Ligne de commande : -executeMethod VoidForge.Editor.QuestBuildPipeline.BuildBatch [-vfDev] [-vfOutput chemin.apk]
    /// </summary>
    public static class QuestBuildPipeline
    {
        public const string DefaultOutput = "Builds/VoidForge-Quest3.apk";

        [MenuItem("VoidForge/3. Construire l'APK Quest 3", priority = 3)]
        public static void BuildRelease()
        {
            Build(DefaultOutput, development: false, batch: false);
        }

        [MenuItem("VoidForge/3b. Construire l'APK Quest 3 (développement)", priority = 4)]
        public static void BuildDevelopment()
        {
            Build("Builds/VoidForge-Quest3-dev.apk", development: true, batch: false);
        }

        [MenuItem("VoidForge/0. Tout faire (configurer, scène, APK)", priority = 0)]
        public static void BuildEverything()
        {
            Quest3ProjectConfigurator.Configure();
            MainSceneBuilder.Build();
            Build(DefaultOutput, development: false, batch: false);
        }

        public static void BuildBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool dev = Array.IndexOf(args, "-vfDev") >= 0;
            string output = DefaultOutput;
            int idx = Array.IndexOf(args, "-vfOutput");
            if (idx >= 0 && idx + 1 < args.Length) output = args[idx + 1];

            if (!File.Exists(MainSceneBuilder.ScenePath))
            {
                Quest3ProjectConfigurator.Configure();
                MainSceneBuilder.Build();
            }
            Build(output, dev, batch: true);
        }

        private static void Build(string output, bool development, bool batch)
        {
            if (!File.Exists(MainSceneBuilder.ScenePath))
            {
                Debug.LogError("[VoidForge] Scène absente : lance d'abord VoidForge > 2. Construire la scène principale.");
                if (batch) EditorApplication.Exit(2);
                return;
            }

            string dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            PlayerSettings.Android.bundleVersionCode += 1;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainSceneBuilder.ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler
                    : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[VoidForge] APK construit : {output} ({summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime.TotalSeconds:0} s).");
                if (batch) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[VoidForge] Échec du build : {summary.result}, {summary.totalErrors} erreur(s).");
                if (batch) EditorApplication.Exit(1);
            }
        }
    }
}
