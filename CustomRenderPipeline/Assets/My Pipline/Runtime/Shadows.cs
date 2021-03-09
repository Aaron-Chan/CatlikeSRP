using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{


	const string bufferName = "Shadows";
	CommandBuffer buffer = new CommandBuffer()
	{
		name = bufferName
	};


	const int maxShadowedDirectionalLightCount = 4;

	struct ShadowedDirectionalLight
	{
		public int visibleLightIndex;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights =
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];


	private ScriptableRenderContext context;
	private CullingResults cullingResults;
	private ShadowSettings shadowSettings;
	int ShadowedDirectionalLightCount;

	static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");


	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings) {

		this.context = context;
		this.cullingResults = cullingResults;
		this.shadowSettings = shadowSettings;
		ShadowedDirectionalLightCount = 0;
	}

	void ExecuteBuffer() {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	public void ReserveDirectionalShadows(Light light, int visibleLightIndex) {
		if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
			&& light.shadowStrength > 0f && light.shadows != LightShadows.None
			&& cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b //返回在灯光区域内是否有投射阴影的物体
			)) {

			ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight()
			{
				visibleLightIndex = visibleLightIndex
			};
			ShadowedDirectionalLightCount++;
		}
	}

	public void Render()
	{
		if (ShadowedDirectionalLightCount > 0)
		{
			RenderDirectionalShadows();
		}
		else {
			//默认生成1像素的图
			buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		}
		
	}

	private void RenderDirectionalShadows()
	{
		
		int atlasSize = (int)this.shadowSettings.directional.atlasSize;
		int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;
		int tileSize = atlasSize / split;


		buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

		//让gpu知道有这个rt
		buffer.SetRenderTarget(
			dirShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.BeginSample(bufferName);
		ExecuteBuffer();

		for (int i = 0; i < ShadowedDirectionalLightCount; i++)
		{
			RenderDirectionalShadows(i, split, tileSize);
		}
		buffer.EndSample(bufferName);
		ExecuteBuffer();

	}

	void SetTileViewport(int index, int split, float tileSize)
	{
		Vector2 offset = new Vector2(index % split, index / split);
		buffer.SetViewport(new Rect(
			offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
		));
	}

	private void RenderDirectionalShadows(int index,int split,  int tileSize)
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
		ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

		cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData
		);
		//The split data contains information about how shadow-casting objects should be culled, which we have to copy to the shadow settings.
		shadowDrawingSettings.splitData = splitData;
		buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
		ExecuteBuffer();
		SetTileViewport(index, split, tileSize);
		context.DrawShadows(ref shadowDrawingSettings);
	}

	public void Cleanup()
	{
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}

}
