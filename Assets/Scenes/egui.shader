Shader "Unlit/egui"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        Blend One OneMinusSrcAlpha, OneMinusDstAlpha One
        Cull off
        ZWrite off
        ZTest off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float2 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(
                    2.0 * v.vertex.x / _ScreenParams.x - 1.0,
                    1.0 - 2.0 * v.vertex.y / _ScreenParams.y,
                    0.0,
                    1.0
                );
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                #if !UNITY_COLORSPACE_GAMMA
                col.xyz = LinearToGammaSpace(col.xyz);
                #endif
                col = col * i.color;
                #if !UNITY_COLORSPACE_GAMMA         
                col.xyz = GammaToLinearSpace(col.xyz);
                #endif
                return col;
            }
            ENDCG
        }
    }
}