using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidForge.Integration
{
    // Preserve the concurrently delivered HUD API and assets. Reflection is cached at bind.
    public sealed class CyberdeckEquipmentBridge : MonoBehaviour
    {
        EquipmentRig equipment;
        MonoBehaviour session;
        EventInfo equipEvent;
        FieldInfo targetId, waypoints;
        MethodInfo notify;
        Action<int> select;
        GpsTargetGuide guide;
        float nextBind;
        string currentTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SceneManager.GetActiveScene().name == "Main" && FindFirstObjectByType<CyberdeckEquipmentBridge>() == null)
                new GameObject("Thigh equipment and GPS binding").AddComponent<CyberdeckEquipmentBridge>();
        }

        void Update()
        {
            if (session == null || equipment == null)
            {
                if (Time.unscaledTime < nextBind) return;
                nextBind = Time.unscaledTime + .5f;
                equipment = FindFirstObjectByType<EquipmentRig>();
                foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (behaviour.GetType().FullName == "VoidForge.UI.Cyberdeck.CyberdeckSession") { Bind(behaviour); break; }
                return;
            }
            string id = (string)targetId.GetValue(session);
            if (id == currentTarget) return;
            currentTarget = id;
            guide.hasTarget = false;
            if (string.IsNullOrEmpty(id)) return;
            foreach (object point in (IEnumerable)waypoints.GetValue(session))
            {
                var type = point.GetType();
                if ((string)type.GetField("id").GetValue(point) != id) continue;
                guide.target = (Transform)type.GetField("target").GetValue(point);
                guide.targetPosition = (Vector3)type.GetProperty("Position").GetValue(point);
                guide.hasTarget = true;
                break;
            }
        }

        void Bind(MonoBehaviour candidate)
        {
            if (session != null || equipment == null || !equipment.Ready) return;
            session = candidate;
            var type = session.GetType();
            targetId = type.GetField("targetedWaypoint");
            waypoints = type.GetField("waypoints");
            notify = type.GetMethod("Notify");
            equipEvent = type.GetEvent("EquipRequested");
            select = Select;
            equipEvent.AddEventHandler(session, select);
            guide = gameObject.AddComponent<GpsTargetGuide>();
            Debug.Log("[VoidForge Integration] Cyberdeck selection stocks one right-thigh tool; GPS target guide ready.");
        }

        void Select(int index)
        {
            if (index < 0 || index > 2) return;
            var kinds = new[] { HandToolKind.MiningDrill, HandToolKind.Welder, HandToolKind.Grinder };
            var tool = Array.Find(equipment.tools, t => t.kind == kinds[index]);
            if (tool != null && equipment.SelectTool(tool))
                notify.Invoke(session, new object[] { "OUTIL À LA CUISSE DROITE • APPROCHER LA MAIN ET SERRER LE GRIP" });
        }
        void OnDestroy() { if (session != null && equipEvent != null) equipEvent.RemoveEventHandler(session, select); }
    }
}
