using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(menuName ="Rendering/ASRenderPipeline")]
public class ASRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = false;
    [SerializeField]
    bool useGPUInstancing = false;
    [SerializeField]
    bool useSRPBatching = false;
    [SerializeField]
    ASShadowSettings shadows = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new ASRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatching,shadows);
    }
}
