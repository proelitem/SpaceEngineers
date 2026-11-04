// Editor-only validation helper; keep this file under an Editor directory.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HudMaskImportCheck
{
    [Serializable] private sealed class Report
    {
        public bool success;
        public string unityVersion;
        public string shader;
        public int shaderErrors;
        public string[] messages;
    }

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-vfHudReport");
        if (index < 0 || index + 1 >= args.Length) throw new Exception("Missing -vfHudReport");
        var path = "Assets/VoidForgeGlyphMask.shader";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        if (shader == null) throw new Exception("Shader not imported");
        var messages = ShaderUtil.GetShaderMessages(shader);
        var errors = messages.Count(m => m.severity.ToString() == "Error");
        var report = new Report
        {
            success = errors == 0 && !ShaderUtil.ShaderHasError(shader),
            unityVersion = Application.unityVersion,
            shader = shader.name,
            shaderErrors = errors,
            messages = messages.Select(m => m.severity + ": " + m.message + " (" + m.file + ":" + m.line + ")").ToArray()
        };
        File.WriteAllText(args[index + 1], JsonUtility.ToJson(report, true));
        if (!report.success) throw new Exception("HUD mask shader errors; see report");
        Debug.Log("VF_HUD_MASK_IMPORT_OK");
    }
}
