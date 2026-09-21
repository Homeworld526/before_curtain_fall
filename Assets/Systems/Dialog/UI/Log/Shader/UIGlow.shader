Shader "UI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowPower ("Glow Power", Range(0, 5)) = 1.5
        _GlowSize ("Glow Size", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _GlowColor;
            float _GlowPower;
            float _GlowSize;

            v2f vert (appdata v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.uv = TRANSFORM_TEX(v.uv, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // 采样周围像素，计算发光强度
                float glow = 0;
                glow += tex2D(_MainTex, i.uv + float2(_GlowSize, 0)).a;
                glow += tex2D(_MainTex, i.uv - float2(_GlowSize, 0)).a;
                glow += tex2D(_MainTex, i.uv + float2(0, _GlowSize)).a;
                glow += tex2D(_MainTex, i.uv - float2(0, _GlowSize)).a;
                glow += tex2D(_MainTex, i.uv + float2(_GlowSize, _GlowSize)).a;
                glow += tex2D(_MainTex, i.uv - float2(_GlowSize, _GlowSize)).a;
                glow += tex2D(_MainTex, i.uv + float2(_GlowSize, -_GlowSize)).a;
                glow += tex2D(_MainTex, i.uv - float2(_GlowSize, -_GlowSize)).a;
                
                glow /= 8;
                glow = saturate(glow - col.a); // 只取边缘部分
                
                fixed4 glowCol = _GlowColor * glow * _GlowPower;
                col.rgb += glowCol.rgb;
                col.a = col.a;
                
                return col;
            }
            ENDCG
        }
    }
}