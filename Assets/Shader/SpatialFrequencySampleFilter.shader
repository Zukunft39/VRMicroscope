Shader "UI/Spatial Frequency Sample Filter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ResolutionQuality ("Resolution Quality", Range(0,1)) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _ResolutionQuality;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float quality = saturate(_ResolutionQuality);

                // A finer grating contributes fewer captured diffraction orders. Use a
                // UV-space kernel so the resolution loss remains visible after UI scaling.
                float blurRadius = lerp(0.018, 0.0008, quality);
                float2 offsetX = float2(blurRadius, 0);
                float2 offsetY = float2(0, blurRadius);

                fixed4 source = tex2D(_MainTex, input.texcoord);
                fixed4 blurred = source * 0.20;
                blurred += tex2D(_MainTex, input.texcoord + offsetX) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord - offsetX) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord + offsetY) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord - offsetY) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord + offsetX + offsetY) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord - offsetX + offsetY) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord + offsetX - offsetY) * 0.10;
                blurred += tex2D(_MainTex, input.texcoord - offsetX - offsetY) * 0.10;

                float detailRecovery = smoothstep(0.12, 1.0, quality);
                fixed3 filtered = lerp(blurred.rgb, source.rgb, detailRecovery);
                filtered += (source.rgb - blurred.rgb) * smoothstep(0.55, 1.0, quality) * 0.45;
                filtered *= lerp(0.68, 1.0, quality);

                float sourceBrightness = max(source.r, max(source.g, source.b));
                float preserveBlack = smoothstep(0.012, 0.06, sourceBrightness);
                filtered *= preserveBlack;

                fixed4 color = fixed4(saturate(filtered), source.a) * input.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
