Shader "Unlit/NewUnlitShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            StructuredBuffer<float3x4> VirtualJoints;
            uint NumInfluences;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float4 weight  : BLENDWEIGHTS;
                uint4  index   : BLENDINDICES;
            };

            struct v2f
            {
                half3 normal: TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                float3x4 m = (float3x4)0;
                for (int i = 0; i < NumInfluences; ++i)
                {
                    m += v.weight[i] * VirtualJoints[v.index[i]];
                }
                o.vertex = UnityObjectToClipPos(mul(m, v.vertex));
                m._14 = m._24 = m._34 = 0;
                o.normal = normalize(mul(m, v.normal));
                o.normal = UnityObjectToWorldNormal(o.normal);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.5f, 0.5f, 0.5f, 1.0f);
                col.rgb *= max(0.2f, dot(i.normal, _WorldSpaceLightPos0.xyz));
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
