using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
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

            // Isole aussi les builds par menu d'un GRADLE_USER_HOME hérité d'un autre projet.
            string previousGradleHome = Environment.GetEnvironmentVariable("GRADLE_USER_HOME");
            string previousBeeThreads = Environment.GetEnvironmentVariable("BEE_BUILD_THREADS");
            BuildReport report;
            try
            {
                Environment.SetEnvironmentVariable("GRADLE_USER_HOME", Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/GradleUserHome")));
                // Évite les échecs LLVM « out of memory » avec vingt compilateurs natifs simultanés.
                Environment.SetEnvironmentVariable("BEE_BUILD_THREADS", "4");
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GRADLE_USER_HOME", previousGradleHome);
                Environment.SetEnvironmentVariable("BEE_BUILD_THREADS", previousBeeThreads);
            }
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

    /// <summary>Limite la concurrence Gradle pour éviter les timeouts AAPT2 observés au premier build.</summary>
    public sealed class QuestGradleConfigurator : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string propertiesPath = Path.Combine(Path.GetDirectoryName(path), "gradle.properties");
            string properties = File.ReadAllText(propertiesPath);
            const string pattern = @"(?m)^org\.gradle\.workers\.max=.*$";
            properties = Regex.IsMatch(properties, pattern)
                ? Regex.Replace(properties, pattern, "org.gradle.workers.max=4")
                : properties + "\norg.gradle.workers.max=4\n";
            File.WriteAllText(propertiesPath, properties);
            Debug.Log("[VoidForge] Gradle limité à 4 workers.");
        }
    }
}
