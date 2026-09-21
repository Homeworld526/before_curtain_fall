Shader "Custom/PPTGradientWipe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WipeProgress ("Wipe Progress", Range(0,1)) = 0.0  // 刷屏进度 0-1
        _GradientWidth ("Gradient Width", Range(0,0.2)) = 0.05  // 渐变宽度
        _WipeDirection ("Wipe Direction", Int) = 0  // 0=右→左 1=左→右 2=下→上 3=上→下
        _MaskColor ("Mask Color", Color) = (1,1,1,1)  // 遮罩颜色（PPT背景色，通常为白色）
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite On
        ZTest Always

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

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _WipeProgress;
            float _GradientWidth;
            int _WipeDirection;
            float4 _MaskColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float progress = _WipeProgress;
                float gradientWidth = _GradientWidth;
                float edge = 0;

                // 根据方向计算裁切边缘
                if(_WipeDirection == 0) edge = 1 - progress;       // 右→左
                else if(_WipeDirection == 1) edge = progress;      // 左→右
                else if(_WipeDirection == 2) edge = 1 - progress;  // 下→上
                else if(_WipeDirection == 3) edge = progress;      // 上→下

                // 计算UV的判断轴（水平/垂直）
                float uvAxis = (_WipeDirection < 2) ? i.uv.x : i.uv.y;

                // 计算渐变透明度
                float alpha = smoothstep(edge - gradientWidth, edge, uvAxis);
                alpha = 1 - alpha;  // 反转，让渐变从边缘向内过渡

                // 输出遮罩颜色 + 渐变透明度
                fixed4 col = _MaskColor;
                col.a = alpha;
                return col;
            }
            ENDCG
        }
    }
}
