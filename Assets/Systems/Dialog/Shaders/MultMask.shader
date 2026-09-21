Shader "Custom/ScreenMultiplyMask_BuiltIn"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _Intensity ("Blend Intensity", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _MaskTex; float4 _MaskTex_ST;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 screenColor = tex2D(_MainTex, i.uv);
                fixed4 maskColor = tex2D(_MaskTex, i.uv);
                fixed4 multiplyColor = screenColor * maskColor;
                return lerp(screenColor, multiplyColor, _Intensity);
            }
            ENDCG
        }
    }
}
