// Copied into the isolated validation project's Assets/Editor by the PowerShell launcher.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HandToolImportCheck
{
    [Serializable] public sealed class Tool
    {
        public string name;
        public long triangles;
        public int meshes;
        public Vector3 bounds_m;
        public string[] anchors;
        public string[] materials;
        public string[] shaders;
        public bool success;
    }
    [Serializable] public sealed class Report
    {
        public bool success;
        public string unity_version;
        public Tool[] tools;
        public string error;
        public string limitations = "Import geometry only; URP appearance, XR and Quest not tested";
    }

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int argument = Array.IndexOf(args, "-vfHandToolReport");
        if (argument < 0 || argument + 1 == args.Length)
            throw new ArgumentException("-vfHandToolReport requires a path");
        string path = Path.GetFullPath(args[argument + 1]);
        var report = new Report { unity_version = Application.unityVersion };
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var records = new List<Tool>();
            foreach (string name in new[] { "MiningDrill", "Grinder", "Welder" })
            {
                string assetPath = "Assets/VoidForge/Art/HandTools/" + name + "/VF_" + name + ".fbx";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset == null) throw new Exception("FBX import failed: " + assetPath);
                var instance = UnityEngine.Object.Instantiate(asset);
                try
                {
                    var filters = instance.GetComponentsInChildren<MeshFilter>(true);
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                    if (renderers.Length == 0) throw new Exception("No renderers: " + assetPath);
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    long triangles = 0;
                    foreach (var filter in filters)
                        for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
                            triangles += filter.sharedMesh.GetIndexCount(i) / 3;
                    var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).ToArray();
                    var anchors = instance.GetComponentsInChildren<Transform>(true)
                        .Select(t => t.name).Where(n => n.Contains("Grip") || n.Contains("ToolTip")).ToArray();
                    float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    bool valid = triangles > 0 && triangles <= 8000 && longest >= 0.15f && longest <= 0.65f
                        && anchors.Any(n => n.Contains("GripPrimary")) && anchors.Any(n => n.Contains("ToolTip"))
                        && materials.Length > 0;
                    records.Add(new Tool {
                        name = name, triangles = triangles, meshes = filters.Length, bounds_m = bounds.size,
                        anchors = anchors, materials = materials.Select(m => m.name).Distinct().ToArray(),
                        shaders = materials.Select(m => m.shader == null ? "MISSING" : m.shader.name).Distinct().ToArray(),
                        success = valid
                    });
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            report.tools = records.ToArray();
            report.success = records.Count == 3 && records.All(r => r.success);
        }
        catch (Exception exc)
        {
            report.error = exc.ToString();
            Debug.LogException(exc);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log("HAND_TOOL_IMPORT " + (report.success ? "OK" : "FAILED") + " " + path);
        EditorApplication.Exit(report.success ? 0 : 1);
    }
}
