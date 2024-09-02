using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class ASRenderPipeline : RenderPipeline
{
    ASCameraRenderer renderer = new ASCameraRenderer();

    public ASRenderPipeline()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            renderer.Render(context, cameras[i]);
        }
    }
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i=0;i<cameras.Count;i++)
        {
            renderer.Render(context, cameras[i]);
        }
    }
}
