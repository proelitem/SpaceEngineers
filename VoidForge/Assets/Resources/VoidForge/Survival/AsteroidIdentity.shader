Shader "VoidForge/AsteroidIdentity"
{
 Properties { _BaseMap("Original asteroid texture",2D)="white"{} _BaseColor("Original tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "ForwardLit"
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4x4 _WorldToAsteroid;
    float4 _VeinA0,_VeinB0,_VeinColor0,_VeinA1,_VeinB1,_VeinColor1;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   Varyings vert(Attributes v)
   {
    Varyings o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(v.normalOS);return o;
   }
   bool ore(float3 p,float4 a,float4 b)
   {float3 axis=b.xyz-a.xyz;float t=saturate(dot(p-a.xyz,axis)/max(dot(axis,axis),1e-8));return a.w>0&&length(p-a.xyz-axis*t)<=a.w;}
   half4 frag(Varyings v):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
    float3 p=mul(_WorldToAsteroid,float4(v.world,1)).xyz;
    half3 n=normalize(v.normal);half3 localN=normalize(mul((float3x3)_WorldToAsteroid,n));
    half3 weights=pow(abs(localN),4);weights/=max(dot(weights,half3(1,1,1)),.001);
    half3 rock=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.yz*4).rgb*weights.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz*4).rgb*weights.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy*4).rgb*weights.z;
    half3 color=rock*_BaseColor.rgb;
    if(ore(p,_VeinA0,_VeinB0))color=lerp(color,_VeinColor0.rgb,.72);
    else if(ore(p,_VeinA1,_VeinB1))color=lerp(color,_VeinColor1.rgb,.72);
    Light light=GetMainLight(TransformWorldToShadowCoord(v.world));
    half3 illumination=SampleSH(n)+light.color*(saturate(dot(n,light.direction))*light.shadowAttenuation+.08);
    return half4(color*max(illumination,half3(.12,.12,.12)),1);
   }
   ENDHLSL
  }
 }
}
