using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MicroUniverse {

    [RequireComponent(typeof(Camera))]
    public class CaptureOverviewMask : MonoBehaviour {

        public Shader replacementShader;
        public int textureWH = 128;

        public Texture2D Capture() {
            Camera orthoCam = GetComponent<Camera>();
            orthoCam.enabled = false;
            RenderTexture rt = new RenderTexture(textureWH, textureWH, 0); // get a temp rt
            orthoCam.targetTexture = rt;
            orthoCam.RenderWithShader(replacementShader, "RenderType");
            Texture2D captured = Util.RT2Tex(rt);
            rt.Release();
            orthoCam.targetTexture = null;
            print("Captured!");
            return captured;
        }
    }

}

