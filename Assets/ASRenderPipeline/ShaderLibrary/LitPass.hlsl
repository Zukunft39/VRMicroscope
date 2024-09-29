#ifndef ASRP_LIT_PASS_INCLUDED
#define ASRP_LIT_PASS_INCLUDED
#include "Common.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GI.hlsl"
#include "Lighting.hlsl"


struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float3 normalOS : NORMAL;
	GI_ATTRIBUTE_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings {
	float4 positionCS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float3 normalWS : VAR_NORMAL;
	GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
Varyings LitPassVertex(Attributes input){
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	TRANSFER_GI_DATA(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	
	output.baseUV = TransformBaseUV(input.baseUV);
	
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 base = GetBase(input.baseUV);
#if defined(_SHADOWS_CLIP)
	clip(base.a - GetCutoff(input.baseUV));
#elif defined(_SHADOWS_DITHER)
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(base.a - dither);
#endif
	Surface surface;
	surface.position = input.positionWS;
	surface.normal = normalize(input.normalWS);
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(input.baseUV);
	surface.smoothness = GetSmoothness(input.baseUV);

	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif
	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);
	float3 color = GetLighting(surface,brdf,gi);
	return float4(color, surface.alpha);
}

#endif