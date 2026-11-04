using UnityEngine;
using VoidForge.UI.Cyberdeck;

namespace VoidForge.Integration
{
    // Keep UI in world metres after IK. Imported bone scales must never scale the stand-off.
    [DefaultExecutionOrder(10030)]
    public sealed class WristHudAttachment:MonoBehaviour
    {
        CyberdeckHud hud;
        CyberdeckSurface surface;
        public const float ScreenGap=.00015f;
        public static void Attach(CyberdeckHud hud,CyberdeckSurface surface)
        {
            var attachment=hud.GetComponent<WristHudAttachment>()??hud.gameObject.AddComponent<WristHudAttachment>();
            attachment.hud=hud;attachment.surface=surface;attachment.Refresh();
        }
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(hud==null||surface==null||hud.Expanded)return;
            if(transform.parent!=null)transform.SetParent(null,true);
            transform.SetPositionAndRotation(surface.hudAnchor.position+surface.hudAnchor.forward*ScreenGap,surface.hudAnchor.rotation*Quaternion.Euler(0,180,0));
            float scale=Mathf.Min(surface.safeMeters.x/CyberdeckHud.Width,surface.safeMeters.y/CyberdeckHud.Height);
            transform.localScale=Vector3.one*scale;
        }
    }
}
