using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class ASCameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    public void Render(ScriptableRenderContext context,Camera camera)
    {
        this.camera = camera;
        this.context = context;

        context.DrawSkybox(camera);
        context.Submit();
    }
}
