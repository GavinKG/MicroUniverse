Shader "MicroUniverse/Ring2StripeBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InnerRing("Inner Ring Percentage", Range(0, 1)) = 0.1
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _InnerRing;

            fixed4 frag (v2f i) : SV_Target
            {
                float r = lerp(_InnerRing, 1, i.uv.y);
                float theta = i.uv.x * 3.1415 * 2;
                float2 ringUV = float2(r * cos(theta), r * sin(theta));
                ringUV = (ringUV + float2(1, 1)) * 0.5;
                // return float4(ringUV.x, ringUV.y, 0, 1);
                fixed4 color = tex2D(_MainTex, ringUV);
                return color;
            }
            ENDCG
        }
    }
}
