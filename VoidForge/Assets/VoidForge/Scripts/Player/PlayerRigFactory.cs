using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using VoidForge.UI;

namespace VoidForge.Player
{
    /// <summary>
    /// Construit le rig du joueur entièrement en code, pour que la scène soit reproductible
    /// sans prefab fait à la main. Utilisé par l'outil d'éditeur (scène sauvegardée) et par le
    /// bootstrap runtime (secours si la scène ne contient pas de rig).
    ///
    /// Hiérarchie :
    ///   PlayerRig (Rigidbody, SphereCollider, PlayerInputProvider, ZeroGLocomotion, SnapTurn)
    ///     XR Origin (XROrigin, mode de suivi « Device » : la tête est à l'origine du rig)
    ///       Camera Offset
    ///         Main Camera (Camera, AudioListener, TrackedPoseDriver)
    ///           DebugHud
    ///         LeftHand (TrackedPoseDriver, HandVisual)
    ///         RightHand (TrackedPoseDriver, HandVisual)
    /// </summary>
    public static class PlayerRigFactory
    {
        public const string RigName = "PlayerRig";

        public static GameObject Create(Vector3 position)
        {
            var rig = new GameObject(RigName);
            rig.SetActive(false); // on configure tout avant d'activer, pour que OnEnable voie les bonnes valeurs
            rig.transform.position = position;

            var body = rig.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 80f;

            var collider = rig.AddComponent<SphereCollider>();
            collider.radius = 0.35f;

            rig.AddComponent<PlayerInputProvider>();

            // XR Origin
            var originGo = new GameObject("XR Origin");
            originGo.transform.SetParent(rig.transform, false);
            var offsetGo = new GameObject("Camera Offset");
            offsetGo.transform.SetParent(originGo.transform, false);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(offsetGo.transform, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.005f, 0.01f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 3000f;
            cameraGo.AddComponent<AudioListener>();
            AddTrackedPose(cameraGo, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");

            var origin = originGo.AddComponent<XROrigin>();
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offsetGo;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            origin.CameraYOffset = 0f;

            var left = CreateHand("LeftHand", offsetGo.transform, "LeftHand");
            var right = CreateHand("RightHand", offsetGo.transform, "RightHand");

            var locomotion = rig.AddComponent<ZeroGLocomotion>();
            locomotion.Configure(cameraGo.transform, left.transform);

            var snap = rig.AddComponent<SnapTurn>();
            snap.Configure(cameraGo.transform);

            var hudGo = new GameObject("DebugHud");
            hudGo.transform.SetParent(cameraGo.transform, false);
            hudGo.transform.localPosition = new Vector3(0f, -0.12f, 0.8f);
            hudGo.AddComponent<DebugHud>();

            rig.SetActive(true);
            return rig;
        }

        private static GameObject CreateHand(string name, Transform parent, string usage)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            AddTrackedPose(go,
                $"<XRController>{{{usage}}}/devicePosition",
                $"<XRController>{{{usage}}}/deviceRotation",
                $"<XRController>{{{usage}}}/trackingState");
            go.AddComponent<HandVisual>();
            return go;
        }

        private static void AddTrackedPose(GameObject go, string positionPath, string rotationPath, string statePath)
        {
            var driver = go.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.positionInput = new InputActionProperty(
                new InputAction(go.name + "/Position", InputActionType.Value, positionPath));
            driver.rotationInput = new InputActionProperty(
                new InputAction(go.name + "/Rotation", InputActionType.Value, rotationPath));
            driver.trackingStateInput = new InputActionProperty(
                new InputAction(go.name + "/TrackingState", InputActionType.Value, statePath));
        }
    }
}
