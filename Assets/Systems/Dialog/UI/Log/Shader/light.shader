Shader "UI/GlowHover"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowPower ("Glow Power", Range(0, 10)) = 3
        _GlowSize ("Glow Size", Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        LOD 100
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowPower;
            float _GlowSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= i.color;

                float glow = 0;
                glow += tex2D(_MainTex, i.uv + float2(_GlowSize, 0)).a;
                glow += tex2D(_MainTex, i.uv - float2(_GlowSize, 0)).a;
                glow += tex2D(_MainTex, i.uv + float2(0, _GlowSize)).a;
                glow += tex2D(_MainTex, i.uv - float2(0, _GlowSize)).a;

                fixed3 final = col.rgb * col.a + _GlowColor.rgb * glow * _GlowPower;
                return fixed4(final, col.a);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}