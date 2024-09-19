using UnityEngine;
using UnityEngine.Rendering;

public class ASShadow
{

	const string bufferName = "Shadows";

	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};

	const int maxShadowedDirectionalLightCount = 1;//currently only support one directional light shadow

	struct ShadowedDirectionalLight
	{
		public int visibleLightIndex;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights =
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

	ScriptableRenderContext context;

	CullingResults cullingResults;

	ASShadowSettings settings;

	int ShadowedDirectionalLightCount;

	static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
	public void ReserveDirectionalShadows(Light light, int visibleLightIndex) //figure out which directional light get shadow
	{
		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
			light.shadows != LightShadows.None && light.shadowStrength > 0f &&
			cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) //1.Directional light within the max count index 2.light Shadow settings 3.cannot be culled
		{
			ShadowedDirectionalLights[ShadowedDirectionalLightCount++] =
				new ShadowedDirectionalLight
				{
					visibleLightIndex = visibleLightIndex
				};
		}
	}
	public void Setup(
		ScriptableRenderContext context, CullingResults cullingResults,
		ASShadowSettings settings
	)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
		ShadowedDirectionalLightCount = 0;

	}
	public void Render()
	{
		if (ShadowedDirectionalLightCount > 0)
		{
			RenderDirectionalShadows();
		}
	}

	void RenderDirectionalShadows() 
	{
		int atlasSize = (int)settings.directional.atlasSize;
		buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
	}
	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}