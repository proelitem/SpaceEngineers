using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using VoidForge.Player;

namespace VoidForge.Net
{
    /// <summary>
    /// Démarrage du multijoueur (base posée au jalon J1, élargie aux jalons M1 à M3 de la feuille de route).
    /// Modèle : un joueur héberge (host = serveur + client), les autres rejoignent par adresse IP
    /// sur le réseau local. Sans PC dédié ni service de relais pour l'instant.
    ///
    /// Commandes (en attendant le menu au poignet du jalon J5) :
    /// - Manette gauche, bouton Menu maintenu 1,5 s : héberger.
    /// - Manette droite, bouton B maintenu 1,5 s : rejoindre <see cref="address"/>.
    /// - Clavier (éditeur) : H héberger, J rejoindre, K quitter.
    /// </summary>
    public sealed class NetworkLauncher : MonoBehaviour
    {
        public enum StartMode { Offline, Host, Client }

        [Header("Démarrage")]
        [Tooltip("Mode lancé automatiquement au démarrage de la scène.")]
        [SerializeField] private StartMode autoStart = StartMode.Offline;
        [Tooltip("Adresse IPv4 de l'hôte à rejoindre (réseau local).")]
        [SerializeField] private string address = "192.168.1.10";
        [SerializeField] private ushort port = 7777;
        [SerializeField] private float holdSeconds = 1.5f;

        private NetworkManager _manager;
        private UnityTransport _transport;
        private PlayerInputProvider _input;
        private float _hostHold;
        private float _joinHold;

        public string StatusText { get; private set; } = "réseau : hors ligne";

        private void Awake()
        {
            _manager = GetComponent<NetworkManager>();
            if (_manager == null) _manager = NetworkManager.Singleton;
            _transport = _manager != null ? _manager.GetComponent<UnityTransport>() : null;
        }

        private void Start()
        {
            _input = FindFirstObjectByType<PlayerInputProvider>();

            if (_manager != null)
            {
                _manager.OnClientConnectedCallback += _ => RefreshStatus();
                _manager.OnClientDisconnectCallback += _ => RefreshStatus();
            }

            switch (autoStart)
            {
                case StartMode.Host: StartHost(); break;
                case StartMode.Client: StartClient(); break;
            }
            RefreshStatus();
        }

        private void Update()
        {
            if (_manager == null) return;

            bool hostPressed = false, joinPressed = false, quitPressed = false;

            if (_input != null && _input.Actions != null)
            {
                hostPressed = _input.Actions.LeftMenu.IsPressed();
                joinPressed = _input.Actions.RightSecondary.IsPressed();
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                hostPressed |= kb.hKey.isPressed;
                joinPressed |= kb.jKey.isPressed;
                quitPressed = kb.kKey.wasPressedThisFrame;
            }

            if (!_manager.IsListening)
            {
                _hostHold = hostPressed ? _hostHold + Time.unscaledDeltaTime : 0f;
                _joinHold = joinPressed ? _joinHold + Time.unscaledDeltaTime : 0f;

                if (_hostHold >= holdSeconds) { _hostHold = 0f; StartHost(); }
                else if (_joinHold >= holdSeconds) { _joinHold = 0f; StartClient(); }
            }
            else if (quitPressed)
            {
                Shutdown();
            }
        }

        public void StartHost()
        {
            if (_manager == null || _manager.IsListening) return;
            ApplyConnectionData(listenAll: true);
            bool ok = _manager.StartHost();
            Debug.Log(ok ? "[VoidForge] Hôte démarré." : "[VoidForge] Échec du démarrage de l'hôte.");
            RefreshStatus();
        }

        public void StartClient()
        {
            if (_manager == null || _manager.IsListening) return;
            ApplyConnectionData(listenAll: false);
            bool ok = _manager.StartClient();
            Debug.Log(ok ? $"[VoidForge] Connexion à {address}:{port}…" : "[VoidForge] Échec du démarrage client.");
            RefreshStatus();
        }

        public void Shutdown()
        {
            if (_manager == null) return;
            _manager.Shutdown();
            Debug.Log("[VoidForge] Session réseau arrêtée.");
            RefreshStatus();
        }

        private void ApplyConnectionData(bool listenAll)
        {
            if (_transport == null) return;
            _transport.SetConnectionData(address, port, listenAll ? "0.0.0.0" : null);
        }

        private void RefreshStatus()
        {
            if (_manager == null) { StatusText = "réseau : absent"; return; }
            if (!_manager.IsListening) { StatusText = "réseau : hors ligne (Menu=héberger, B=rejoindre)"; return; }
            if (_manager.IsHost) StatusText = $"réseau : hôte, {_manager.ConnectedClientsIds.Count} joueur(s)";
            else if (_manager.IsConnectedClient) StatusText = $"réseau : connecté à {address}";
            else StatusText = $"réseau : connexion à {address}…";
        }
    }
}
