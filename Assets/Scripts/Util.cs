using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public static class Util {

        // Bool map: row major!

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

        /// <summary>
        /// Convert a Texture2D to a row-major bool map.
        /// </summary>
        public static bool[,] Tex2BoolMap(Texture2D texture, bool brighterEquals) {
            bool[,] ret = new bool[texture.height, texture.width];
            Color[] pix = texture.GetPixels(); // left to right, bottom to top (i.e. row after row)
            for (int i = 0; i < pix.Length; ++i) {
                ret[texture.height - 1 - i / texture.width, i % texture.width] = (pix[i].r > 0.5f ? brighterEquals : !brighterEquals);
            }
            return ret;
        }

        public static bool[,] Tex2BoolMap(RenderTexture rt, bool brighterEquals) {
            Texture2D texture = RT2Tex(rt);
            return Tex2BoolMap(texture, brighterEquals);
        }

        /// <summary>
        /// Convert a bool map to a B&W texture.
        /// </summary>
        /// <param name="map">row contains column, from top left.</param>
        public static Texture2D BoolMap2Tex(in bool[,] map, bool brighterEquals) {
            Texture2D tex = new Texture2D(map.GetLength(1), map.GetLength(0));
            int rowCount = map.GetLength(0), colCount = map.GetLength(1);
            for (int r = 0; r < rowCount; ++r) {
                for (int c = 0; c < colCount; ++c) {
                    tex.SetPixel(c, r, map[r, c] == brighterEquals ? Color.white : Color.black);
                }
            }
            tex.Apply();
            Debug.Log("BoolMap2Tex generates a " + tex.width.ToString() + "x" + tex.height.ToString() + " texture.");
            return tex;
        }

    }


}


