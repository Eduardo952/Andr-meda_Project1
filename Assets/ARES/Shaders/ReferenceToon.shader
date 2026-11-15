Shader "ARES/Reference Toon"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _BaseMap("Albedo", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0)
        _Cull("Cull", Float) = 2
        _Surface("Surface", Float) = 0
        _ZWrite("Depth", Float) = 1
        _SrcBlend("Source blend", Float) = 1
        _DstBlend("Destination blend", Float) = 0
        _Cutoff("Cutoff", Float) = .5
        _Smoothness("Smoothness", Float) = 0
        _Metallic("Metallic", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull] ZWrite [_ZWrite] Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor, _EmissionColor;
                float _Surface, _Cutoff, _Smoothness, _Metallic;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o; VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap); o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings v):SV_Target
            {
                float3 n=normalize(v.normalWS);
                Light light=GetMainLight(TransformWorldToShadowCoord(v.positionWS));
                float level=dot(n,light.direction)*.5+.5;
                // Soft toon shoulders preserve rounded forms without a harsh posterized terminator.
                float ramp=.32+.34*smoothstep(.25,.48,level)+.34*smoothstep(.62,.82,level);
                half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb;
                float3 view=GetWorldSpaceNormalizeViewDir(v.positionWS);
                float shade=lerp(.26,1,light.shadowAttenuation);
                half3 color=albedo*(SampleSH(n)*.85+light.color*ramp*light.distanceAttenuation*shade*.42);
                float spec=pow(saturate(dot(n,normalize(light.direction+view))),lerp(18,90,_Smoothness));
                color+=light.color*spec*lerp(.025,.20,_Smoothness)*shade;
                #ifdef _ADDITIONAL_LIGHTS
                uint count=GetAdditionalLightsCount();
                for(uint i=0;i<count;i++)
                {
                    Light local=GetAdditionalLight(i,v.positionWS);
                    color+=albedo*local.color*saturate(dot(n,local.direction))*min(local.distanceAttenuation,2)*local.shadowAttenuation*.32;
                }
                #endif
                color+=albedo*half3(.23,.38,.48)*pow(1-saturate(dot(n,view)),3)*.14+_EmissionColor.rgb;
                return half4(MixFog(color,v.fog),_BaseColor.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
