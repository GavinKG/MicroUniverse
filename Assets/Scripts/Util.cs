using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public static class Util {

        public static Texture2D RT2Tex(RenderTexture rt, TextureFormat format = TextureFormat.RGBA32, FilterMode filterMode = FilterMode.Bilinear) {
            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, format, false, true);
            tex.filterMode = filterMode;
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); // read pixel info from active RT
            RenderTexture.active = currentActiveRT;
            tex.Apply();
            return tex;
        }

        public static bool[,] Tex2BoolMap(Texture2D texture) {
            bool[,] ret = new bool[texture.width, texture.height];
            Color[] pix = texture.GetPixels();
            for (int i = 0; i < pix.Length; ++i) {
                ret[i % texture.width, i / texture.width] = (pix[i].r < 0.5f ? true : false);
            }
            return ret;
        }

        public static bool[,] Tex2BoolMap(RenderTexture rt) {
            Texture2D texture = RT2Tex(rt);
            return Tex2BoolMap(texture);
        }
    }


}


