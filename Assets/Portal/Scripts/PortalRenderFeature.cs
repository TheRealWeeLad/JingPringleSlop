using UnityEngine.Rendering.Universal;

public class PortalRenderFeature : ScriptableRendererFeature
{
    PortalRenderPass renderPass;

    public override void Create()
    {
        renderPass = new() { renderPassEvent = RenderPassEvent.BeforeRendering };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // renderer.EnqueuePass(renderPass); SO MUCH WORK FOR NOTHING!!
    }
}