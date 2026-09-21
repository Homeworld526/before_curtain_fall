Shader "Custom/UI_ParchmentBorder"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Texture (Square Mask)", 2D) = "white" {}
        
        [Header(Color and Texture)]
        _Color ("Edge Dark Color (边缘深色)", Color) = (0.35, 0.2, 0.1, 1)
        _PaperColor ("Paper Light Color (纸张亮色)", Color) = (0.6, 0.45, 0.3, 1)
        _NoiseTex ("Parchment Noise (噪声/质感图)", 2D) = "grey" {}
        _NoiseScale ("Noise Scale (质感缩放)", Range(0.1, 5)) = 1.0

        [Header(Shape and Distortion)]
        _Distortion ("Edge Irregularity (边缘扭曲度)", Range(0, 0.5)) = 0.08
        _Thickness ("Border Thickness (边框厚度)", Range(0.01, 0.5)) = 0.1
        _EdgeBlur ("Outer Edge Softness (外边缘羽化)", Range(0.001, 0.2)) = 0.05
        _InnerBlur ("Inner Edge Softness (内边缘羽化)", Range(0.001, 0.2)) = 0.02
        
        [Header(Transparency)]
        _Opacity ("Overall Opacity (整体透明度)", Range(0, 1)) = 1.0
        _TextureInfluence ("Texture Influence (质感对透明度影响)", Range(0, 1)) = 0.5

        // UGUI Support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { 
                float4 vertex : POSITION; 
                float4 color : COLOR; 
                float2 texcoord : TEXCOORD0; 
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };
            
            struct v2f { 
                float4 vertex : SV_POSITION; 
                fixed4 color : COLOR; 
                float2 texcoord : TEXCOORD0; 
                float4 worldPosition : TEXCOORD1; 
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            
            fixed4 _Color;
            fixed4 _PaperColor;
            float _NoiseScale;
            float _Distortion;
            float _Thickness;
            float _EdgeBlur;
            float _InnerBlur;
            float _Opacity;
            float _TextureInfluence;
            
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. 采样噪声并对 UV 进行扭曲，创造不规则边缘
                float2 noiseUV = IN.texcoord * _NoiseScale;
                half4 noiseVal = tex2D(_NoiseTex, noiseUV);
                
                // 使用噪声偏移原始 UV，使得原本直线的 Mask 变弯曲
                float2 distortedUV = IN.texcoord + (noiseVal.rg - 0.5) * _Distortion;
                
                // 2. 采样主遮罩（资源中心为透明时使用 Alpha 通道）
                half maskVal = tex2D(_MainTex, distortedUV).a;

                // 3. 计算边框形状
                // OuterCutoff 默认为 0 附近，InnerCutoff 由厚度决定
                float outerRef = 0.1; 
                float innerRef = outerRef + _Thickness;

                // 使用 smoothstep 实现内外两层羽化
                half outerAlpha = smoothstep(outerRef - _EdgeBlur, outerRef + _EdgeBlur, maskVal);
                half innerAlpha = smoothstep(innerRef - _InnerBlur, innerRef + _InnerBlur, maskVal);
                
                // 镂空中心，得到边框形状
                half borderAlpha = outerAlpha - innerAlpha;

                // 4. 颜色与质感融合
                // 根据噪声的 R 通道在深色和亮色间插值，模拟纸张纹理
                half3 paperEffect = lerp(_Color.rgb, _PaperColor.rgb, noiseVal.r);
                
                // 5. 最终透明度计算
                // 结合质感细节，让边框看起来有薄厚不均的感觉
                half textureAlpha = lerp(1.0, noiseVal.b, _TextureInfluence);
                half finalAlpha = borderAlpha * _Opacity * textureAlpha * IN.color.a;

                half4 color = half4(paperEffect, finalAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return color;
            }
            ENDCG
        }
    }
}