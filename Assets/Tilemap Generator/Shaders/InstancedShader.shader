Shader "Instanced/InstancedShader" {
    Properties {
        _MainTex3D ("Albedo (RGB)", 3D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

        // #if SHADER_TARGET >= 45
        //     StructuredBuffer<float> textureIndices;
        // #endif

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
            // #if SHADER_TARGET >= 45
            //     float data = textureIndices[instanceID];
            // #else
            //     float data = 0;
            // #endif
                vertStruct o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = float3(v.texcoord.x, v.texcoord.y, 0);
                return o;
            }

            fixed4 frag (vertStruct IN) : SV_Target
            {
                fixed4 c = Sample3DTexture (IN.texcoord) * _Color;
                c.rgb *= c.a;
                return c;
            }

            ENDCG
        }
    }
}