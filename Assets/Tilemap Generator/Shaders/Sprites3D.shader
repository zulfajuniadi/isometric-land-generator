Shader "Sprites/3D"
{
	Properties
	{
        _MainTex3D ("Albedo (RGB)", 3D) = "white" {}
		_TextureOffset ("Texture Offset", Float) = 0
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct vertStruct
            {
                float4 vertex   : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            sampler3D _MainTex3D;
			sampler2D _MainTex;
			float _TextureOffset;

            fixed4 Sample3DTexture (float3 uv)
            {
                fixed4 color = tex3D (_MainTex3D, uv);
                return color;
            }
			
			vertStruct vert (appdata v)
			{
				vertStruct o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = float3(v.uv , _TextureOffset);
				return o;
			}
			
			fixed4 frag (vertStruct IN) : SV_Target
			{
                fixed4 c = Sample3DTexture (IN.texcoord);
				c.rgb -= 0.25;
                c.rgb *= c.a;
				return c;
			}
			ENDCG
		}
	}
}