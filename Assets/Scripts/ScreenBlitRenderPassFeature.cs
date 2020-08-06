using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Screen Blit implmentation for URP

public class ScreenBlitRenderPassFeature : ScriptableRendererFeature {

    class ScreenBlitRenderPass : ScriptableRenderPass {
        ScreenBlitRPSettings _CustomRPSettings;
        RenderTargetHandle _TemporaryColorTexture;
        private RenderTargetIdentifier _Source;
        private RenderTargetHandle _Destination;
        public ScreenBlitRenderPass(ScreenBlitRPSettings settings) {
            _CustomRPSettings = settings;
        }
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination) {
            _Source = source;
            _Destination = destination;
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _TemporaryColorTexture.Init("_TemporaryColorTexture");
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get("Blit pass");
            if (_Destination == RenderTargetHandle.CameraTarget) {
                cmd.GetTemporaryRT(_TemporaryColorTexture.id, renderingData.cameraData.cameraTargetDescriptor, FilterMode.Point);
                cmd.Blit(_Source, _TemporaryColorTexture.Identifier()); // screen -> temp
                cmd.Blit(_TemporaryColorTexture.Identifier(), _Source, _CustomRPSettings.m_Material); // temp -> screen (To apply effect)
            } else {
                cmd.Blit(_Source, _Destination.Identifier(), _CustomRPSettings.m_Material, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public override void FrameCleanup(CommandBuffer cmd) {
            if (_Destination == RenderTargetHandle.CameraTarget) {
                cmd.ReleaseTemporaryRT(_TemporaryColorTexture.id);
            }
        }
    }
    [System.Serializable]
    public class ScreenBlitRPSettings {
        public Material m_Material;
        public RenderPassEvent blitPoint = RenderPassEvent.AfterRenderingPostProcessing;
    }
    public ScreenBlitRPSettings m_ScreenBlitSettings = new ScreenBlitRPSettings();
    ScreenBlitRenderPass _ScriptablePass;
    public override void Create() {
        _ScriptablePass = new ScreenBlitRenderPass(m_ScreenBlitSettings);
        _ScriptablePass.renderPassEvent = m_ScreenBlitSettings.blitPoint;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        _ScriptablePass.Setup(renderer.cameraColorTarget, RenderTargetHandle.CameraTarget);
        renderer.EnqueuePass(_ScriptablePass);
    }
}