using UnityEngine;

namespace VoidForge.Integration
{
    public sealed class HandToolEffects:MonoBehaviour
    {
        HandTool tool;
        GameObject instance;
        ParticleSystem[] particles;
        bool running;
        public bool Running=>running;
        public void Bind(HandTool owner)
        {
            tool=owner;
            var library=Resources.Load<ToolEffectLibrary>("VoidForge/Survival/ToolEffects");
            if(library==null)throw new System.InvalidOperationException("Sci-Fi Arsenal tool effect library missing.");
            instance=Instantiate(owner.kind==HandToolKind.Welder?library.weldingSparks:library.cuttingSparks,owner.tip,false);
            instance.name="Sci-Fi Arsenal • reusable tool contact effect";
            instance.transform.localScale=Vector3.one*(owner.kind==HandToolKind.Welder?.035f:.055f);
            particles=instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach(var particle in particles)particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void SetRunning(bool operating)
        {
            bool contact=false;RaycastHit nearest=default;
            if(operating&&tool!=null&&tool.tip!=null)
            {
                float distance=float.PositiveInfinity;
                foreach(var hit in Physics.RaycastAll(tool.tip.position,tool.tip.forward,1,~0,QueryTriggerInteraction.Collide))
                {
                    if(hit.collider.GetComponentInParent<HandTool>()!=null||hit.collider.GetComponentInParent<VoidForge.Player.PlayerInputProvider>()!=null)continue;
                    if(hit.collider.isTrigger&&hit.collider.GetComponent<ConstructionPlanMarker>()==null)continue;
                    if(hit.distance<distance){nearest=hit;distance=hit.distance;contact=true;}
                }
            }
            if(contact&&instance!=null)instance.transform.SetPositionAndRotation(nearest.point+nearest.normal*.005f,Quaternion.LookRotation(nearest.normal));
            bool next=operating&&contact;
            if(next==running)return;running=next;
            if(particles==null)return;
            foreach(var particle in particles)
                if(next)particle.Play(false);else particle.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        void OnDisable()=>SetRunning(false);
    }
}
