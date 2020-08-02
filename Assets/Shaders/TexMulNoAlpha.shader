Shader "MicroUniverse/TexMulNoAlpha"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        [NoScaleOffset] _MaskTex ("Texture", 2D) = "white" {}
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
            sampler2D _MaskTex;

            fixed4 frag (v2f i) : SV_Target
            {
                // double clip.
                fixed4 col = tex2D(_MainTex, i.uv);
                if (col.a < 0.5) col = 1;
                fixed4 maskCol = tex2D(_MaskTex, i.uv);
                if (maskCol.a < 0.5) maskCol = 1;
                return fixed4(col.r * maskCol.r, col.g * maskCol.g, col.b * maskCol.b, 1);
            }
            ENDCG
        }
    }
}
