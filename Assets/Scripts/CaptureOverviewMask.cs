using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MicroUniverse {

    [RequireComponent(typeof(Camera))]
    public class CaptureOverviewMask : MonoBehaviour {
        
        public RenderTexture rt;
        public Shader replacementShader;

        public Texture2D Capture() {
            Camera orthoCam = GetComponent<Camera>();
            orthoCam.RenderWithShader(replacementShader, "");
            Texture2D captured = Util.RT2Tex(rt);
            return captured;
        }
    }

}

