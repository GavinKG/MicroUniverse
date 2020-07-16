using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public static class GaussianBlur {

        public static Texture2D Blur(Texture src, int downSampleNum, float blurSpreadSize, int blurIterations) {

            Shader shader = Shader.Find("MicroUniverse/GuassianBlur");
            if (shader == null) {
                throw new System.Exception("Guassian blur shader not found.");
            }
            Material material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;

            float widthMod = 1.0f / (1.0f * (1 << downSampleNum));
            material.SetFloat("_DownSampleValue", blurSpreadSize * widthMod);

            int renderWidth = src.width >> downSampleNum;
            int renderHeight = src.height >> downSampleNum;

            // pass 0: downsample
            RenderTexture rt = RenderTexture.GetTemporary(renderWidth, renderHeight, 0);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, rt, material, 0);

            for (int i = 0; i < blurIterations; ++i) {
                float iterationOffs = (i * 1.0f);
                material.SetFloat("_DownSampleValue", blurSpreadSize * widthMod + iterationOffs);

                // pass 1: vertical blur
                RenderTexture rt1 = RenderTexture.GetTemporary(renderWidth, renderHeight, 0);
                Graphics.Blit(rt, rt1, material, 1);
                RenderTexture.ReleaseTemporary(rt);
                rt = rt1;

                // pass 2: horizontal blur
                rt1 = RenderTexture.GetTemporary(renderWidth, renderHeight, 0);
                Graphics.Blit(rt, rt1, material, 2);
                RenderTexture.ReleaseTemporary(rt);
                rt = rt1;
            }

            Texture2D ret = Util.RT2Tex(rt);
            RenderTexture.ReleaseTemporary(rt);

            return ret;

        }

    }

}