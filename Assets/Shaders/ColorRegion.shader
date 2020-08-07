Shader "MicroUniverse/ColorRegion"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
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
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {

                // color MapTex -> ColoredMapTex
                // MapTex's black pixel will be treated as transparent (0, 0, 0, 0)
                // White pixel will be colored by given _Color.

                fixed4 col = tex2D(_MainTex, i.uv);
                col = (col.r + col.g + col.b) > 0 ? _Color : fixed4(0, 0, 0, 0);
                return col;
            }
            ENDCG
        }
    }
}
