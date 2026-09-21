Shader "Custom/UI/Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurRadius ("Blur Radius", Range(0, 10)) = 1.0  // 模糊半径（动态调整）
        _BlurIterations ("Blur Iterations", Range(1, 8)) = 2  // 模糊迭代次数（越高越模糊，性能稍降）
        _BlurSpread ("Blur Spread", Range(0.0, 0.5)) = 0.1  // 模糊扩散度
    }

    SubShader
    {
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _BlurRadius;
            int _BlurIterations;
            float _BlurSpread;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 blurVector = float2(_BlurRadius * _BlurSpread, 0);
                fixed4 col = fixed4(0,0,0,0);

                // 高斯模糊核心：多次采样叠加（动态迭代）
                for (int iter = 0; iter < _BlurIterations; iter++)
                {
                    float offset = (iter - _BlurIterations / 2.0) * blurVector.x;
                    col += tex2D(_MainTex, i.uv + float2(offset, 0)) * i.color;
                    col += tex2D(_MainTex, i.uv - float2(offset, 0)) * i.color;
                    col += tex2D(_MainTex, i.uv + float2(0, offset)) * i.color;
                    col += tex2D(_MainTex, i.uv - float2(0, offset)) * i.color;
                }
                col /= _BlurIterations * 4; // 平均采样值
                col.a *= i.color.a; // 保留原Image透明度
                return col;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
