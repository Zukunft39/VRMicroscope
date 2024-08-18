Shader "Unlit/OutlineShader"
{
    Properties
    {
        _Color("Color",COLOR) = (0,0,0,1)
        _OutlineWidth("OutlineWidth",float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"  "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            ZWrite On
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
           
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                
            };

            float _OutlineWidth;
            float4 _Color;
            Varyings vert (appdata input)
            {
                
                
                float4 scaledScreenParams = GetScaledScreenParams();//获取屏幕参数
                float ScaleX = abs(scaledScreenParams.x / scaledScreenParams.y);//这里获取屏幕宽高比
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float3 normalCS = TransformWorldToHClipDir(normalInput.normalWS);
                output.positionCS = vertexInput.positionCS;
                
                output.positionCS.xy += normalCS.xy*_OutlineWidth*output.positionCS.w*0.001/ScaleX ;
                float clampW = clamp(1/output.positionCS.w,0.7,1)   ;  
                output.positionCS.xy += normalCS.xy*_OutlineWidth*clampW*0.001/ScaleX*input.color.r ;
                
                return output;
            }

            float4 frag (Varyings i) : SV_Target
            {

                return _Color;
            }
            ENDHLSL
        }
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
           
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                
            };

            float _OutlineWidth;
            float4 _Color;
            Varyings vert (appdata input)
            {
                
                
                float4 scaledScreenParams = GetScaledScreenParams();//获取屏幕参数
                float ScaleX = abs(scaledScreenParams.x / scaledScreenParams.y);//这里获取屏幕宽高比
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float3 normalCS = TransformWorldToHClipDir(normalInput.normalWS);
                output.positionCS = vertexInput.positionCS;
                
                
                return output;
            }

            float4 frag (Varyings i) : SV_Target
            {

                return float4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}
