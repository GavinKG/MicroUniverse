using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    /// <summary>
    /// A basic graph node interface.
    /// Currently used in MST generation.
    /// </summary>
    public interface IGraphNode {
        Vector2Int Center { get; }
        void RegisterConnected(IGraphNode other);
    }

    /// <summary>
    /// Static utility wrapper class.
    /// </summary>
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
                    tex.SetPixel(c, r, map[r, c] == brighterEquals ? Color.white : Color.black); // IMPORTANT: c, r -> x, y
                }
            }
            tex.Apply();
            Debug.Log("BoolMap2Tex generates a " + tex.width.ToString() + "x" + tex.height.ToString() + " texture.");
            return tex;
        }

        public static bool[,] PlotPointsToBoolMap(List<Vector2> points, int rowSize, int colSize, bool pointEquals = true) {
            bool[,] ret = new bool[rowSize, colSize];
            foreach (Vector2 point in points) {
                float r = Mathf.Clamp(point.x, 0f, rowSize);
                float c = Mathf.Clamp(point.y, 0f, colSize);
                int r1 = Mathf.FloorToInt(r), r2 = Mathf.CeilToInt(r), c1 = Mathf.FloorToInt(c), c2 = Mathf.CeilToInt(c); // "Conservative rasterization"
                if (r2 == rowSize) --r2;
                if (c2 == colSize) --c2;
                ret[r1, c1] = pointEquals;
                ret[r1, c2] = pointEquals;
                ret[r2, c1] = pointEquals;
                ret[r2, c2] = pointEquals;
            }
            return ret;
        }

        public static bool[,] PlotPointsToBoolMap(List<Vector2Int> points, int rowSize, int colSize, bool pointEquals = true) {
            bool[,] ret = new bool[rowSize, colSize];
            foreach (Vector2Int point in points) {
                ret[point.x, point.y] = pointEquals;
            }
            return ret;
        }


        public static Texture2D Downsample(Texture src, int downsampleRatio) {
            RenderTexture rt, prevRT;
            int newWidth = src.width / downsampleRatio, newHeight = src.height / downsampleRatio; // not error prone!!
            rt = RenderTexture.GetTemporary(newWidth, newHeight, 0);
            // rt = new RenderTexture(newWidth, newHeight, 0);
            prevRT = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = prevRT;
            Texture2D ret = RT2Tex(rt);
            RenderTexture.ReleaseTemporary(rt);
            // rt.Release();
            return ret;
        }

    }

    public static class Vector2Extension {

        public static Vector2 Rotate(this Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }
    }


}


