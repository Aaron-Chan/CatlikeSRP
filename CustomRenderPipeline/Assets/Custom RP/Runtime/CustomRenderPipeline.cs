using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline {

	CameraRenderer renderer = new CameraRenderer();

	bool useDynamicBatching, useGPUInstancing;

	ShadowSettings shadowSettings;
	PostFXSettings postFXSettings;

	public CustomRenderPipeline (
		bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
		ShadowSettings shadowSettings, PostFXSettings postFXSettings
	) {
		this.shadowSettings = shadowSettings;
		this.postFXSettings = postFXSettings;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
		GraphicsSettings.lightsUseLinearIntensity = true;
	}

	protected override void Render (
		ScriptableRenderContext context, Camera[] cameras
	) {
		foreach (Camera camera in cameras) {
			renderer.Render(
				context, camera, useDynamicBatching, useGPUInstancing,
				shadowSettings, postFXSettings
			);
		}
	}
}