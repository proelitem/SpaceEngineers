#if VF_LOCAL_ECONOMY
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidForge.Player;

namespace VoidForge.Integration
{
    // Local solo rigid modules. Constant force means cargo changes both acceleration and braking.
    [DefaultExecutionOrder(-25)]
    public sealed class ModuleMotion:MonoBehaviour
    {
        readonly Dictionary<int,Rigidbody> bodies=new Dictionary<int,Rigidbody>();
        SurvivalSession session;
        PlayerInputProvider player;
        int seatedGrid=-1;
        Vector3 previousPosition;
        Quaternion previousRotation;
        public void Initialize(SurvivalSession owner){session=owner;player=FindFirstObjectByType<PlayerInputProvider>();Refresh();}
        public Transform Root(int grid)=>bodies[grid].transform;
        public void Refresh()
        {
            foreach(var frame in session.construction.Grids)
            {
                if(!bodies.TryGetValue(frame.id,out var body))
                {
                    body=new GameObject("Module body "+frame.id).AddComponent<Rigidbody>();
                    body.transform.SetPositionAndRotation(frame.origin,frame.rotation);
                    body.useGravity=false;body.isKinematic=frame.id==0;body.linearDamping=body.angularDamping=0;
                    body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                    if(!body.isKinematic){body.linearVelocity=frame.velocity;body.angularVelocity=frame.angularVelocity;}
                    bodies.Add(frame.id,body);
                }
                body.position=frame.origin;body.rotation=frame.rotation;
                body.mass=Mathf.Max(1,session.modules.DryMass(frame.id)+session.modules.CargoMass(frame.id));
            }
        }
        void Update()
        {
            if(session==null||!session.Ready)return;
            foreach(var frame in session.construction.Grids)
            {
                var body=bodies[frame.id];frame.origin=body.position;frame.rotation=body.rotation;
                frame.velocity=body.linearVelocity;frame.angularVelocity=body.angularVelocity;
            }
            if(session.economy.Seated&&player!=null)
            {
                int grid=session.modules.GridOf(session.economy.SeatedBlock.uid);var body=bodies[grid];
                if(grid==seatedGrid)
                {
                    Quaternion delta=body.rotation*Quaternion.Inverse(previousRotation);
                    player.transform.SetPositionAndRotation(body.position+delta*(player.transform.position-previousPosition),delta*player.transform.rotation);
                }
                seatedGrid=grid;previousPosition=body.position;previousRotation=body.rotation;
            }
            else seatedGrid=-1;
            foreach(var frame in session.construction.Grids)session.construction.TargetForGrid(frame.id);
        }
        void FixedUpdate()
        {
            if(session==null||!session.CanAct||!Application.isFocused||!session.economy.Seated||player?.Actions==null||session.deck?.PointerBusy==true)return;
            int grid=session.modules.GridOf(session.economy.SeatedBlock.uid);var body=bodies[grid];if(body.isKinematic||!session.modules.Enabled(session.economy.SeatedBlock.uid))return;
            body.mass=Mathf.Max(1,session.modules.DryMass(grid)+session.modules.CargoMass(grid));
            var world=session.modules.World(grid);
            int thrusters=world.Blocks.Count(b=>b.type=="Thruster"&&session.modules.Enabled(b.uid));
            int gyros=world.Blocks.Count(b=>b.type=="Gyroscope"&&session.modules.Enabled(b.uid));
            Vector2 stick=player.Actions.LeftStick.ReadValue<Vector2>();Vector2 turn=player.Actions.RightStick.ReadValue<Vector2>();
            float vertical=player.Actions.RightTrigger.ReadValue<float>()-player.Actions.LeftTrigger.ReadValue<float>();
            Vector3 direction=body.rotation*Vector3.ClampMagnitude(new Vector3(stick.x,vertical,stick.y),1);
            if(thrusters>0)
            {
                float strength=thrusters*2500;
                if(direction.sqrMagnitude>.01f&&session.modules.DrawPower(grid,10*thrusters*Time.fixedDeltaTime/3600))body.AddForce(direction*strength,ForceMode.Force);
                if(player.Actions.LeftGrip.ReadValue<float>()>.5f&&body.linearVelocity.sqrMagnitude>.00001f&&session.modules.DrawPower(grid,10*thrusters*Time.fixedDeltaTime/3600))
                    body.AddForce(-body.linearVelocity.normalized*Mathf.Min(strength,body.mass*body.linearVelocity.magnitude/Time.fixedDeltaTime),ForceMode.Force);
            }
            if(gyros>0&&turn.sqrMagnitude>.01f&&session.modules.DrawPower(grid,2*gyros*Time.fixedDeltaTime/3600))
                body.AddTorque(body.rotation*new Vector3(-turn.y,turn.x,0)*150*gyros,ForceMode.Force);
        }
        void OnDestroy(){foreach(var body in bodies.Values)if(body!=null)Destroy(body.gameObject);}
    }
}
#endif
