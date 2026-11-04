using UnityEngine;
using UnityEngine.UI;

namespace VoidForge.Integration
{
    public sealed class BlockWorkDisplay:MonoBehaviour
    {
        Canvas canvas;
        Text label;
        public void Hide(){if(canvas!=null)canvas.enabled=false;}
        public void Show(string value,Vector3 point)
        {
            if(string.IsNullOrEmpty(value)||Camera.main==null){Hide();return;}
            if(canvas==null)
            {
                canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
                var rect=(RectTransform)transform;rect.sizeDelta=new Vector2(600,420);transform.localScale=Vector3.one*.00065f;
                var image=new GameObject("Panel",typeof(RectTransform),typeof(Image));image.transform.SetParent(transform,false);
                var r=(RectTransform)image.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                image.GetComponent<Image>().color=new Color(.01f,.04f,.06f,.92f);image.GetComponent<Image>().raycastTarget=false;
                var text=new GameObject("Installed components",typeof(RectTransform),typeof(Text));text.transform.SetParent(image.transform,false);
                r=(RectTransform)text.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(20,15);r.offsetMax=new Vector2(-20,-15);
                label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=30;label.color=Color.cyan;label.supportRichText=false;label.raycastTarget=false;
            }
            canvas.enabled=true;label.text=value;
            Vector3 head=Camera.main.transform.position;transform.position=point+Vector3.up*.3f+(head-point).normalized*.12f;
            transform.rotation=Quaternion.LookRotation(transform.position-head,Camera.main.transform.up);
        }
    }
}
