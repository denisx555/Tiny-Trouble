// Shader — правило рисования поверхности. Создаёт синий фон, свечение и полосы.
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
            // Из каждой вершины (угла фигуры) передаём положение и uv — координаты точки на поверхности от 0 до 1.
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            // Переводим положение вершины в координаты экрана и передаём координаты поверхности дальше.
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            // Выбираем цвет каждого пикселя — маленькой точки изображения.
            fixed4 frag(v2f i) : SV_Target
            {
                // Смещаем центр свечения и учитываем ширину фона.
                float2 p=(i.uv-float2(.49,.48))*float2(1.7,1);
                // Чем ближе к центру, тем ярче; lerp плавно смешивает тёмный и светлый синий.
                float glow=exp(-dot(p,p)*12);
                float3 col=lerp(float3(.003,.007,.023),float3(.012,.075,.14),glow);
                // Небольшая светлая окружность вокруг центра.
                float ring=smoothstep(.009,.003,abs(length(p)-.30));
                col+=float3(.003,.025,.035)*ring;
                // Повторяющийся узор создаёт тонкие диагональные полосы.
                float lines=pow(saturate(1-abs(frac((i.uv.x+i.uv.y*.35)*22)-.5)*50),3);
                col+=lines*.006;
                // Последнее число 1 означает полностью непрозрачную поверхность.
                return fixed4(col,1);
            }
            ENDCG
        }
    }
}
