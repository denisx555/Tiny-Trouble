Shader "TinyTrouble/LobbyBackdrop"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p=(i.uv-float2(.49,.48))*float2(1.7,1);
                float glow=exp(-dot(p,p)*12);
                float3 col=lerp(float3(.003,.007,.023),float3(.012,.075,.14),glow);
                float ring=smoothstep(.009,.003,abs(length(p)-.30));
                col+=float3(.003,.025,.035)*ring;
                float lines=pow(saturate(1-abs(frac((i.uv.x+i.uv.y*.35)*22)-.5)*50),3);
                col+=lines*.006;
                return fixed4(col,1);
            }
            ENDCG
        }
    }
}
