using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class ASLighting
{
	const string bufferName = "Lighting";
	const int maxDirLightCount = 4;
	CullingResults cullingResults;
	ASShadow shadows = new ASShadow();
	CommandBuffer buffer = new CommandBuffer
	{
		name = bufferName
	};
	static int
		dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount];
	public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ASShadowSettings shadowSettings)
	{
		this.cullingResults = cullingResults;
		buffer.BeginSample(bufferName);
		shadows.Setup(context, cullingResults, shadowSettings);
		SetupLights();
		shadows.Render();
		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void SetupDirectionalLight(int index, ref VisibleLight visibleLight) {
		dirLightColors[index] = visibleLight.finalColor;
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}
	void SetupLights()
	{
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		int dirLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++)
		{
			VisibleLight visibleLight = visibleLights[i];
			SetupDirectionalLight(dirLightCount++, ref visibleLight);
			if (dirLightCount >= maxDirLightCount)
			{
				break;
			}
		}

		buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
		buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
	}
}
