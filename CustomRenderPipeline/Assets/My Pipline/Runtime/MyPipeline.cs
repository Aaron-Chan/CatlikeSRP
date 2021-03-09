using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MyPipeline : RenderPipeline
{
	bool useDynamicBatching, useGPUInstancing;
	ShadowSettings shadowSettings;

	public MyPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings) {
		
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.shadowSettings = shadowSettings;
		GraphicsSettings.lightsUseLinearIntensity = true;
	}

	CameraRenderer renderer = new CameraRenderer();

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		foreach (Camera camera in cameras)
		{
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);
		}
	}
}
