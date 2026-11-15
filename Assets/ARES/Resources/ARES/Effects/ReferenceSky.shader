Shader "ARES/Reference Sky"
{
    Properties { _Day("Day", Range(0,1)) = 1 _SunDirection("Sun", Vector) = (0,1,0,0) _Dust("Dust storm", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Day; float4 _SunDirection; float _Dust;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.direction=v.positionOS.xyz; return o; }
            half4 Frag(Varyings v) : SV_Target
            {
                float3 d=normalize(v.direction), sd=normalize(_SunDirection.xyz);
                float h=saturate(d.y), s=max(dot(d,sd),0);
                float3 zen=lerp(float3(.025,.035,.12),float3(.28,.37,.66),_Day);
                float3 hor=lerp(float3(.15,.12,.25),float3(.97,.65,.49),_Day);
                float3 col=lerp(hor,zen,pow(h,.55));
                float low=1-smoothstep(0,.35,abs(sd.y)), vis=smoothstep(-.2,.05,sd.y);
                col+=lerp(float3(1,.92,.7),float3(.45,.68,1),low)*pow(s,6)*(.25+.75*low)*vis;
                col+=float3(1,.98,.9)*smoothstep(.9988,.9996,s)*vis;
                float3 dustSky=lerp(float3(.53,.33,.19),float3(.31,.22,.17),h)*(.42+.58*_Day);
                col=lerp(col,dustSky,_Dust*.94);
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
