#if VF_LOCAL_DIGGER
using System;
using System.Threading.Tasks;
using Digger.Modules.Core.Sources;
using Digger.Modules.Runtime.Sources;
using Unity.Netcode;
using UnityEngine;
using VoidForge.Player;

namespace VoidForge.World
{
    /// <summary>Zone d'essai Digger locale : une excavation asynchrone à la fois, gachette droite.</summary>
    public sealed class MiningPrototype : MonoBehaviour
    {
        public Terrain terrain;
        public DiggerMasterRuntime digger;
        public TextMesh label;
        private PlayerInputProvider _input;
        private Transform _hand;
        private LineRenderer _beam;
        private bool _busy;
        private bool _testing;
        private float _nextDig;
        private int _edits;

        public Vector3 RockCenter => new Vector3(0, -1, 6);
        public const float RockRadius = 2.5f;
        public Vector3 ProbePoint => RockCenter + Vector3.up * RockRadius;

        private async void Start()
        {
            _input = FindFirstObjectByType<PlayerInputProvider>();
            if (_input != null)
                foreach (var child in _input.GetComponentsInChildren<Transform>())
                    if (child.name == "RightHand") { _hand = child; break; }
            var beam = new GameObject("MiningBeam");
            beam.transform.SetParent(transform, false);
            _beam = beam.AddComponent<LineRenderer>();
            _beam.positionCount = 2;
            _beam.startWidth = 0.012f;
            _beam.endWidth = 0.006f;
            _beam.sharedMaterial = MaterialLibrary.Star;
            _beam.enabled = false;
            UpdateLabel();
            _testing = true;
            try
            {
                await digger.ModifyAsync(RockCenter, BrushType.Sphere, ActionType.Add, 0, 1f, RockRadius);
                HideSupportTerrain();
                Debug.Log("[VoidForge Mining] Volume voxel isolé créé avec Digger Add.");
                if (Debug.isDebugBuild) await ValidateExcavation();
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { _testing = false; UpdateLabel(); }
        }

        private void Update()
        {
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (_hand == null || _input == null || _input.Actions == null || _testing || networked)
            {
                if (_beam != null) _beam.enabled = false;
                if (networked && label != null) label.text = "ZONE VOXEL — prototype solo\nQuitter le réseau pour creuser";
                return;
            }
            var ray = new Ray(_hand.position, _hand.forward);
            bool hitZone = Physics.Raycast(ray, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.transform.IsChildOf(transform);
            _beam.enabled = hitZone;
            if (hitZone)
            {
                _beam.SetPosition(0, ray.origin);
                _beam.SetPosition(1, hit.point);
            }
            if (hitZone && !_busy && !digger.IsRunningAsync && Time.unscaledTime >= _nextDig &&
                _input.Actions.RightTrigger.ReadValue<float>() > 0.55f)
            {
                _nextDig = Time.unscaledTime + 0.25f;
                _ = DigAt(hit.point - hit.normal * 0.15f);
            }
        }

        public async Task DigAt(Vector3 point)
        {
            if (_busy || digger.IsRunningAsync) return;
            _busy = true;
            try
            {
                double start = Time.realtimeSinceStartupAsDouble;
                await digger.ModifyAsync(point, BrushType.Sphere, ActionType.Dig, 0, 1f, 0.75f);
                HideSupportTerrain();
                _edits++;
                Debug.Log($"[VoidForge Mining] Excavation {_edits}, {(Time.realtimeSinceStartupAsDouble - start) * 1000:0} ms.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally { _busy = false; UpdateLabel(); }
        }

        public async Task ValidateExcavation()
        {
            Physics.SyncTransforms();
            Vector3 origin = ProbePoint + Vector3.up * 2f;
            if (!Physics.Raycast(origin, Vector3.down, out var before, 12f) ||
                !before.collider.transform.IsChildOf(transform))
                throw new InvalidOperationException("Digger self-test : surface initiale introuvable.");
            await DigAt(before.point - Vector3.up * 0.15f);
            for (int i = 0; i < 10; i++)
            {
                await Task.Yield();
                Physics.SyncTransforms();
                if (Physics.Raycast(origin, Vector3.down, out var after, 12f) &&
                    after.collider.transform.IsChildOf(transform) && after.distance > before.distance + 0.05f)
                {
                    Debug.Log($"[VoidForge Mining] SELFTEST OK : collider excavé de {after.distance - before.distance:0.000} m ; {_edits} opération.");
                    return;
                }
            }
            throw new InvalidOperationException("Digger self-test : le collider ne reflète pas l'excavation.");
        }

        private void UpdateLabel()
        {
            if (label != null) label.text = $"ASTÉROÏDE CREUSABLE\nGâchette droite : creuser (8 m)\nPrototype solo / {_edits} excavation(s)\nRedémarrer pour réinitialiser";
        }

        public void HideSupportTerrain()
        {
            terrain.drawHeightmap = false;
            terrain.GetComponent<TerrainCollider>().enabled = false;
        }
    }
}
#endif
