Shader "Unlit/DeformedCube"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            #include "./shared/Vertex.cginc"

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 normal : NORMAL;
                half3 diffuse : TEXCOORD0;
            };

            StructuredBuffer<Vertex> DeformedVertices;

            Varyings vert (uint vertex_id: SV_VERTEXID, uint instance_id: SV_INSTANCEID)
            {
                Varyings OUT;

                uint index = vertex_id; // since we are rendering just one instance
                Vertex v = DeformedVertices[index];

                OUT.position = UnityObjectToClipPos(v.position);
                OUT.normal = float4(v.normal, 0);

                // calculate lighting
                float3 lightDirection = _WorldSpaceLightPos0.xyz;

                half3 worldNormal = UnityObjectToWorldNormal(OUT.normal);

                // dot product between normal and light vector, provide the basis for the lit shading
                half lightInfluence = max(0, dot(worldNormal, lightDirection)); // avoid negative values
                OUT.diffuse = lightInfluence * _LightColor0;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half3 color = half3(1,1,1) * IN.diffuse;
                return half4(color,1);
            }
            ENDCG
        }
    }
}
