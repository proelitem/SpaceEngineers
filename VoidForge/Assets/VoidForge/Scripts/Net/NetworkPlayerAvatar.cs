using System;
using Unity.Netcode;
using UnityEngine;
using VoidForge.World;

namespace VoidForge.Net
{
    /// <summary>
    /// Avatar réseau d'un joueur : tête et deux mains. Le propriétaire publie les poses locales
    /// (variable réseau à écriture propriétaire) ; les autres clients les interpolent.
    /// Le propriétaire masque son propre avatar pour ne pas se voir de l'intérieur.
    /// </summary>
    public sealed class NetworkPlayerAvatar : NetworkBehaviour
    {
        /// <summary>Poses tête/mains dans le repère monde.</summary>
        public struct Poses : INetworkSerializable, IEquatable<Poses>
        {
            public Vector3 HeadPos, LeftPos, RightPos;
            public Quaternion HeadRot, LeftRot, RightRot;

            public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
            {
                s.SerializeValue(ref HeadPos);
                s.SerializeValue(ref LeftPos);
                s.SerializeValue(ref RightPos);
                s.SerializeValue(ref HeadRot);
                s.SerializeValue(ref LeftRot);
                s.SerializeValue(ref RightRot);
            }

            public bool Equals(Poses o)
            {
                return HeadPos == o.HeadPos && LeftPos == o.LeftPos && RightPos == o.RightPos
                    && HeadRot == o.HeadRot && LeftRot == o.LeftRot && RightRot == o.RightRot;
            }
        }

        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private float interpolationSpeed = 15f;

        private readonly NetworkVariable<Poses> _poses = new NetworkVariable<Poses>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private Transform _localHead, _localLeft, _localRight;

        /// <summary>Construit les parties visibles de l'avatar (utilisé par l'outil d'éditeur qui crée le prefab).</summary>
        public void BuildVisuals()
        {
            head = CreatePart("Head", 0.2f);
            leftHand = CreatePart("LeftHand", 0.07f);
            rightHand = CreatePart("RightHand", 0.07f);
        }

        private Transform CreatePart(string name, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Avatar;
            return go.transform;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    _localHead = cam.transform;
                    var offset = cam.transform.parent;
                    if (offset != null)
                    {
                        _localLeft = offset.Find("LeftHand");
                        _localRight = offset.Find("RightHand");
                    }
                }

                // Le joueur ne voit pas son propre avatar.
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            }
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner)
            {
                if (_localHead == null) return;
                _poses.Value = new Poses
                {
                    HeadPos = _localHead.position,
                    HeadRot = _localHead.rotation,
                    LeftPos = _localLeft != null ? _localLeft.position : _localHead.position,
                    LeftRot = _localLeft != null ? _localLeft.rotation : _localHead.rotation,
                    RightPos = _localRight != null ? _localRight.position : _localHead.position,
                    RightRot = _localRight != null ? _localRight.rotation : _localHead.rotation,
                };
            }
            else
            {
                var p = _poses.Value;
                float t = 1f - Mathf.Exp(-interpolationSpeed * Time.deltaTime);
                Apply(head, p.HeadPos, p.HeadRot, t);
                Apply(leftHand, p.LeftPos, p.LeftRot, t);
                Apply(rightHand, p.RightPos, p.RightRot, t);
            }
        }

        private static void Apply(Transform target, Vector3 pos, Quaternion rot, float t)
        {
            if (target == null) return;
            target.position = Vector3.Lerp(target.position, pos, t);
            target.rotation = Quaternion.Slerp(target.rotation, rot, t);
        }
    }
}
