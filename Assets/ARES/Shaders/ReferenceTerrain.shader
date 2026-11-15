Shader "ARES/Reference Terrain"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _BaseMap("Reference regolith", 2D) = "white" {}
        _HeightMap("Reference relief", 2D) = "gray" {}
        _HeightScale("Bump scale", Float) = 0.12
        _Smoothness("Smoothness", Range(0,1)) = 0.02
        _Metallic("Metallic", Range(0,1)) = 0
        _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _HeightScale;
                half _Smoothness, _Metallic;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half4 color : COLOR; half fog : TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.color = v.color;
                o.fog = ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings v) : SV_Target
            {
                float3 n = normalize(v.normalWS);
                float height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, v.uv).r * _HeightScale;
                float3 dx = ddx(v.positionWS), dy = ddy(v.positionWS);
                float3 r1 = cross(dy, n), r2 = cross(n, dx);
                float determinant = dot(dx, r1);
                n = normalize(abs(determinant) * n - sign(determinant) * (ddx(height) * r1 + ddy(height) * r2));
                InputData input = (InputData)0;
                input.positionWS = v.positionWS; input.normalWS = n;
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
                input.shadowCoord = TransformWorldToShadowCoord(v.positionWS);
                input.bakedGI = SampleSH(n); input.shadowMask = half4(1,1,1,1);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, v.uv).rgb * v.color.rgb * _BaseColor.rgb;
                surface.metallic = _Metallic; surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1); surface.occlusion = 1; surface.alpha = 1;
                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, v.fog); return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
