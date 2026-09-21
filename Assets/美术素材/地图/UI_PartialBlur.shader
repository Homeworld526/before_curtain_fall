Shader "Custom/SandFlowBorder"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white" {}
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _FlowSpeed("Flow Speed", Float) = 0.1
        _BorderColor("Border Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _NoiseTex;
            float _FlowSpeed;
            float4 _BorderColor;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 主纹理颜色
                fixed4 col = tex2D(_MainTex, i.uv);

                // 边框形状
                float mask = tex2D(_MaskTex, i.uv).r;

                // 噪声流动
                float2 uvNoise = i.uv + float2(_Time.y * _FlowSpeed, _Time.y * _FlowSpeed);
                float noise = tex2D(_NoiseTex, uvNoise).r;

                // alpha = mask * noise
                float alpha = mask * noise;
                if(alpha < 0.05) discard;

                // 返回颜色 * 边框颜色叠加，带 alpha
                return float4(col.rgb * _BorderColor.rgb, alpha);
            }
            ENDCG
        }
    }
}