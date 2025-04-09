Shader "Semitransparent Shadows" 
{
    Properties 
    {
        _Color ("Main Color", Color) = (1,1,1,0.5)
        [Toggle(_Reverse)] _Reverse("Reverse", Float) = 0
        _MainTex ("Base", 2D) = "white" {}
        _Alpha("Alpha",Range(0,5))=1
        _Intensity("Intensity",Range(0,5))=1
        _ShadowIntensity("ShadowIntensity",Range(0,5))=1
        [Header(Global Variables)]
        _TAA_Jitter("_TAA_Jitter",Vector)=(0,0,0,0)
        _Power("MaskPower",Range(0.1,3))=1
        _Mask("Mask",2D)="white"{}
    }

    SubShader 
    {
        Tags 
        { 
            "Queue" = "Transparent"       
            "RenderType" = "Transparent"  
            "IgnoreProjector" = "True"    
        }

        LOD 200

        Pass 
        {
            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off                      
            Cull Off
            HLSLPROGRAM
            #pragma multi_compile __ _Reverse
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct appdata 
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Power;
            float _Alpha;
            sampler2D _Mask;
            float _Intensity;
            v2f vert (appdata v) 
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, v.vertex)); 
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);  
                return o;
            }

            float4 frag (v2f i) : SV_Target 
            {
                float4 texColor = tex2D(_MainTex, i.uv);
                float mask= pow(max(0,tex2D(_Mask, i.uv).a),_Power);
                texColor*=mask;
                float4 finalColor=texColor * _Color;
                finalColor=float4(finalColor.rgb,finalColor.a*_Alpha*_Intensity);
                return finalColor;             
            }
            ENDHLSL
        }
        Pass
        {
            Tags{"LightMode" = "ShadowCaster"}
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };
                float3 _LightDirection;
                float3 _LightPosition;
                float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }
            float4 _MainTex_ST;
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex); 
            float4 _TAA_Jitter;
            float _PCF_Softness;
            float _Power;
            sampler2D _Mask;
            uniform float4x4 _PCF_RotationMatrix;
            float _Alpha;
            float _ShadowIntensity;
            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                float alpha=SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)).a;
                alpha=alpha*_ShadowIntensity*_Alpha;
                float ditherMatrix[64] =
                { 
                    0.01587f, 0.50794f, 0.12698f, 0.63492f, 0.03175f, 0.53968f, 0.15873f, 0.66667f,
                    0.76190f, 0.25397f, 0.88889f, 0.38095f, 0.79365f, 0.28571f, 0.92063f, 0.41270f,
                    0.19048f, 0.69841f, 0.06349f, 0.57143f, 0.22222f, 0.73016f, 0.09524f, 0.60317f,
                    0.95238f, 0.44444f, 0.82540f, 0.31746f, 0.98413f, 0.47619f, 0.85714f, 0.34921f,
                    0.04762f, 0.55556f, 0.17460f, 0.68254f, 0.01587f, 0.52381f, 0.14286f, 0.65079f,
                    0.80952f, 0.30159f, 0.93651f, 0.42857f, 0.77778f, 0.26984f, 0.90476f, 0.39683f,
                    0.23810f, 0.74603f, 0.11111f, 0.61905f, 0.20635f, 0.71429f, 0.07937f, 0.58730f,
                    1.00000f, 0.49206f, 0.87302f, 0.36508f, 0.96825f, 0.46032f, 0.84127f, 0.33333f
                };
                float mask= pow(max(0,tex2D(_Mask, input.uv).a),_Power);
                alpha*=mask;
                
                float2 ScreenUV = GetNormalizedScreenSpaceUV(input.positionCS) * _ScreenParams.xy;
                ScreenUV +=  (_ScreenParams.xy * 16*_TAA_Jitter.xy);
                uint index = (uint(ScreenUV.x) % 8) * 8 + uint(ScreenUV.y) % 8;
                clip( alpha - ditherMatrix[index]);
                return 0;
            }
            ENDHLSL
         }
    }
    FallBack "Diffuse"
}