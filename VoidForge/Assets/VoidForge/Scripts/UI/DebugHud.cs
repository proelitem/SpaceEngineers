using UnityEngine;
using VoidForge.Net;
using VoidForge.Player;

namespace VoidForge.UI
{
    /// <summary>
    /// HUD de débogage minimal (texte 3D attaché à la caméra) : images/s, vitesse, état réseau.
    /// Sert aux notes de test du jalon J1 ; remplacé par un vrai HUD au jalon J3.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        [SerializeField] private float refreshInterval = 0.25f;

        private TextMesh _text;
        private ZeroGLocomotion _locomotion;
        private NetworkLauncher _net;
        private float _timer;
        private int _frames;
        private float _fps;

        private void Awake()
        {
            _text = GetComponent<TextMesh>();
            if (_text == null)
            {
                _text = gameObject.AddComponent<TextMesh>();
                _text.anchor = TextAnchor.MiddleCenter;
                _text.alignment = TextAlignment.Center;
                _text.characterSize = 0.004f;
                _text.fontSize = 64;
                _text.color = new Color(0.6f, 0.9f, 1f, 0.9f);
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    _text.font = font;
                    GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
            }
        }

        private void Start()
        {
            _locomotion = GetComponentInParent<ZeroGLocomotion>();
            _net = FindFirstObjectByType<NetworkLauncher>();
        }

        private void Update()
        {
            _frames++;
            _timer += Time.unscaledDeltaTime;
            if (_timer < refreshInterval) return;

            _fps = _frames / _timer;
            _frames = 0;
            _timer = 0f;

            float speed = _locomotion != null ? _locomotion.Speed : 0f;
            string net = _net != null ? _net.StatusText : "réseau : absent";
            _text.text = $"{_fps:0} i/s\n{speed:0.0} m/s\n{net}";
        }
    }
}
