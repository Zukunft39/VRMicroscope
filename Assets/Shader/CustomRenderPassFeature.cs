using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private List<ShaderTagId> list = new List<ShaderTagId>()
        {
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("LightweightForward"),
            new ShaderTagId("UniversalForward")
        };
        private FilteringSettings filteringSettings;
        private RenderQueue m_renderQueue;
        private Material m_Mat;
        private int m_MatPass;
        private RenderStateBlock m_RenderStateBlock;
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public CustomRenderPass(RenderQueue renderQueue,LayerMask layerMask,int renderingLayerMask,Material mat,int matPass,RenderPassEvent passEvent)
        {
            m_Mat = mat;
            m_MatPass = matPass;
            m_renderQueue = renderQueue;
            this.renderPassEvent = passEvent;
            var renderQueueRange = (renderQueue == RenderQueue.Transparent) ? RenderQueueRange.transparent : RenderQueueRange.opaque;
            uint uintRenderingLayerMask = (uint)1<<renderingLayerMask;
            filteringSettings = new FilteringSettings(renderQueueRange,layerMask,uintRenderingLayerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }
        
        public void SetDepthState(bool writeEnabled,CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }
      
       
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool isOpaque = (m_renderQueue != RenderQueue.Transparent);
            SortingCriteria sortingCriteria = (isOpaque) ? renderingData.cameraData.defaultOpaqueSortFlags:SortingCriteria.CommonTransparent;
            var drawingSettings = CreateDrawingSettings(list,ref renderingData,sortingCriteria);
            drawingSettings.overrideMaterial = m_Mat;
            drawingSettings.overrideMaterialPassIndex = m_MatPass;
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings,ref m_RenderStateBlock);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;
    public RenderQueue queue;
    public LayerMask layer;
    [Range(0,31)]public int renderingLayerMask;
    public Material overrideMat;
    public int[] overrideMaterialPassIndex;
    public RenderPassEvent renderPassEvent;
    private List<ScriptableRenderPass> renderPasses = new List<ScriptableRenderPass>(2);
    public bool depth;
    public bool depthWrite;
    public CompareFunction compare;
    /// <inheritdoc/>
    public override void Create()
    {
        renderPasses.Clear();
        for (int i=0;i<overrideMaterialPassIndex.Length;i++)
        {
            var onePass = new CustomRenderPass(queue, layer, renderingLayerMask, overrideMat, overrideMaterialPassIndex[i], renderPassEvent);
           
            onePass.renderPassEvent = renderPassEvent;
            if (depth)
            {
                onePass.SetDepthState(depthWrite,compare);
            }
            renderPasses.Add(onePass);
        }
    }
    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        foreach(var pass in renderPasses)
        {
            renderer.EnqueuePass(pass);
        }
    }
}


