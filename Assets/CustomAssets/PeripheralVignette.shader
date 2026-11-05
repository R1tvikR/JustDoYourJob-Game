Shader "Custom/PeripheralVignette"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Intensity ("Intensity", Range(0,1)) = 0
        _Bias ("Side Bias (-1 left, +1 right)", Range(-1,1)) = 0
        _BiasStrength ("Bias Strength", Range(0,1)) = 0.8
        _InnerMax ("Inner Radius At 0 Intensity", Range(0,1.5)) = 1.1
        _InnerMin ("Inner Radius At Full Intensity", Range(0,1.5)) = 0.35
        _Softness ("Edge Softness", Range(0.05,1)) = 0.45
        _MaxAlpha ("Max Alpha", Range(0,1)) = 0.95
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity, _Bias, _BiasStrength, _InnerMax, _InnerMin, _Softness, _MaxAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 p = IN.uv * 2.0 - 1.0;
                float dist = length(p);
                float inner = lerp(_InnerMax, _InnerMin, _Intensity);
                float edge = smoothstep(inner, inner + _Softness, dist);
                float side = saturate(1.0 + _Bias * p.x * _BiasStrength);
                float a = edge * side * _MaxAlpha * saturate(_Intensity * 3.0);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}