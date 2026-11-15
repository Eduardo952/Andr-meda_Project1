Shader "ARES/Reference Shield"
{
    Properties { _BaseColor("Shield color",Color)=(.38,.66,.9,1) _Phase("Animation time",Float)=0 _Storm("Storm envelope",Range(0,1))=0 _StormTime("Storm time",Float)=0 _ShieldIntegrity("Integrity",Range(0,1))=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; float _Phase, _Storm, _StormTime, _ShieldIntegrity;
            CBUFFER_END
            struct Attributes { float3 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 viewWS:TEXCOORD1; float height:TEXCOORD2; float3 positionWS:TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings o; float3 world=TransformObjectToWorld(input.positionOS);
                o.positionCS=TransformWorldToHClip(world); o.normalWS=TransformObjectToWorldNormal(input.normalOS);
                o.viewWS=GetWorldSpaceViewDir(world); o.height=input.positionOS.y+4; o.positionWS=world; return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // Normalization may overshoot by an ulp; keep fractional pow away from negative bases.
                float rim=pow(saturate(1-abs(dot(normalize(input.normalWS),normalize(input.viewWS)))),2.6);
                float band=.5+.5*sin(input.height*.35-_Phase*.8);
                float3 sphereNormal=normalize(input.normalWS);
                float2 p=float2(atan2(sphereNormal.x,sphereNormal.z),acos(clamp(sphereNormal.y,-.999,.999)))*16;
                const float2 cell=float2(1,1.73205);
                float2 a=frac(p/cell)-.5,b=frac((p-cell*.5)/cell)-.5;
                a*=cell;b*=cell;float2 q=dot(a,a)<dot(b,b)?a:b;
                float edge=max(abs(q.x),dot(abs(q),normalize(float2(1,1.73205))));
                float hex=smoothstep(.43-fwidth(edge)*1.5,.49,edge);
                float wave=pow(.5+.5*sin(input.height*.55-_StormTime*2),7);
                float3 normal=normalize(input.normalWS);
                float3 impact=normalize(float3(sin(_StormTime*.31),.8,cos(_StormTime*.31)));
                float distance=acos(clamp(dot(normal,impact),-.999,.999));
                float pulse=pow(.5+.5*sin(distance*20-_StormTime*3),10)*exp(-distance*3);
                float energy=_Storm*(hex*.25+wave*.24+pulse*.9);
                half3 stormColor=lerp(half3(1,.47,.14),half3(1,.18,.10),1-_ShieldIntegrity);
                half3 color=lerp(_BaseColor.rgb,stormColor,saturate(_Storm*(.5+wave+pulse)));
                return half4(color+energy*.3,clamp(rim*rim*.36+band*rim*.05+energy,0,.62));
            }
            ENDHLSL
        }
    }
}
