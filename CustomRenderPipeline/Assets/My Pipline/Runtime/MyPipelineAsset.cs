using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/My Pipeline")]
public class MyPipelineAsset : RenderPipelineAsset
{
	[SerializeField]
	bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
	protected override RenderPipeline CreatePipeline()
	{
		return new MyPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher);
	}
}
