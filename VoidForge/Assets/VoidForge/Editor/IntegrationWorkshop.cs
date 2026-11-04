using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VoidForge.Integration;
using Object = UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class IntegrationWorkshop
    {
        const string Kit = "Assets/VoidForge/Art/Kit";
        const string Local = "Assets/ThirdParty/VoidForgeIntegration/Generated";
        public const string Output = "Assets/Resources/VoidForge/Integration";
        public const string AvatarPath = "Assets/VoidForge/Prefabs/Avatars/VoidForgeAstronaut.prefab";
        [Serializable] class MaterialSpec { public string name, texture; public float[] color; public float metallic, roughness; public bool unlit; }
        [Serializable] class KitSpec { public string id; public MaterialSpec[] materials; }
        [Serializable] class KitReport { public KitSpec[] assets; }
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("VoidForge/4. Intégrer avatar, poignet, outils et blocs")]
        public static void BuildBatch()
        {
            if (!File.Exists(Kit + "/kit-materials.json") || !File.Exists(AvatarPath))
                throw new InvalidOperationException("Stage the delivered kit and astronaut first; see docs/INTEGRATION.md.");
            Ensure(Output); Ensure(Output + "/Blocks"); Ensure(Local);
            Ensure("Assets/VoidForge/Materials/Kit");
            Ensure("Assets/VoidForge/Materials/HandTools");
            Materials.Clear();
            var report = JsonUtility.FromJson<KitReport>(File.ReadAllText(Kit + "/kit-materials.json"));
            foreach (var asset in report.assets)
            foreach (var spec in asset.materials) MakeMaterial(spec, "Assets/VoidForge/Materials/Kit");
            MakeHandMaterials();
            BuildEquipment();
            BuildCatalogue();
            AssetDatabase.SaveAssets();
            ValidateBatch();
            Debug.Log("[VoidForge Integration] Equipment prefab and eight block prefabs generated.");
        }

        static void Ensure(string path)
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        static Material MakeMaterial(MaterialSpec spec, string folder)
        {
            string path = folder + "/" + spec.name.Replace(':', '_') + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(spec.unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP shader missing.");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(spec.color[0], spec.color[1], spec.color[2], spec.color[3]));
            if (!string.IsNullOrEmpty(spec.texture))
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/" + spec.texture));
            if (!spec.unlit)
            {
                material.SetFloat("_Metallic", spec.metallic);
                material.SetFloat("_Smoothness", 1 - spec.roughness);
            }
            EditorUtility.SetDirty(material);
            Materials[spec.name] = material;
            return material;
        }

        static void MakeHandMaterials()
        {
            string[] names = { "Ceramic", "Graphite", "Steel", "Safety" };
            float[][] colors = { new[]{ .66f,.68f,.63f,1 }, new[]{ .023f,.029f,.033f,1 },
                new[]{ .36f,.42f,.46f,1 }, new[]{ .9f,.43f,.035f,1 } };
            float[] metallic = { .28f,.15f,.9f,.25f }, roughness = { .30f,.48f,.23f,.31f };
            for (int i = 0; i < names.Length; i++) MakeMaterial(new MaterialSpec { name = "VF_HT_" + names[i],
                color = colors[i], metallic = metallic[i], roughness = roughness[i] }, "Assets/VoidForge/Materials/HandTools");
        }

        static GameObject Model(string path, Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new FileNotFoundException(path);
            var go = Object.Instantiate(source, parent);
            go.name = source.name;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string name = materials[i] != null ? materials[i].name : "";
                    if (Materials.TryGetValue(name, out var replacement)) materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
            }
            return go;
        }

        static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        static Transform Suffix(Transform root, string suffix) => root.GetComponentsInChildren<Transform>(true).First(t => t.name.EndsWith(suffix, StringComparison.Ordinal));

        static void BuildEquipment()
        {
            var root = new GameObject("LocalEquipment");
            root.SetActive(false);
            try
            {
                var equipment = root.AddComponent<EquipmentRig>();
                var avatar = Model(AvatarPath, root.transform);
                ApplyAnatomicalHands(avatar);
                var localAvatar = root.AddComponent<LocalAstronaut>();
                equipment.astronaut = localAvatar;
                localAvatar.avatar = avatar.transform;
                localAvatar.headTarget = Find(avatar.transform, "HeadTarget");
                localAvatar.leftTarget = Find(avatar.transform, "LeftHandTarget");
                localAvatar.rightTarget = Find(avatar.transform, "RightHandTarget");
                var head = Find(avatar.transform, "Head");
                Vector3 view = VisorView(avatar, head);
                localAvatar.restHead = avatar.transform.InverseTransformPoint(view);
                localAvatar.headFromView = Quaternion.Inverse(avatar.transform.rotation) * (head.position - view);
                localAvatar.headRotation = Quaternion.Inverse(avatar.transform.rotation) * head.rotation;
                localAvatar.leftRotation = WristRotation(avatar.transform, true);
                localAvatar.rightRotation = WristRotation(avatar.transform, false);
                ConfigureFingerCurl(avatar);
                RemoveLocalHead(avatar);
                var deck = Model(Kit + "/Cyberdeck.fbx", null);
                var wrist = Find(avatar.transform, "LeftHand");
                var forearm = Find(avatar.transform, "LeftForeArm");
                Vector3 along = (forearm.position - wrist.position).normalized;
                Vector3 fingers = (Find(avatar.transform, "LMiddleFinger1").position - wrist.position).normalized;
                Vector3 thumb = Find(avatar.transform, "LThumb1").position - wrist.position;
                Vector3 dorsal = Vector3.Cross(fingers, thumb).normalized;
                var screenTransform = Suffix(deck.transform, "ScreenBlank");
                var screenMesh = screenTransform.GetComponent<MeshFilter>().sharedMesh;
                Vector3 normal = screenTransform.TransformDirection(screenMesh.normals[0]).normalized;
                Vector3 horizontal = ScreenHorizontal(screenTransform, screenMesh);
                var nativeFrame = Quaternion.LookRotation(normal, Vector3.Cross(normal, horizontal));
                Vector3 facing = -dorsal; // Inside of the forearm, on the palm side.
                var desiredFrame = Quaternion.LookRotation(facing, Vector3.Cross(facing, along));
                deck.transform.rotation = desiredFrame * Quaternion.Inverse(nativeFrame) * deck.transform.rotation;
                var sleeveSamples = SleeveSamples(avatar, forearm, wrist.position + along * .10f, along);
                float sleeve = ForearmStandOff(sleeveSamples, wrist.position + along * .10f, along, facing);
                float back = DeckBackProjection(deck, facing);
                float depth = Vector3.Dot(screenTransform.GetComponent<Renderer>().bounds.center, facing) - back;
                float standOff = sleeve + depth - .001f;
                deck.transform.position += wrist.position + along * .10f + facing * standOff - screenTransform.GetComponent<Renderer>().bounds.center;
                Debug.Log($"[VoidForge Integration] Cyberdeck screen stand-off: {standOff:0.000} m from forearm axis.");
                if (Vector3.Dot(screenTransform.TransformDirection(screenMesh.normals[0]).normalized, facing) < .99f)
                    throw new InvalidOperationException("Cyberdeck screen must face the palm side of the forearm.");
                deck.transform.SetParent(forearm, true);
                var surface = deck.AddComponent<CyberdeckSurface>();
                surface.hudAnchor = Suffix(deck.transform, "HUDAnchor");
                surface.hudAnchor.position=screenTransform.GetComponent<Renderer>().bounds.center;
                Vector3 screenNormal = screenTransform.TransformDirection(screenMesh.normals[0]).normalized;
                surface.hudAnchor.rotation = Quaternion.LookRotation(screenNormal,
                    Vector3.Cross(screenNormal, ScreenHorizontal(screenTransform, screenMesh)));
                surface.screen = Suffix(deck.transform, "ScreenBlank").GetComponent<Renderer>();
                var mount = deck.AddComponent<WristDisplayMount>();
                mount.surface = surface; mount.wrist = wrist; mount.forearm = forearm;
                mount.middle = Find(avatar.transform, "LMiddleFinger1"); mount.thumb = Find(avatar.transform, "LThumb1");
                mount.standOff = standOff;
                mount.housingDepth = depth;
                mount.sleeveSamples = sleeveSamples;
                mount.Refresh();
                var seat=new GameObject("Conforming housing • no straps",typeof(MeshFilter),typeof(MeshRenderer));seat.transform.SetParent(deck.transform,false);
                seat.GetComponent<MeshRenderer>().sharedMaterial=deck.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).First(m=>m!=null&&m.name.Contains("Graphite"));
                seat.AddComponent<WristHousingSeat>().Configure(mount);
                string seatPath=Local+"/WristHousingSeat.asset";var seatMesh=seat.GetComponent<MeshFilter>().sharedMesh;var existingSeat=AssetDatabase.LoadAssetAtPath<Mesh>(seatPath);
                if(existingSeat==null)AssetDatabase.CreateAsset(seatMesh,seatPath);else{EditorUtility.CopySerialized(seatMesh,existingSeat);Object.DestroyImmediate(seatMesh);seat.GetComponent<MeshFilter>().sharedMesh=existingSeat;}

                string[] ids = { "MiningDrill", "Grinder", "Welder" };
                equipment.tools = new HandTool[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    var holster = new GameObject(ids[i] + "Holster").transform;
                    holster.SetParent(root.transform, false);
                    var follower = holster.gameObject.AddComponent<ThighToolHolster>();
                    follower.avatar = avatar.transform;
                    follower.thigh = Find(avatar.transform, "RightUpLeg");
                    follower.Refresh();
                    var toolRoot = new GameObject(ids[i]);
                    toolRoot.transform.SetParent(root.transform, false);
                    var model = Model($"Assets/VoidForge/Art/HandTools/{ids[i]}/VF_{ids[i]}.fbx", toolRoot.transform);
                    var tool = toolRoot.AddComponent<HandTool>();
                    tool.kind = (HandToolKind)i;
                    tool.movingPart = model.GetComponentsInChildren<Transform>().First(t => t.name.Contains(i == 0 ? "Rotor" : i == 1 ? "Disc" : "Nozzle"));
                    var sourceGrip = Suffix(model.transform, "GripPrimary");
                    var sourceTip = Suffix(model.transform, "ToolTip");
                    // FBX root/empty axes are not Unity's tool-forward axes.
                    // Derive the physical forward direction from the mobile head to the tip.
                    Vector3 physicalForward = (sourceTip.position - tool.movingPart.position).normalized;
                    var gripFrame = Quaternion.LookRotation(physicalForward, sourceGrip.forward);
                    tool.primaryGrip = ToolFrame(toolRoot.transform, sourceGrip, gripFrame, "CalibratedGrip");
                    tool.tip = ToolFrame(toolRoot.transform, sourceTip, gripFrame, "CalibratedTip");
                    tool.spinAxisLocal = tool.movingPart.InverseTransformDirection(physicalForward);
                    if (tool.kind == HandToolKind.Grinder) ConfigureDisc(tool);
                    var bounds = BoundsOf(model);
                    var collider = toolRoot.AddComponent<BoxCollider>();
                    collider.center = toolRoot.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size; collider.isTrigger = true;
                    var body = toolRoot.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                    // Seat the handle at belt height; keep the working end pointing down, clear of the torso.
                    toolRoot.transform.rotation = holster.rotation * Quaternion.Inverse(tool.primaryGrip.rotation);
                    toolRoot.transform.position += holster.position - tool.primaryGrip.position;
                    toolRoot.transform.SetParent(holster, true);
                    equipment.tools[i] = tool;
                }
                equipment.instructions = null; // Interaction guidance now belongs to the Cyberdeck.
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, Output + "/Equipment.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void ConfigureDisc(HandTool tool)
        {
            var mesh = tool.movingPart.GetComponent<MeshFilter>().sharedMesh;
            var size = mesh.bounds.size;
            Vector3 axis = size.x < size.y && size.x < size.z ? Vector3.right :
                size.y < size.z ? Vector3.up : Vector3.forward;
            // The delivered assembly pivot is offset from the disc centre. Centre the spindle,
            // preserving the geometry and the FBX; rotating around its old origin makes it orbit.
            var spindle = new GameObject("DiscSpindle").transform;
            spindle.SetParent(tool.movingPart.parent, false);
            spindle.SetPositionAndRotation(tool.movingPart.TransformPoint(mesh.bounds.center), tool.movingPart.rotation);
            tool.movingPart.SetParent(spindle, true);
            tool.movingPart = spindle;
            tool.spinAxisLocal = axis;
        }

        static void ConfigureFingerCurl(GameObject avatar)
        {
            // Rebind the corrected rest skeleton using the producer's original flexion rule.
            // Chirality is corrected in the asset, not by reversing the curl controls.
            var component = avatar.GetComponents<MonoBehaviour>().First(c => c.GetType().FullName == "VoidForge.Avatars.AstronautFingerPose");
            var serialized = new SerializedObject(component);
            var joints = serialized.FindProperty("joints");
            if (joints == null) throw new InvalidOperationException("Finger bindings missing.");
            joints.ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            component.GetType().GetMethod("Rebuild").Invoke(component, null);
        }

        static void ApplyAnatomicalHands(GameObject avatar)
        {
            const string path = "Assets/ThirdParty/VoidForgeIntegration/CorrectedAstronaut/Astronaut_AnatomicalHands.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Run Build/correct-astronaut-hands.ps1 first; original opposite-handed gloves must not be reused.");
            var bones = avatar.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name);
            foreach (var t in source.GetComponentsInChildren<Transform>(true))
                if (bones.TryGetValue(t.name, out var target) && (t.name.StartsWith("L") || t.name.StartsWith("R")) && t.name.Any(char.IsDigit))
                { target.localPosition = t.localPosition; target.localRotation = t.localRotation; target.localScale = t.localScale; }
            foreach (var original in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var target = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == original.name);
                target.sharedMesh = original.sharedMesh;
                target.bones = original.bones.Select(b => bones[b.name]).ToArray();
                target.rootBone = bones[original.rootBone.name];
            }
        }

        static Vector3 VisorView(GameObject avatar, Transform head)
        {
            Vector3 view = head.position + avatar.transform.up * .09f;
            float front = Vector3.Dot(view, avatar.transform.forward);
            foreach (var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); renderer.BakeMesh(mesh, true);
                foreach (var vertex in mesh.vertices)
                {
                    Vector3 p = renderer.transform.TransformPoint(vertex);
                    if (Mathf.Abs(p.y - view.y) < .035f && Mathf.Abs(Vector3.Dot(p - head.position, avatar.transform.right)) < .10f)
                        front = Mathf.Max(front, Vector3.Dot(p, avatar.transform.forward));
                }
                Object.DestroyImmediate(mesh);
            }
            view += avatar.transform.forward * (front - Vector3.Dot(view, avatar.transform.forward) + .005f);
            return view;
        }

        static float DeckBackProjection(GameObject deck, Vector3 normal)
        {
            float back = float.PositiveInfinity;
            foreach (var filter in deck.GetComponentsInChildren<MeshFilter>())
            foreach (var p in filter.sharedMesh.vertices)
                back = Mathf.Min(back, Vector3.Dot(filter.transform.TransformPoint(p), normal));
            return back;
        }

        static Quaternion WristRotation(Transform avatar, bool left)
        {
            string prefix = left ? "L" : "R";
            var wrist = Find(avatar, left ? "LeftHand" : "RightHand");
            Vector3 fingers = (Find(avatar, prefix + "MiddleFinger1").position - wrist.position).normalized;
            Vector3 thumb = Find(avatar, prefix + "Thumb1").position - wrist.position;
            Vector3 dorsal = Vector3.Cross(fingers, thumb).normalized * (left ? 1 : -1);
            return Quaternion.LookRotation(Vector3.forward, left ? Vector3.left : Vector3.right) *
                Quaternion.Inverse(Quaternion.LookRotation(fingers, dorsal)) * wrist.rotation;
        }

        static Transform ToolFrame(Transform root, Transform source, Quaternion orientation, string name)
        {
            var frame = new GameObject(name).transform;
            frame.SetParent(root, false);
            frame.SetPositionAndRotation(source.position, orientation);
            return frame;
        }

        static Vector3 ScreenHorizontal(Transform screen, Mesh mesh)
        {
            var vertices = mesh.vertices; var uv = mesh.uv;
            Vector3 low = Vector3.zero, high = Vector3.zero; int lows = 0, highs = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (uv[i].x < .01f) { low += vertices[i]; lows++; }
                if (uv[i].x > .99f) { high += vertices[i]; highs++; }
            }
            if (lows == 0 || highs == 0) throw new InvalidOperationException("Screen UV width is incomplete.");
            return screen.TransformDirection(high / highs - low / lows).normalized;
        }

        static float ForearmStandOff(WristDisplayMount.SleeveSample[] samples, Vector3 center, Vector3 along, Vector3 facing)
        {
            float height = 0;
            int matches=0;
            foreach (var sample in samples)
            {
                Vector3 delta=sample.WorldPosition-center;
                if(Mathf.Abs(Vector3.Dot(delta,along))>=.07f||Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(along,facing)))>=.055f)continue;
                matches++;height=Mathf.Max(height,Vector3.Dot(delta,facing));
            }
            if (height <= 0) throw new InvalidOperationException($"Sleeve contact surface missing: {samples.Length} probes, {matches} in patch; centre={center}, normal={facing}.");
            return height;
        }

        static WristDisplayMount.SleeveSample[] SleeveSamples(GameObject avatar, Transform forearm, Vector3 centre, Vector3 along)
        {
            var samples = new List<WristDisplayMount.SleeveSample>();
            foreach (var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;var bones=renderer.bones;
                var sleeve=bones.Select(b=>b==forearm||b!=null&&b.IsChildOf(forearm)).ToArray();
                var candidates=new WristDisplayMount.SleeveSample[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];
                    float influence=(sleeve[w.boneIndex0]?w.weight0:0)+(sleeve[w.boneIndex1]?w.weight1:0)+(sleeve[w.boneIndex2]?w.weight2:0)+(sleeve[w.boneIndex3]?w.weight3:0);
                    if(influence<.25f)continue;
                    var sample=new WristDisplayMount.SleeveSample { a=bones[w.boneIndex0],b=bones[w.boneIndex1],c=bones[w.boneIndex2],d=bones[w.boneIndex3],
                        pa=bind[w.boneIndex0].MultiplyPoint3x4(vertices[i]),pb=bind[w.boneIndex1].MultiplyPoint3x4(vertices[i]),
                        pc=bind[w.boneIndex2].MultiplyPoint3x4(vertices[i]),pd=bind[w.boneIndex3].MultiplyPoint3x4(vertices[i]),
                        weights=new Vector4(w.weight0,w.weight1,w.weight2,w.weight3) };
                    Vector3 delta=sample.WorldPosition-centre;
                    if(delta.sqrMagnitude<.16f)
                    {
                        candidates[i]=sample;
                        if(Mathf.Abs(Vector3.Dot(delta,along))<.12f)samples.Add(sample);
                    }
                }
                // Probe face interiors too: low-poly sleeve rings can straddle the entire
                // mounting patch without placing a single vertex inside it.
                var triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=candidates[triangles[i]];var b=candidates[triangles[i+1]];var c=candidates[triangles[i+2]];
                    if(a==null||b==null||c==null)continue;
                    Vector3 delta=(a.WorldPosition+b.WorldPosition+c.WorldPosition)/3-centre;
                    if(delta.sqrMagnitude<.09f&&Mathf.Abs(Vector3.Dot(delta,along))<.12f)samples.Add(MergeSkinPoints(a,b,c));
                }
            }
            if(samples.Count==0)throw new InvalidOperationException("Live sleeve contact samples missing.");
            return samples.ToArray();
        }

        static WristDisplayMount.SleeveSample MergeSkinPoints(params WristDisplayMount.SleeveSample[] samples)
        {
            var contributions=new Dictionary<Transform,(Vector3 point,float weight)>();
            void Add(Transform bone,Vector3 point,float weight)
            {
                if(weight<=0)return;
                contributions.TryGetValue(bone,out var previous);
                contributions[bone]=(previous.point+point*weight,previous.weight+weight);
            }
            foreach(var sample in samples)
            {
                Add(sample.a,sample.pa,sample.weights.x/3);Add(sample.b,sample.pb,sample.weights.y/3);
                Add(sample.c,sample.pc,sample.weights.z/3);Add(sample.d,sample.pd,sample.weights.w/3);
            }
            var sorted=contributions.OrderByDescending(p=>p.Value.weight).Take(4).ToArray();
            float total=sorted.Sum(p=>p.Value.weight);
            var bones=new Transform[4];var points=new Vector3[4];var weights=Vector4.zero;
            for(int i=0;i<4;i++)
            {
                bones[i]=i<sorted.Length?sorted[i].Key:sorted[0].Key;
                if(i>=sorted.Length)continue;
                points[i]=sorted[i].Value.point/sorted[i].Value.weight;weights[i]=sorted[i].Value.weight/total;
            }
            return new WristDisplayMount.SleeveSample {a=bones[0],b=bones[1],c=bones[2],d=bones[3],pa=points[0],pb=points[1],pc=points[2],pd=points[3],weights=weights};
        }

        static void RemoveLocalHead(GameObject avatar)
        {
            int removed = 0;
            foreach (var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var weights = renderer.sharedMesh.boneWeights;
                var headBones = renderer.bones.Select(b => {
                    while (b != null) { if (b.name == "Head") return true; b = b.parent; } return false;
                }).ToArray();
                bool IsHead(int vertex)
                {
                    var w = weights[vertex];
                    return (headBones[w.boneIndex0] ? w.weight0 : 0) + (headBones[w.boneIndex1] ? w.weight1 : 0) +
                        (headBones[w.boneIndex2] ? w.weight2 : 0) + (headBones[w.boneIndex3] ? w.weight3 : 0) > .25f;
                }
                var mesh = Object.Instantiate(renderer.sharedMesh);
                int count = 0;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var triangles = mesh.GetTriangles(s); var kept = new List<int>();
                    for (int i = 0; i < triangles.Length; i += 3)
                        if (IsHead(triangles[i]) || IsHead(triangles[i+1]) || IsHead(triangles[i+2])) count++;
                        else { kept.Add(triangles[i]); kept.Add(triangles[i+1]); kept.Add(triangles[i+2]); }
                    mesh.SetTriangles(kept, s);
                }
                if (count == 0) { Object.DestroyImmediate(mesh); continue; }
                string path = Local + "/" + renderer.name + "_LocalHeadless.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
                else { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(mesh); }
                renderer.sharedMesh = existing;
                removed += count;
            }
            if (removed == 0) throw new InvalidOperationException("Local head mask did not remove helmet triangles.");
            Debug.Log("[VoidForge Integration] Local helmet/head mask removed " + removed + " triangles; source avatar unchanged.");
        }

        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Model without renderer: " + go.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static TextMesh MakeText(GameObject go, string text, float size)
        {
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 64; mesh.characterSize = size; mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center; mesh.color = new Color(.8f,.9f,1);
            go.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            return mesh;
        }

        static void BuildCatalogue()
        {
            string[] ids = { "ArmorCube", "ArmorSlope", "ArmorCorner", "Gyroscope", "Battery", "Thruster", "Cockpit", "DrillR", "Refinery", "Assembler", "Cargo" };
            var entries = new List<BlockCatalogue.Entry>();
            foreach (var id in ids)
            {
                var entry = new BlockCatalogue.Entry { id = id, size = id == "Cockpit" ? new Vector3Int(2,2,3) :
                    id == "Thruster" ? new Vector3Int(1,1,2) : id == "DrillR" ? new Vector3Int(2,2,4) : Vector3Int.one };
                if (id == "Refinery" || id == "Assembler" || id == "Cargo") { entries.Add(entry); continue; }
                var root = new GameObject(id);
                try
                {
                    var model = Model(id == "DrillR" ? "Assets/ThirdParty/ToolAssets/Ship/DRILL-R/Drill-R.prefab" : Kit + "/" + id + ".fbx", root.transform);
                    foreach (var collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                    var bounds = BoundsOf(model);
                    Vector3 targetCenter = ((Vector3)entry.size - Vector3.one) * .5f;
                    model.transform.position += targetCenter - bounds.center;
                    bounds = BoundsOf(model);
                    if (id != "DrillR" && (bounds.size - (Vector3)entry.size).sqrMagnitude > .00001f)
                        throw new InvalidOperationException($"{id} grid envelope mismatch: {bounds.size} vs {entry.size}");
                    if (id.StartsWith("Armor", StringComparison.Ordinal))
                    {
                        foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                        { var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; collider.convex = true; }
                    }
                    else { var collider = root.AddComponent<BoxCollider>(); collider.center = targetCenter; collider.size = entry.size; }
                    entry.prefab = PrefabUtility.SaveAsPrefabAsset(root, Output + "/Blocks/" + id + ".prefab");
                }
                finally { Object.DestroyImmediate(root); }
                entries.Add(entry);
            }
            string path = Output + "/BlockCatalogue.asset";
            var catalogue = AssetDatabase.LoadAssetAtPath<BlockCatalogue>(path);
            if (catalogue == null) { catalogue = ScriptableObject.CreateInstance<BlockCatalogue>(); AssetDatabase.CreateAsset(catalogue, path); }
            catalogue.entries = entries.ToArray(); EditorUtility.SetDirty(catalogue);
        }

        public static void ValidateBatch() => IntegrationValidation.Run();

        public static void BuildQuestBatch()
        {
            BuildBatch();
            QuestBuildPipeline.BuildBatch();
        }
    }
}
