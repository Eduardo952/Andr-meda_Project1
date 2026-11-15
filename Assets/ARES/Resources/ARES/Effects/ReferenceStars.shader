Shader "ARES/Reference Stars"
{
    Properties { _Size("Pixels",Float)=1.6 _Opacity("Opacity",Range(0,1))=.05 }
    SubShader
    {
        Tags { "Queue"="Background+1" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Size,_Opacity;
            CBUFFER_END
            struct Varyings {float4 positionCS:SV_POSITION;};
            Varyings Vert(float3 positionOS:POSITION,float2 uv:TEXCOORD0)
            {Varyings o;o.positionCS=TransformObjectToHClip(positionOS);o.positionCS.xy+=uv*_Size/_ScreenParams.xy*o.positionCS.w;return o;}
            half4 Frag(Varyings v):SV_Target {clip(.5-unity_OrthoParams.w);return half4(1,.965,.878,_Opacity);}
            ENDHLSL
        }
    }
}
