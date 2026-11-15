Shader "ARES/Martian Wind Dust"
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
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float distance : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.color = input.color; output.uv = input.uv;
                output.distance = distance(world,GetCameraPositionWS());
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv*2-1;
                float softness = pow(saturate(1-dot(p,p)),2);
                float billow = Noise(input.uv*5 + float2(_Time.y*.18,0));
                billow = lerp(billow,Noise(input.uv*11-float2(0,_Time.y*.12)),.3);
                float nearFade = smoothstep(1.5,4,input.distance);
                return half4(input.color.rgb*lerp(.82,1.08,billow),input.color.a*softness*lerp(.4,1.5,billow)*nearFade);
            }
            ENDHLSL
        }
    }
}
