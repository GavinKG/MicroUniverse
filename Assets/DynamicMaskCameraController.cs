using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    [RequireComponent(typeof(Camera))]
    public class DynamicMaskCameraController : MonoBehaviour {
        
        public int resDownRate = 2;

        Camera cam;
        RenderTexture rt;

        // Start is called before the first frame update
        void Start() {
            int width = Screen.width / resDownRate;
            int height = Screen.height / resDownRate;
            rt = new RenderTexture(width, height, 0, RenderTextureFormat.R8);
            rt.width = width;
            rt.height = height;
            cam = GetComponent<Camera>();
            cam.targetTexture = rt;
            Shader.SetGlobalTexture("DarkMaskTex", rt);
        }

        // Update is called once per frame
        void Update() {
            Camera mainCam = Camera.main;
            cam.fieldOfView = mainCam.fieldOfView;
        }

        void OnDestroy() {
            rt.Release();
        }
    }

}