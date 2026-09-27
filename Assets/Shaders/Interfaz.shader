// Shader de la interfaz (reloj en pantalla, menú de pausa y pantalla de tiempo agotado).
//
// Pinta un color plano: no le afectan las luces ni la niebla de los cuartos, así el reloj y los
// menús se ven con sus mismos colores en los cuatro cuartos, aunque estén a oscuras.
//
// Por qué no el "Unlit" de URP: URP revisa sus materiales transparentes cada vez que los carga y
// les apaga "ZWrite" (escribir la profundidad). Sin profundidad, los carteles del cuarto que están
// DETRÁS del reloj se dibujaban encima de él. Acá las reglas de dibujado las pone el material y
// nadie las cambia:
//  - _ZWrite: 1 para las piezas de los paneles (tapan lo que está detrás), 0 para la esfera que
//    oscurece el cuarto.
//  - _Cull: 2 (Back) para las piezas, 0 (Off) para la esfera, que se ve desde adentro.
// El orden de dibujado (qué pieza va encima de cuál) es la "Render Queue" de cada material.
//
// Tiene las macros de estéreo de Unity: en el Quest se dibuja en los dos ojos a la vez
// (single pass / multiview); sin ellas se vería en un solo ojo.
Shader "EscapeRoom/Interfaz"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [Enum(Off, 0, On, 1)] _ZWrite ("Escribe profundidad", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Caras", Float) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _BaseColor;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _BaseColor;
            }
            ENDCG
        }
    }
}
