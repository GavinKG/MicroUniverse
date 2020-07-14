using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MicroUniverse {

    [RequireComponent(typeof(Camera))]
    public class CaptureOverviewMask : MonoBehaviour {
        
        public int textureWH = 128;

        public Texture2D Capture(FilterMode filterMode) {
            Camera orthoCam = GetComponent<Camera>();
            orthoCam.enabled = false;
            RenderTexture rt = new RenderTexture(textureWH, textureWH, 0); // get a temp rt
            orthoCam.targetTexture = rt;
            orthoCam.Render(); // render with replacement material assigned in ForwardRendererMaskReplacement
            Texture2D captured = Util.RT2Tex(rt, TextureFormat.RGBA32, filterMode);
            orthoCam.targetTexture = null;

            rt.Release();
            return captured;
        }
    }

}

