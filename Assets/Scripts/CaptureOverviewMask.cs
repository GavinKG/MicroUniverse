using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MicroUniverse {

    [RequireComponent(typeof(Camera))]
    public class CaptureOverviewMask : MonoBehaviour {

        public Texture2D Capture(FilterMode filterMode, int sceneWH, int resolution) {
            Camera orthoCam = GetComponent<Camera>();
            orthoCam.orthographicSize = sceneWH / 2;
            orthoCam.enabled = false;
            RenderTexture rt = new RenderTexture(resolution, resolution, 0); // get a temp rt
            orthoCam.targetTexture = rt;
            orthoCam.Render(); // render with replacement material assigned in ForwardRendererMaskReplacement
            Texture2D captured = Util.RT2Tex(rt, TextureFormat.RGBA32, filterMode);
            orthoCam.targetTexture = null;

            rt.Release();
            return captured;
        }
    }

}

