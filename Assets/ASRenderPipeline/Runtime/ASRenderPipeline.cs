using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class ASRenderPipeline : RenderPipeline
{
    ASCameraRenderer renderer = new ASCameraRenderer();
    bool useDynamicBatching, useGPUInstancing;
    ASShadowSettings shadowSettings;
    public ASRenderPipeline(bool useDynamicBatching,bool useGPUInstancing,bool useSRPBatching,ASShadowSettings shadowSettings)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowSettings = shadowSettings;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            renderer.Render(context, cameras[i],useDynamicBatching,useGPUInstancing,shadowSettings);
        }
    }
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i=0;i<cameras.Count;i++)
        {
            renderer.Render(context, cameras[i],useDynamicBatching,useGPUInstancing,shadowSettings);
        }
    }
}
