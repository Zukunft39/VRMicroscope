#ifndef ASRP_LIGHTING_INCLUDED
#define ASRP_LIGHTING_INCLUDED

float3 IncomingLight(Surface surface, Light light) {
	return saturate(dot(surface.normal, light.direction)) * light.color;
}
float3 GetLighting(Surface surface, Light light, BRDF brdf) {
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}
float3 GetLighting(Surface surface, BRDF brdf) {
	float3 color = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		color += GetLighting(surface,GetDirectionalLight(i),brdf);
	}
	return color;
}


#endif