Shader "Custom/OutlineShader"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.xyz += normalWS * _OutlineWidth;
                output.positionCS = TransformWorldToHClip(positionWS.xyz);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
