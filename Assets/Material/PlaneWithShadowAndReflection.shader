Shader "URP/SpecularReflective"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _BaseMap("Base Map", 2D) = "white" {}
        _SpecularMap("Specular Map", 2D) = "white" {}
        _ReflectionTex("Reflection Texture", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _ReflectionIntensity("Reflection Intensity", Range(0,5)) = 1
    }

    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
            "LightMode"="UniversalForward"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;
                float4 screenPos    : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            TEXTURE2D(_SpecularMap);
            TEXTURE2D(_SSPRReflectionTexture);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Smoothness;
                float _ReflectionIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.positionWS = vertexInput.positionWS;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 基础材质采样
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half4 specMap = SAMPLE_TEXTURE2D(_SpecularMap, sampler_BaseMap, input.uv);

                // 镜面反射采样（使用屏幕空间坐标）
                float2 reflectionUV = input.screenPos.xy / input.screenPos.w;
                half4 reflection = SAMPLE_TEXTURE2D(_SSPRReflectionTexture, sampler_BaseMap, reflectionUV);

                // 光照计算
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // 漫反射
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * baseColor.rgb * NdotL;

                // Blinn-Phong高光
                float3 halfDir = normalize(mainLight.direction + viewDir);
                half specPower = exp2(10 * _Smoothness + 1);
                half specTerm = pow(saturate(dot(normalWS, halfDir)), specPower);
                half3 specular = mainLight.color * specTerm * specMap.rgb;

                // 反射合成
                half3 reflectionColor = reflection.rgb * _ReflectionIntensity * specMap.a;

                // 阴影计算
                half shadow = mainLight.shadowAttenuation;

                // 最终颜色混合
                half3 finalColor = specular+diffuse* shadow + reflectionColor;
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}