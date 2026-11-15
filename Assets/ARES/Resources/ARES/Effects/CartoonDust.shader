Shader "ARES/Cartoon Dust"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color; output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                // Three round lobes form a soft, slightly outlined cartoon cloud.
                float cloud = min(length(p-float2(-.3,-.12))-.48,
                              min(length(p-float2(.26,-.12))-.49, length(p-float2(0,.25))-.51));
                float edge = max(fwidth(cloud), .012);
                half alpha = (1-smoothstep(-edge,edge,cloud)) * input.color.a;
                half shade = lerp(.78,1,1-smoothstep(-.11,-.035,cloud));
                return half4(input.color.rgb * shade,alpha);
            }
            ENDHLSL
        }
    }
}
