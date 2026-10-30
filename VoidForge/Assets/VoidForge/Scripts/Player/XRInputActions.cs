using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidForge.Player
{
    /// <summary>
    /// Actions d'entrée créées en code (aucun asset .inputactions à maintenir).
    /// Les chemins de liaison utilisent les alias génériques XR du package Input System,
    /// qui correspondent aux manettes Touch du Quest 3 via le profil OpenXR « Oculus Touch ».
    /// </summary>
    public sealed class XRInputActions : IDisposable
    {
        public readonly InputAction LeftStick;
        public readonly InputAction RightStick;
        public readonly InputAction LeftGrip;
        public readonly InputAction RightGrip;
        public readonly InputAction LeftTrigger;
        public readonly InputAction RightTrigger;
        public readonly InputAction LeftPrimary;    // X
        public readonly InputAction LeftSecondary;  // Y
        public readonly InputAction RightPrimary;   // A
        public readonly InputAction RightSecondary; // B
        public readonly InputAction LeftMenu;       // bouton menu (manette gauche)

        private readonly InputAction[] _all;

        public XRInputActions()
        {
            LeftStick      = Value("LeftStick",      "<XRController>{LeftHand}/primary2DAxis");
            RightStick     = Value("RightStick",     "<XRController>{RightHand}/primary2DAxis");
            LeftGrip       = Value("LeftGrip",       "<XRController>{LeftHand}/grip");
            RightGrip      = Value("RightGrip",      "<XRController>{RightHand}/grip");
            LeftTrigger    = Value("LeftTrigger",    "<XRController>{LeftHand}/trigger");
            RightTrigger   = Value("RightTrigger",   "<XRController>{RightHand}/trigger");
            LeftPrimary    = Button("LeftPrimary",   "<XRController>{LeftHand}/primaryButton");
            LeftSecondary  = Button("LeftSecondary", "<XRController>{LeftHand}/secondaryButton");
            RightPrimary   = Button("RightPrimary",  "<XRController>{RightHand}/primaryButton");
            RightSecondary = Button("RightSecondary","<XRController>{RightHand}/secondaryButton");
            LeftMenu       = Button("LeftMenu",      "<XRController>{LeftHand}/menuButton");

            _all = new[]
            {
                LeftStick, RightStick, LeftGrip, RightGrip, LeftTrigger, RightTrigger,
                LeftPrimary, LeftSecondary, RightPrimary, RightSecondary, LeftMenu
            };
        }

        private static InputAction Value(string name, string binding)
        {
            return new InputAction(name, InputActionType.Value, binding);
        }

        private static InputAction Button(string name, string binding)
        {
            return new InputAction(name, InputActionType.Button, binding);
        }

        public void Enable()
        {
            foreach (var a in _all) a.Enable();
        }

        public void Disable()
        {
            foreach (var a in _all) a.Disable();
        }

        public void Dispose()
        {
            foreach (var a in _all) a.Dispose();
        }
    }
}
