using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class ASCameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "Render Camera";
    CullingResults cullingResults;
    CommandBuffer cmd = new CommandBuffer
    {
        name = bufferName
    };//using cmd to do more commands
    static ShaderTagId unlitShaderTag = new ShaderTagId("SRPDefaultUnlit");
    public void Render(ScriptableRenderContext context,Camera camera)//The core Render Function
    {
        this.camera = camera;
        this.context = context;
        if (!Cull())
            return;
        Setup();
        DrawGeometry();
        Submit();
    }
    void Setup()
    {
        context.SetupCameraProperties(camera);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.BeginSample(bufferName);
        ExecuteCommandBuffer();
        
    }
    void DrawGeometry()
    {
        context.DrawSkybox(camera);

        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTag,sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);//Seperate the Transparent and Opaque Render
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    void Submit()
    {
        cmd.EndSample(bufferName);
        ExecuteCommandBuffer();
        context.Submit();
    }
    void ExecuteCommandBuffer()
    {
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
        {
            cullingResults = context.Cull(ref cullingParameters);//To Avoid More Memory Allocation using ref
            return true;
        }
        return false;
    }
}
