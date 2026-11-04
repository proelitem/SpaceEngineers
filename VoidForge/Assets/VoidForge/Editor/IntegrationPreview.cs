using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoidForge.Integration;
using VoidForge.Player;
using Object = UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class IntegrationPreview
    {
        public static void BuildAndReviewBatch()
        {
            IntegrationWorkshop.BuildBatch();
            Render();
        }

        public static void Render()
        {
            var player = PlayerRigFactory.Create(Vector3.zero);
            var equipment = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(IntegrationWorkshop.Output + "/Equipment.prefab"), player.transform).GetComponent<EquipmentRig>();
            var hmd = player.GetComponentInChildren<Camera>();
            var left = hmd.transform.parent.Find("LeftHand"); var right = hmd.transform.parent.Find("RightHand");
            left.localPosition = new Vector3(-.28f,-.35f,.3f);
            left.localRotation = Quaternion.Euler(-30,0,75); // Turn the inner wrist towards the eyes.
            right.localPosition = new Vector3(.28f,-.35f,.35f);
            equipment.Bind(player.GetComponent<PlayerInputProvider>());
            equipment.astronaut.UpdatePose(true);
            equipment.Take(equipment.tools[0]);
            equipment.astronaut.SetGrip(true, .8f, 0);
            var avatar = equipment.astronaut.avatar;
            var bridge = avatar.GetComponents<MonoBehaviour>().First(c => c.GetType().FullName == "VoidForge.Avatars.AstronautFinalIK");
            var component = bridge.GetType().GetProperty("SolverComponent").GetValue(bridge);
            var solver = component.GetType().GetField("solver").GetValue(component);
            solver.GetType().GetMethod("Initiate", new[] { typeof(Transform) }).Invoke(solver, new object[] { avatar });
            for (int i = 0; i < 10; i++) solver.GetType().GetMethod("Update", Type.EmptyTypes).Invoke(solver, null);
            var fingers = avatar.GetComponents<MonoBehaviour>().First(c => c.GetType().FullName == "VoidForge.Avatars.AstronautFingerPose");
            fingers.GetType().GetMethod("ApplyPose").Invoke(fingers, null);
            equipment.GetComponentInChildren<WristDisplayMount>().Refresh();
            RenderSettings.ambientLight = new Color(.45f,.45f,.45f);
            var sun = new GameObject("PreviewSun").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = 2f; sun.transform.rotation = Quaternion.Euler(35,-40,0);
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f,.045f,.065f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 30;
            hmd.enabled = false;
            Directory.CreateDirectory("Builds/IntegrationPreview");
            Capture(camera, new Vector3(1.8f,.5f,2), new Vector3(0,-.65f,.1f), 50, "avatar-equipment");
            foreach (var tool in equipment.tools) if (tool != equipment.Held) tool.gameObject.SetActive(false);
            equipment.instructions.gameObject.SetActive(false);
            Capture(camera, new Vector3(.7f,-.15f,.6f), new Vector3(.3f,-.35f,.4f), 45, "right-grip");
            Capture(camera, new Vector3(-.05f,-.12f,.65f), new Vector3(.28f,-.35f,.35f), 42, "right-grip-inside");
            equipment.Take(equipment.tools[1]);
            equipment.Held.gameObject.SetActive(true);
            equipment.tools[0].gameObject.SetActive(false);
            equipment.Held.SetRotationPhase(0);
            Capture(camera, new Vector3(.7f,-.12f,.65f), new Vector3(.28f,-.35f,.5f), 45, "grinder-disc-0");
            equipment.Held.SetRotationPhase(90);
            Capture(camera, new Vector3(.7f,-.12f,.65f), new Vector3(.28f,-.35f,.5f), 45, "grinder-disc-90");
            equipment.Held.gameObject.SetActive(false);
            var deck = equipment.GetComponentInChildren<CyberdeckSurface>();
            var screen = deck.screen.bounds.center;
            var filter = deck.screen.GetComponent<MeshFilter>();
            Vector3 screenNormal = filter.transform.TransformDirection(filter.sharedMesh.normals[0]).normalized;
            Capture(camera, screen + screenNormal * .24f + Vector3.up * .02f, screen, 60, "left-wrist");
            Capture(camera, hmd.transform.position, screen, 65, "wrist-from-eyes");
            Object.DestroyImmediate(player); Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(sun.gameObject);
            Debug.Log("VF_INTEGRATION_PREVIEWS Builds/IntegrationPreview");
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, float fov, string name)
        {
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position)); camera.fieldOfView = fov;
            var rt = new RenderTexture(1000,1000,24); var texture = new Texture2D(1000,1000,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0,0,1000,1000),0,0); texture.Apply();
                File.WriteAllBytes("Builds/IntegrationPreview/" + name + ".png", texture.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(rt); Object.DestroyImmediate(texture); }
        }
    }
}
