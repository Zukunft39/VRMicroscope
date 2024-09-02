using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class ASRenderPipeline : RenderPipeline
{
    ASCameraRenderer renderer = new ASCameraRenderer();
    bool useDynamicBatching, useGPUInstancing;
    public ASRenderPipeline(bool useDynamicBatching,bool useGPUInstancing,bool useSRPBatching)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            renderer.Render(context, cameras[i],useDynamicBatching,useGPUInstancing);
        }
    }
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i=0;i<cameras.Count;i++)
        {
            renderer.Render(context, cameras[i],useDynamicBatching,useGPUInstancing);
        }
    }
}
