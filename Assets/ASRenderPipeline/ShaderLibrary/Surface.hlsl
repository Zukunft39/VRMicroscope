#ifndef ASRP_SURFACE_INCLUDED
#define ASRP_SURFACE_INCLUDED

struct Surface {
	float3 position;
	float3 normal;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float3 viewDirection;
};

#endif