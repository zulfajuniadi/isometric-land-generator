Shader "Instanced/InstancedIndirectShader" {
    Properties {
        _MainTex3D ("Albedo (RGB)", 3D) = "white" {}
    }
    SubShader {

        Pass {

            Tags
            {
                "Queue"="Transparent"
                "IgnoreProjector"="True"
                "RenderType"="Transparent"
                "PreviewType"="Plane"
                "CanUseSpriteAtlas"="True"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

        #if SHADER_TARGET >= 45
            StructuredBuffer<float4> positionBuffer;
        #endif
            sampler3D _MainTex3D;

            struct vertStruct
            {
                float4 vertex   : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            fixed4 Sample3DTexture (float3 uv)
            {
                fixed4 color = tex3D (_MainTex3D, uv);
                return color;
            }

            vertStruct vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4 data = positionBuffer[instanceID];
            #else
                float4 data = 0;
            #endif
                float3 localPosition = v.vertex.xyz;
                float3 worldPosition = data.xyz + localPosition;
                float3 worldNormal = v.normal;

                vertStruct o;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPosition, 1));
                o.texcoord = float3(v.texcoord.x, v.texcoord.y , data.w);
                return o;
            }

            fixed4 frag (vertStruct IN) : SV_Target
            {
                fixed4 c = Sample3DTexture (IN.texcoord);
                c.rgb *= c.a;
                return c;
            }

            ENDCG
        }
    }
}