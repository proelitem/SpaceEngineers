using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidForge.Integration;
using VoidForge.Player;
using Object = UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class IntegrationValidation
    {
        [Serializable] class Report
        {
            public string unity;
            public int handTools, availableBlocks, pendingBlocks, occupiedTestCells;
            public bool gripAlignment, tipForward, returnToHolster, urpMaterials, wristMount, recenter, finalIK, simulatedTrackingSafety;
            public float rightHandErrorMeters, headErrorMeters;
            public float screenStandOffMeters;
            public bool beltMount, anatomicalCurl, discSpindle, palmSideScreen, cyberdeckRecall;
            public bool anatomicalAssets, constantToolSize, oneSelectedThighTool, visorView;
            public float sleeveContactGapMeters;
        }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException("Integration validation: " + message); }
        static Transform Bone(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        public static void Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = PlayerRigFactory.Create(Vector3.zero);
            var equipment = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(IntegrationWorkshop.Output + "/Equipment.prefab"), player.transform).GetComponent<EquipmentRig>();
            var result = new Report { unity = Application.unityVersion };
            try
            {
                var camera = player.GetComponentInChildren<Camera>().transform;
                camera.localPosition = new Vector3(0,.02f,0);
                var left = camera.parent.Find("LeftHand"); var right = camera.parent.Find("RightHand");
                left.localPosition = new Vector3(-.25f,-.25f,.3f);
                right.localPosition = new Vector3(.25f,-.25f,.3f);
                Vector3 neutralRight = right.position;
                equipment.Bind(player.GetComponent<PlayerInputProvider>());
                result.handTools = equipment.tools.Length;
                ValidateHandAssets(equipment.astronaut.avatar);
                result.anatomicalAssets = true;
                Require(result.handTools == 3, "three tools required");
                foreach (var tool in equipment.tools)
                {
                    equipment.SelectTool(tool);
                    var holster = tool.transform.parent.GetComponent<ThighToolHolster>();
                    Require(holster != null && holster.thigh.name == "RightUpLeg", "right thigh holster required");
                    holster.Refresh();
                    Require(equipment.tools.Count(t=>t.gameObject.activeSelf)==1, "only the selected tool may be visible");
                    Require(tool.tip.position.y < tool.primaryGrip.position.y - .1f, "stowed working end must point down");
                    var parent = tool.transform.parent; var position = tool.transform.localPosition; var rotation = tool.transform.localRotation;
                    float scale = tool.transform.lossyScale.magnitude;
                    float length = Vector3.Distance(tool.primaryGrip.position, tool.tip.position);
                    Require(length > .12f && length < .8f, "authored tool size must remain in metres");
                    equipment.Grab(tool);
                    Require(Mathf.Abs(tool.transform.lossyScale.magnitude-scale)<.001f && Mathf.Abs(Vector3.Distance(tool.primaryGrip.position,tool.tip.position)-length)<.001f, "grab changes physical tool size");
                    Require(player.GetComponent<PlayerInputProvider>().RightGripConsumed, "grip ownership while held");
                    Require(Vector3.Distance(tool.primaryGrip.position, right.position) < .0001f, "primary grip alignment: " + tool.kind);
                    Require(Quaternion.Angle(tool.primaryGrip.rotation, right.rotation) < .01f, "primary grip orientation");
                    Require(Vector3.Dot(tool.tip.forward, right.forward) > .99f, "tool tip does not aim along controller: " + tool.kind);
                    Require(Vector3.Dot((tool.tip.position - tool.primaryGrip.position).normalized, right.forward) > .7f,
                        "visible tool geometry does not extend forward: " + tool.kind + " delta=" + (tool.tip.position - tool.primaryGrip.position));
                    Require(tool.GetComponent<Rigidbody>().isKinematic && tool.GetComponent<Collider>().isTrigger, "tool collision policy");
                    tool.Operate(1); Require(tool.IsOperating, "trigger");
                    if (tool.kind == HandToolKind.Grinder)
                    {
                        var filter = tool.movingPart.GetComponentInChildren<MeshFilter>();
                        var size = filter.sharedMesh.bounds.size;
                        Vector3 axis = size.x < size.y && size.x < size.z ? Vector3.right : size.y < size.z ? Vector3.up : Vector3.forward;
                        Vector3 normal = filter.transform.TransformDirection(axis).normalized;
                        Vector3 center = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
                        tool.SetRotationPhase(90);
                        Require(Vector3.Dot(normal, filter.transform.TransformDirection(axis).normalized) > .9999f,
                            "disc tilts out of its cutting plane");
                        Require(Vector3.Distance(center, filter.transform.TransformPoint(filter.sharedMesh.bounds.center)) < .0001f,
                            "disc orbits an offset pivot");
                        result.discSpindle = true;
                    }
                    equipment.Release();
                    for(int cycle=0;cycle<8;cycle++)
                    {
                        equipment.Grab(tool);equipment.Release();
                        Require(Mathf.Abs(tool.transform.lossyScale.magnitude-scale)<.001f && Mathf.Abs(Vector3.Distance(tool.primaryGrip.position,tool.tip.position)-length)<.001f, "scale drift after repeated grab/release");
                        Require(tool.gameObject.activeSelf, "selected tool disappears after release");
                    }
                    Require(!player.GetComponent<PlayerInputProvider>().RightGripConsumed, "grip ownership released");
                    Require(!tool.IsOperating && !tool.IsHeld && tool.transform.parent == parent &&
                        Vector3.Distance(tool.transform.localPosition, position) < .0001f && Quaternion.Angle(tool.transform.localRotation, rotation) < .01f, "release to holster");
                }
                result.gripAlignment = result.tipForward = result.returnToHolster = true;
                result.beltMount = true;
                result.constantToolSize = result.oneSelectedThighTool = true;
                equipment.Take(equipment.tools[1]);
                equipment.ProcessInput(true, true, 0, 1, 0);
                Require(equipment.Held == null && equipment.Selected == equipment.tools[1], "cyberdeck must select thigh tool without grabbing");
                right.position=equipment.Selected.primaryGrip.position;
                equipment.ProcessInput(true, true, 1, 1, 0);
                Require(equipment.Held != null && equipment.Held.IsOperating, "selected tool proximity grip");
                equipment.ProcessInput(true, true, 0, 0, 0);
                Require(equipment.Held == null, "recalled tool returns to belt");
                result.cyberdeckRecall = true;
                equipment.SelectTool(equipment.tools[0]);
                Vector3 savedRight = neutralRight;
                right.position = equipment.tools[0].primaryGrip.position;
                equipment.ProcessInput(true, true, 1, 1, 0);
                Require(equipment.Held == equipment.tools[0], "proximity grab");
                equipment.ProcessInput(false, true, 1, 1, 0);
                Require(equipment.Held == null, "tracking loss releases tool");
                equipment.ProcessInput(true, true, 1, 1, 0);
                Require(equipment.Held == null, "tracking recovery must require fresh squeeze");
                equipment.ProcessInput(true, true, 0, 0, 0);
                equipment.ProcessInput(true, true, 1, 1, 0);
                Require(equipment.Held != null, "fresh squeeze after recovery");
                equipment.ProcessInput(true, false, 1, 1, 0);
                Require(equipment.Held == null, "focus loss releases tool");
                right.position = savedRight;
                result.simulatedTrackingSafety = true;
                var deck = equipment.GetComponentInChildren<CyberdeckSurface>();
                Require(deck != null && deck.transform.parent.name == "LeftForeArm" && deck.hudAnchor != null && deck.screen != null, "left wrist mount");
                Require(Mathf.Abs(deck.activeMeters.x / deck.activeMeters.y - 21f/9) < .001f, "screen ratio");
                var screenFilter = deck.screen.GetComponent<MeshFilter>();
                Require(Vector3.Dot(deck.hudAnchor.forward,
                    screenFilter.transform.TransformDirection(screenFilter.sharedMesh.normals[0]).normalized) > .99f, "HUD anchor must face out of the screen");
                result.wristMount = true;
                foreach (var renderer in equipment.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                    Require(material != null && (material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) || material.shader.name.Contains("GUI/Text")), "non-URP equipment material: " + material?.name);
                result.urpMaterials = true;

                equipment.astronaut.UpdatePose(true);
                var bridge = equipment.astronaut.avatar.GetComponents<MonoBehaviour>().First(c => c.GetType().FullName == "VoidForge.Avatars.AstronautFinalIK");
                var component = bridge.GetType().GetProperty("SolverComponent").GetValue(bridge);
                var solver = component.GetType().GetField("solver").GetValue(component);
                solver.GetType().GetMethod("Initiate", new[] { typeof(Transform) }).Invoke(solver, new object[] { equipment.astronaut.avatar });
                for (int i = 0; i < 10; i++) solver.GetType().GetMethod("Update", Type.EmptyTypes).Invoke(solver, null);
                result.rightHandErrorMeters = Vector3.Distance(Bone(equipment.astronaut.avatar, "RightHand").position, equipment.astronaut.rightTarget.position);
                result.headErrorMeters = Vector3.Distance(Bone(equipment.astronaut.avatar, "Head").position, equipment.astronaut.headTarget.position);
                Require(result.rightHandErrorMeters < .03f && result.headErrorMeters < .03f, $"VRIK residual hand={result.rightHandErrorMeters}, head={result.headErrorMeters}");
                result.finalIK = true;
                var wrist = Bone(equipment.astronaut.avatar, "RightHand");
                Vector3 direction = (Bone(equipment.astronaut.avatar, "RMiddleFinger1").position - wrist.position).normalized;
                Vector3 thumb = Bone(equipment.astronaut.avatar, "RThumb1").position - wrist.position;
                Vector3 dorsal = -Vector3.Cross(direction, thumb).normalized;
                Require(Vector3.Dot(direction, right.forward) > .95f && Vector3.Dot(dorsal, right.right) > .95f,
                    "controller grip wrist basis");
                ValidateCurl(equipment.astronaut.avatar);
                result.anatomicalCurl = true;
                var leftWrist = Bone(equipment.astronaut.avatar, "LeftHand");
                Vector3 leftFingers = (Bone(equipment.astronaut.avatar, "LMiddleFinger1").position - leftWrist.position).normalized;
                Vector3 leftThumb = Bone(equipment.astronaut.avatar, "LThumb1").position - leftWrist.position;
                Vector3 palm = -Vector3.Cross(leftFingers, leftThumb).normalized;
                var mount = deck.GetComponent<WristDisplayMount>();
                Require(mount != null, "post-IK wrist mount missing");
                result.screenStandOffMeters = mount.standOff;
                mount.Refresh();
                result.sleeveContactGapMeters = mount.ContactGapMeters;
                Require(mount.sleeveSamples.Length>0 && Mathf.Abs(mount.ContactGapMeters)<=.002f, "cyberdeck back must contact the skinned sleeve within 2 mm");
                palm = Vector3.ProjectOnPlane(palm, (mount.forearm.position - leftWrist.position).normalized).normalized;
                Vector3 screenNormal = screenFilter.transform.TransformDirection(screenFilter.sharedMesh.normals[0]).normalized;
                Require(Vector3.Dot(screenNormal, palm) > .99f, "screen must face the palm side after VRIK");
                Require(Vector3.Dot(deck.screen.bounds.center - leftWrist.position, palm) > .04f, "screen must sit in front of inner sleeve");
                result.palmSideScreen = true;
                left.localRotation = Quaternion.Euler(-30,0,75);
                equipment.astronaut.UpdatePose(true);
                for (int i = 0; i < 10; i++) solver.GetType().GetMethod("Update", Type.EmptyTypes).Invoke(solver, null);
                mount.Refresh();
                Vector3 towardEyes = (camera.position - deck.screen.bounds.center).normalized;
                Require(Vector3.Dot(deck.hudAnchor.forward, towardEyes) > .2f, "screen faces away from eyes in reading pose");
                Require(Vector3.Distance(camera.position, deck.screen.bounds.center) > .12f, "reading screen too close to eyes");
                player.transform.SetPositionAndRotation(new Vector3(2,3,4), Quaternion.Euler(0,90,0));
                equipment.astronaut.UpdatePose(true);
                Require(Vector3.Distance(equipment.astronaut.headTarget.position, camera.TransformPoint(equipment.astronaut.headFromView)) < .0001f, "recenter visor view anchor");
                Require(equipment.astronaut.headFromView.z < -.04f, "tracked view must be outside the front visor");
                result.visorView = true;
                Require(Vector3.Distance(equipment.astronaut.rightTarget.position, right.TransformPoint(equipment.astronaut.wristFromController)) < .0001f, "recenter wrist target");
                result.recenter = true;

                var catalogue = AssetDatabase.LoadAssetAtPath<BlockCatalogue>(IntegrationWorkshop.Output + "/BlockCatalogue.asset");
                result.availableBlocks = catalogue.entries.Count(e => e.Available);
                result.pendingBlocks = catalogue.entries.Count(e => !e.Available);
                Require(result.availableBlocks == 8 && result.pendingBlocks == 3, "catalogue availability");
                var grid = new GameObject("TestGrid").AddComponent<BlockGrid>(); grid.catalogue = catalogue;
                Require(grid.TryPlace("Thruster", Vector3Int.zero, Quaternion.Euler(0,90,0), out _), "rotated multi-cell placement");
                Require(!grid.TryPlace("ArmorCube", Vector3Int.right, Quaternion.identity, out _), "occupied cell accepted");
                Require(!grid.TryPlace("ArmorCube", new Vector3Int(3,0,0), Quaternion.Euler(0,45,0), out _), "off-grid rotation accepted");
                Require(!grid.TryPlace("Refinery", new Vector3Int(3,0,0), Quaternion.identity, out _), "pending block accepted");
                foreach (var entry in catalogue.entries.Where(e => e.Available))
                {
                    Require(entry.prefab.GetComponentsInChildren<Collider>().Length > 0, "missing block collider: " + entry.id);
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(entry.prefab) == 0, "missing block script");
                }
                result.occupiedTestCells = grid.OccupiedCount;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/integration-validation.json", JsonUtility.ToJson(result, true));
                Debug.Log("VF_INTEGRATION_PASS " + JsonUtility.ToJson(result));
            }
            finally
            {
                foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            }
        }

        static void ValidateCurl(Transform avatar)
        {
            var fingers = avatar.GetComponents<MonoBehaviour>().First(c => c.GetType().FullName == "VoidForge.Avatars.AstronautFingerPose");
            fingers.GetType().GetMethod("RestoreRest").Invoke(fingers, null);
            var curl = new[] { (float[])fingers.GetType().GetField("leftCurl").GetValue(fingers),
                (float[])fingers.GetType().GetField("rightCurl").GetValue(fingers) };
            string[] names = { "IndexFinger", "MiddleFinger", "RingFinger", "PinkyFinger" };
            var positions = new Vector3[2,4]; var palm = new Vector3[2];
            for (int side = 0; side < 2; side++)
            {
                string prefix = side == 0 ? "L" : "R";
                var wrist = Bone(avatar, side == 0 ? "LeftHand" : "RightHand");
                palm[side] = Vector3.Cross((Bone(avatar, prefix + "MiddleFinger1").position - wrist.position).normalized,
                    Bone(avatar, prefix + "Thumb1").position - wrist.position).normalized * (side == 0 ? -1 : 1);
                for (int f = 0; f < 4; f++) positions[side,f] = Bone(avatar, prefix + names[f] + "3").position;
                for (int f = 0; f < 5; f++) curl[side][f] = .65f;
            }
            fingers.GetType().GetMethod("ApplyPose").Invoke(fingers, null);
            for (int side = 0; side < 2; side++)
            for (int f = 0; f < 4; f++)
            {
                var bone = Bone(avatar, (side == 0 ? "L" : "R") + names[f] + "3");
                float closure = Vector3.Dot(bone.position - positions[side,f], palm[side]);
                Require(closure > .01f, $"finger must close into palm: {bone.name}, displacement={closure}");
            }
            foreach (var values in curl) Array.Clear(values, 0, values.Length);
            fingers.GetType().GetMethod("RestoreRest").Invoke(fingers, null);
        }

        static void ValidateHandAssets(Transform avatar)
        {
            foreach(var side in new[]{"Left","Right"})
            {
                string prefix=side=="Left"?"L":"R";
                var wrist=Bone(avatar,side+"Hand");
                Vector3 fingers=(Bone(avatar,prefix+"MiddleFinger1").position-wrist.position).normalized;
                Vector3 thumb=Vector3.ProjectOnPlane(Bone(avatar,prefix+"Thumb1").position-wrist.position,fingers);
                Vector3 medial=Vector3.ProjectOnPlane(avatar.position-wrist.position,fingers);
                Require(Vector3.Dot(thumb,medial)<0,side+" glove thumb must point laterally with palms forward at rest");
            }
        }
    }
}
