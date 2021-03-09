using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

 public partial class CameraRenderer
{
	const string bufferName = "Render Camera";
	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

	Lighting lighting = new Lighting();
	ScriptableRenderContext context;
	Camera camera;
	CommandBuffer buffer = new CommandBuffer()
	{
		name = bufferName,
	};
	CullingResults cullingResults;
	ShadowSettings shadowSettings;


	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;
		this.shadowSettings = shadowSettings;
		PrepareBuffer();
		PrepareForSceneWindow();
		if (!Cull(shadowSettings.maxDistance))
		{
			return;
		}
		//在绘制物体前先绘制shadow
		//在这里sample是为了让shadow的层级在camera下
		buffer.BeginSample(bufferName);
		ExecuteBuffer();
		lighting.Setup(context, cullingResults, shadowSettings);
		this.Setup();
		buffer.EndSample(bufferName);
	

		this.DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmos();
		//在提交前清理lighting
		lighting.Cleanup();
		this.Submit();
	}

	

	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		var sortingSettings = new SortingSettings(camera) {
			criteria= SortingCriteria.CommonOpaque
		};
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		)
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, litShaderTagId);

		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		this.context.DrawSkybox(camera);

		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	void Setup() {
		context.SetupCameraProperties(camera);
		
		buffer.ClearRenderTarget(camera.clearFlags<=CameraClearFlags.Depth, camera.clearFlags==CameraClearFlags.Color, 
			camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor.linear:Color.clear);
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void Submit()
	{
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
	}

	void ExecuteBuffer() {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	bool Cull(float maxShadowDistance)
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			p.shadowDistance = Mathf.Min(camera.farClipPlane, maxShadowDistance);
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}
}
