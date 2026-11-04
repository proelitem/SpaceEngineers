using UnityEngine;

namespace VoidForge.Integration
{
    public sealed class GpsTargetBounds : MonoBehaviour
    {
        public Vector3 size=Vector3.one;
        public Bounds WorldBounds
        {
            get
            {
                var bounds=new Bounds(transform.position,Vector3.zero);
                for(int i=0;i<8;i++)bounds.Encapsulate(transform.TransformPoint(Vector3.Scale(size,new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f))));
                return bounds;
            }
        }
    }
}
