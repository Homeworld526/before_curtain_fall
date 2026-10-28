Shader "Custom/UI/DialogMultiplyMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _Intensity ("Blend Intensity", Range(0, 1)) = 0.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // RGB uses the destination colour as the source factor: result = mask colour * existing scene/portrait colour.
        // Alpha is reset to one so later normal UI blending remains unaffected.
        Blend DstColor Zero, One Zero
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 maskUv : TEXCOORD0;
            };

            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            float _Intensity;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.maskUv = TRANSFORM_TEX(input.texcoord, _MaskTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 maskColour = tex2D(_MaskTex, input.maskUv);
                fixed4 multiplier = lerp(fixed4(1, 1, 1, 1), maskColour, _Intensity);
                multiplier.a = 1;
                return multiplier;
            }
            ENDCG
        }
    }
}
