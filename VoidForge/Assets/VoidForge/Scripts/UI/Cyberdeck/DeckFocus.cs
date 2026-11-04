using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidForge.UI.Cyberdeck
{
    public sealed class DeckFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public DeckPanel panel;
        public bool active;
        Color resting;
        bool captured;
        void Focus(bool on)
        {
            if (!GetComponent<Button>().interactable) return;
            if (!captured) { resting = panel.border; captured = true; }
            panel.border = on ? Color.white : resting;
            panel.SetVerticesDirty();
        }
        public void OnPointerEnter(PointerEventData data) => Focus(true);
        public void OnPointerExit(PointerEventData data) => Focus(false);
        public void OnSelect(BaseEventData data) => Focus(true);
        public void OnDeselect(BaseEventData data) => Focus(false);
    }
}
