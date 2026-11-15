Shader "ARES/Celestial"
{
    Properties
    {
        _BaseColor("Color",Color)=(1,1,1,1)
        _Surface("Surface",Float)=1 _ZWrite("Depth",Float)=0
        _SrcBlend("Source",Float)=5 _DstBlend("Destination",Float)=10
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            float4 Vert(float3 positionOS:POSITION):SV_POSITION{return TransformObjectToHClip(positionOS);}
            half4 Frag():SV_Target{clip(.5-unity_OrthoParams.w);return _BaseColor;}
            ENDHLSL
        }
    }
}
