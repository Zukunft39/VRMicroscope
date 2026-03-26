Shader "TextMeshPro/SDF-AlwaysOnTop-VR"
{
    Properties
    {
        _MainTex            ("Font Atlas", 2D) = "white" {}
        _FaceColor          ("Face Color (Base)", Color) = (1,1,1,1)
        _FaceDilate         ("Face Dilate", Range(-1,1)) = 0
        
        _OutlineColor       ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth       ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness    ("Outline Softness", Range(0,1)) = 0
        
        _WeightNormal       ("Weight Normal", float) = 0
        _WeightBold         ("Weight Bold", float) = 0.75

        _StencilComp        ("Stencil Comparison", Float) = 8
        _Stencil            ("Stencil ID", Float) = 0
        _StencilOp          ("Stencil Operation", Float) = 0
        _StencilWriteMask   ("Stencil Write Mask", Float) = 255
        _StencilReadMask    ("Stencil Read Mask", Float) = 255
        _ColorMask          ("Color Mask", Float) = 15

        _GradientScale      ("Gradient Scale (Sharpness)", float) = 1.0
        _VertexOffset       ("Vertex Offset", float) = 0
        _ScaleRatioA        ("Scale RatioA", float) = 0
        _ClipRect           ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always 
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _FaceColor;
            float _FaceDilate;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _WeightNormal;
            float _WeightBold;
            float _GradientScale;
            float _VertexOffset;
            float _ScaleRatioA;
            float4 _ClipRect;

            struct appdata_t {
                float4 vertex       : POSITION;
                fixed4 color        : COLOR;
                float2 texcoord0    : TEXCOORD0;
                
                // 【VR修复 1】：获取实例 ID（告诉系统这是左眼还是右眼的数据）
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f {
                float4  vertex          : SV_POSITION;
                fixed4  color           : COLOR;
                float2  texcoord0       : TEXCOORD0;
                #if UNITY_UI_CLIP_RECT
                float4  mask            : TEXCOORD1;
                #endif
                
                // 【VR修复 2】：声明立体渲染输出（将画面输出到对应的眼睛）
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            v2f vert (appdata_t i)
            {
                v2f o;
                
                // 【VR修复 3】：设置实例 ID（初始化双眼偏移计算）
                UNITY_SETUP_INSTANCE_ID(i); 
                // 【VR修复 4】：初始化立体输出阶段
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 

                float4 v = i.vertex;
                v.y += _VertexOffset;

                o.vertex = UnityObjectToClipPos(v);
                o.texcoord0 = i.texcoord0;
                o.color = i.color;

                #if UNITY_UI_CLIP_RECT
                o.mask = i.vertex.xyxy;
                #endif

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float sd = tex2D(_MainTex, i.color.a > 0 ? i.texcoord0 : i.texcoord0).a;

                #if UNITY_UI_CLIP_RECT
                i.color.a *= UnityGet2DClipping(i.mask.xy, _ClipRect);
                #endif

                float weight = i.color.a > 0 ? _WeightNormal : _WeightBold;
                float face = sd - (_FaceDilate * _ScaleRatioA * 0.5 + 0.5) + (weight * 0.25);
                
                // 强制硬边抗锯齿逻辑
                float af = max(0.0001, (fwidth(sd) * _GradientScale * 0.5) - 0.005);

                float alpha = smoothstep(-af, af, face);

                // 颜色叠加
                fixed4 col = _FaceColor * i.color; 
                col.a *= alpha; 

                if (_OutlineWidth > 0)
                {
                    float outline = sd - (0.5 - _OutlineWidth * _ScaleRatioA);
                    float outlineAlpha = smoothstep(-af, af, outline);
                    col = lerp(_OutlineColor, col, alpha);
                    col.a = max(col.a, outlineAlpha * _OutlineColor.a * i.color.a);
                }

                return col;
            }
            ENDCG
        }
    }
}