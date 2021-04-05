using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {

	const string bufferName = "Render Camera";

	static ShaderTagId
		unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
		litShaderTagId = new ShaderTagId("CustomLit");

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	ScriptableRenderContext context;

	Camera camera;

	CullingResults cullingResults;

	Lighting lighting = new Lighting();
	PostFXStack postFXStack = new PostFXStack();

	static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

	public void Render (
		ScriptableRenderContext context, Camera camera,
		bool useDynamicBatching, bool useGPUInstancing,
		ShadowSettings shadowSettings, PostFXSettings postFXSettings
	) {
		this.context = context;
		this.camera = camera;

		PrepareBuffer();
		PrepareForSceneWindow();
		if (!Cull(shadowSettings.maxDistance)) {
			return;
		}
		
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
		lighting.Setup(context, cullingResults, shadowSettings);
		postFXStack.Setup(context, camera, postFXSettings);

		buffer.EndSample(SampleName);
		Setup();
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmosBeforeFX();

		//开启绘制
		if (postFXStack.IsActive)
		{
			postFXStack.Render(frameBufferId);
		}
		DrawGizmosAfterFX();

		Cleanup();
		Submit();
	}

	bool Cull (float maxShadowDistance) {
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	void Cleanup()
	{
		lighting.Cleanup();
		if (postFXStack.IsActive)
		{
			buffer.ReleaseTemporaryRT(frameBufferId);
		}
	}

	void Setup () {
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;

		if (postFXStack.IsActive)
		{
			//因为当前渲染的渲染的rt是这个framebuffer 所以需要清除数据
			if (flags > CameraClearFlags.Color)
			{
				flags = CameraClearFlags.Color;
			}
			buffer.GetTemporaryRT(
				frameBufferId, camera.pixelWidth, camera.pixelHeight,
				32, FilterMode.Bilinear, RenderTextureFormat.Default
			);
			buffer.SetRenderTarget(
				frameBufferId,
				RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
			);
		}

		buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
				camera.backgroundColor.linear : Color.clear
		);
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void Submit () {
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
	}

	void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	void DrawVisibleGeometry (bool useDynamicBatching, bool useGPUInstancing) {
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		) {
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing,
			//将lightmap uv传给gpu
			perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe |
				PerObjectData.LightProbeProxyVolume 
				| PerObjectData.ShadowMask |  PerObjectData.OcclusionProbe |  
				PerObjectData.OcclusionProbeProxyVolume |
				PerObjectData.ReflectionProbes 

		};
		drawingSettings.SetShaderPassName(1, litShaderTagId);

		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		context.DrawSkybox(camera);

		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
}