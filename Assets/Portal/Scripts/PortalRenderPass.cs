using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PortalRenderPass : ScriptableRenderPass
{
    public static readonly Portal[] portals = new Portal[2];

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!Application.isPlaying) return;

        for (int i = 0; i < portals.Length; i++)
        {
            Portal portal = portals[i];
            if (portal != null && portal.IsLinked())
            {
                // Update Camera Texture and Position
                portal.Render();

                // Render Camera
                Camera cam = portal.portalCam;
                cam.enabled = true;

                SortingSettings sortingSettings = new(cam);
                DrawingSettings drawingSettings = new(new ShaderTagId("SRPDefaultUnlit"), sortingSettings);
                FilteringSettings filteringSettings = new(RenderQueueRange.opaque);

                context.SetupCameraProperties(portal.portalCam);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                // context.DrawSkybox(cam);
                context.Submit();

                cam.enabled = false;
            }
        }
    }
}
