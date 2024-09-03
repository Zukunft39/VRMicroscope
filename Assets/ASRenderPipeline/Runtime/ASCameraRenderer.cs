using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public partial class ASCameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "Render Camera";
    CullingResults cullingResults;
    ASLighting lighting = new ASLighting();
    CommandBuffer cmd = new CommandBuffer
    {
        name = bufferName
    };//using cmd to do more commands
    static ShaderTagId unlitShaderTag = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTag = new ShaderTagId("ASRPLit");

    public void Render(ScriptableRenderContext context,Camera camera,bool useDynamicBatching,bool useGPUInstancing)//The core Render Function
    {
        this.camera = camera;
        this.context = context;
        PrepareBuffer();//In edit mode Prepare for Different Cameras different buffer names
        PrepareForSceneWindow();//To Render UI in Scene View
        if (!Cull())
            return;
        Setup();
        lighting.Setup(context,cullingResults);
        DrawGeometry(useDynamicBatching,useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }
    void Setup()
    {
        CameraClearFlags flags = camera.clearFlags;
        context.SetupCameraProperties(camera);
        cmd.ClearRenderTarget(flags<=CameraClearFlags.Depth, flags <= CameraClearFlags.Color, flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear);
        cmd.BeginSample(SampleName);
        ExecuteCommandBuffer();
        
    }
    void DrawGeometry(bool useDynamicBatching,bool useGPUInstancing)
    {
        context.DrawSkybox(camera);

        var sortingSettings = new SortingSettings(camera) { 
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTag, sortingSettings) { 
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTag);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);//Seperate the Transparent and Opaque Render
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    void Submit()
    {
        cmd.EndSample(SampleName);
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
