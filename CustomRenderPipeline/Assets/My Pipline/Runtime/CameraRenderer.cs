using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

 public partial class CameraRenderer
{
	const string bufferName = "Render Camera";
	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	ScriptableRenderContext context;
	Camera camera;
	CommandBuffer buffer = new CommandBuffer()
	{
		name = bufferName,
	};
	CullingResults cullingResults;

	
	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
	{
		this.context = context;
		this.camera = camera;
		PrepareBuffer();
		PrepareForSceneWindow();
		if (!Cull())
		{
			return;
		}
		this.Setup();
		this.DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmos();
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

	bool Cull()
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}
}
