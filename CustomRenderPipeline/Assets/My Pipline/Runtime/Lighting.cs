using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
	const string bufferName = "Lighting";
	const int maxDirLightCount = 4;
	static int
		dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirections");
	static Vector4[]
	dirLightColors = new Vector4[maxDirLightCount],
	dirLightDirections = new Vector4[maxDirLightCount];

	CommandBuffer buffer = new CommandBuffer() {
		name = bufferName
	};
	CullingResults cullingResults;
	Shadows shadows = new Shadows();

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
	{
		this.cullingResults = cullingResults;
		shadows.Setup(context, cullingResults, shadowSettings);
		buffer.BeginSample(bufferName);
		SetupLights();
		shadows.Render();
		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
	int dirLightCount = 0;

	private void SetupLights()
	{
		dirLightCount = 0;
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		for (int i = 0; i < visibleLights.Length; i++) {
			VisibleLight visibleLight = visibleLights[i];
			if (visibleLight.lightType == LightType.Directional) {
				SetupDirectionalLight(dirLightCount++, ref visibleLight);
				if (dirLightCount >= maxDirLightCount)
				{
					break;
				}
			}
			
		}
	
		buffer.SetGlobalInt(dirLightCountId, dirLightCount);
		buffer.SetGlobalVectorArray(dirLightColorId, dirLightColors);
		buffer.SetGlobalVectorArray(dirLightDirectionId, dirLightDirections);
	}

	void SetupDirectionalLight(int index, ref VisibleLight visibleLight) {
		if (index < maxDirLightCount) {
			dirLightColors[index] = visibleLight.finalColor;
			dirLightDirections[index] = - visibleLight.localToWorldMatrix.GetColumn(2);
			shadows.ReserveDirectionalShadows(visibleLight.light, index);
		}
	}

	public void Cleanup()
	{
		this.shadows.Cleanup();
	}

}
