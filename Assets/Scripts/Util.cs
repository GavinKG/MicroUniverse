using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    
    //// Keep this a struct!!
    //// x and y should stick to row / col according to bool map, not x, y in texture coords.
    //public struct Point {
    //    public int row;
    //    public int col;
    //    public Point(int _r, int _c) {
    //        row = _r;
    //        col = _c;
    //    }
    //    public void Accumulate(in Point other) {
    //        row += other.row;
    //        col += other.col;
    //    }
    //    public int X { get { return col; } }
    //    public int Y { get { return row; } }

    //    public float DistanceTo(in Point other) {
    //        return Mathf.Sqrt(Mathf.Pow(row - other.row, 2) + Mathf.Pow(col - other.col, 2));
    //    }
    //}



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


