#if VF_LOCAL_DIGGER && VF_LOCAL_ECONOMY
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VoidForge.Integration;
using VoidForge.UI.Cyberdeck;
using Object=UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class Recette04Capture
    {
        public static void Render(SurvivalSession session)
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
            const string output="Hermes_Works/Preuves/INTEGRATION_04";Directory.CreateDirectory(output);
            var go=Object.Instantiate(Resources.Load<GameObject>("VoidForge/Cyberdeck/Hud"));
            go.transform.position=new Vector3(10000,10000,10000);go.transform.rotation=Quaternion.identity;go.transform.localScale=Vector3.one;
            var hud=go.GetComponent<CyberdeckHud>();hud.Initialize();JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(session.deck.State),hud.State);
            hud.BlockAvailable=session.economy.BlockSupported;hud.BlockSize=session.economy.BlockSize;hud.State.BlockIngredients=session.deck.State.BlockIngredients;
            var camera=new GameObject("QA Module HUD camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=CyberdeckHud.Height*.5f;
            camera.transform.position=go.transform.position+Vector3.back*1000;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.03f,.04f);camera.cullingMask=1<<29;
            try
            {
                foreach(var page in new[]{DeckPage.Systems,DeckPage.Construction})
                {
                    hud.Show(page);if(page==DeckPage.Construction)hud.MoveBlock(3);
                    foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                    Canvas.ForceUpdateCanvases();var rt=new RenderTexture(1914,822,24);rt.Create();camera.targetTexture=rt;
                    var previous=RenderTexture.active;var texture=new Texture2D(1914,822,TextureFormat.RGB24,false);
                    try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1914,822),0,0);texture.Apply();File.WriteAllBytes(output+"/"+(page==DeckPage.Systems?"module-systems":"construction-refinery")+".png",texture.EncodeToPNG());}
                    finally{RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);rt.Release();Object.DestroyImmediate(rt);}
                }
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(camera.gameObject);}
        }
    }
}
#endif
