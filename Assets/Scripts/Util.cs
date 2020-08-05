using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        /*
            Boolmap coord follows Unity TexCoord, which is:

            height (Y/Z)
            ^
            |  ^ ^
            |   - 
            O--------> width (X)       visit: (X, Y/Z)
         */

        public static Texture2D RT2Tex(RenderTexture rt, TextureFormat format = TextureFormat.RGBA32, FilterMode filterMode = FilterMode.Bilinear) {
            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, format, false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = filterMode;
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); // read pixel info from active RT
            RenderTexture.active = currentActiveRT;
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Convert a Texture2D to a bool map.
        /// </summary>
        public static bool[,] Tex2BoolMap(Texture2D texture, bool brighterEquals, float brightThreshold = 0.5f) {
            bool[,] ret = new bool[texture.width, texture.height];
            Color[] pix = texture.GetPixels(); // left to right, bottom to top (i.e. row after row)
            for (int i = 0; i < pix.Length; ++i) {
                ret[i % texture.width, i / texture.width] = (pix[i].r > brightThreshold ? brighterEquals : !brighterEquals);
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
        public static Texture2D BoolMap2Tex(in bool[,] map, bool brighterEquals) {
            int width = map.GetLength(0), height = map.GetLength(1);
            Texture2D tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    tex.SetPixel(x, y, map[x, y] == brighterEquals ? Color.white : Color.black);
                }
            }
            tex.Apply();
            // Debug.Log("BoolMap2Tex generates a " + tex.width.ToString() + "x" + tex.height.ToString() + " texture.");
            return tex;
        }

        public static bool[,] PlotPointsToBoolMap(List<Vector2> points, int width, int height, bool pointEquals = true) {
            bool[,] ret = new bool[width, height];
            foreach (Vector2 point in points) {
                float x = Mathf.Clamp(point.x, 0f, width - 1f);
                float y = Mathf.Clamp(point.y, 0f, height - 1f);
                int x1 = Mathf.FloorToInt(x), x2 = Mathf.CeilToInt(x), y1 = Mathf.FloorToInt(y), y2 = Mathf.CeilToInt(y); // "Conservative rasterization"
                if (x2 == width) --x2;
                if (y2 == height) --y2;
                ret[x1, y1] = pointEquals;
                ret[x1, y2] = pointEquals;
                ret[x2, y1] = pointEquals;
                ret[x2, y2] = pointEquals;
            }
            return ret;
        }

        public static bool[,] PlotPointsToBoolMap(List<Vector2Int> points, int width, int height, bool pointEquals = true) {
            bool[,] ret = new bool[width, height];
            foreach (Vector2Int point in points) {
                ret[point.x, point.y] = pointEquals;
            }
            return ret;
        }

        public static bool[,] ByteMapToBoolMap(byte[,] byteMap, HashSet<int> trueMask) {
            int width = byteMap.GetLength(0), height = byteMap.GetLength(1);
            bool[,] ret = new bool[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    ret[x, y] = trueMask.Contains(byteMap[x, y]) ? true : false;
                }
            }
            return ret;
        }

        public static string ByteMapWithSingleDigitToString(byte[,] byteMap) {
            int width = byteMap.GetLength(0), height = byteMap.GetLength(1);
            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    sb.Append(byteMap[x, y]);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static byte[,] StringToByteMapWithSingleDigit(string s, char lineSeperator) {
            List<List<byte>> lines = new List<List<byte>>();
            List<byte> line = new List<byte>();
            foreach (char c in s) {
                if (char.IsDigit(c)) {
                    line.Add(byte.Parse(c.ToString()));
                } else if (c == lineSeperator) {
                    lines.Add(line);
                    line = new List<byte>();
                }
            }
            if (lines.Count == 0) {
                return null;
            }
            int lineLength = lines[0].Count;
            for (int i = 1; i < lines.Count; ++i) {
                if (lineLength != lines[1].Count) {
                    throw new Exception("pattern is not a square.");
                }
            }
            byte[,] ret = new byte[lineLength, lines.Count];
            for (int x = 0; x < lineLength; ++x) {
                for (int y = 0; y < lines.Count; ++y) {
                    ret[x, y] = lines[y][x];
                }
            }
            return ret;
        }

        public static string ReadStringFromResource(string filePath) {
            TextAsset asset = Resources.Load<TextAsset>(filePath);
            if (asset == null) {
                throw new Exception("Resource not found.");
            }
            return asset.text;
        }


        public static Texture2D Downsample(Texture src, int downsampleRatio) {
            RenderTexture rt, prevRT;
            int newWidth = src.width / downsampleRatio, newHeight = src.height / downsampleRatio; // not error prone!!
            rt = RenderTexture.GetTemporary(newWidth, newHeight, 0);
            prevRT = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = prevRT;
            Texture2D ret = RT2Tex(rt);
            RenderTexture.ReleaseTemporary(rt);
            return ret;
        }

        public static Texture2D Upsample(Texture src, int upsampleRatio) {
            RenderTexture rt, prevRT;
            int newWidth = src.width * upsampleRatio, newHeight = src.height * upsampleRatio;
            rt = RenderTexture.GetTemporary(newWidth, newHeight, 0);
            prevRT = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = prevRT;
            Texture2D ret = RT2Tex(rt);
            RenderTexture.ReleaseTemporary(rt);
            return ret;
        }
        

        public static bool[,] Upscale(bool[,] src, int upsampleRatio, float threshold) {
            Texture2D srcTex = BoolMap2Tex(src, true); // bilinear
            srcTex.filterMode = FilterMode.Bilinear;
            Texture2D upTex = Upsample(srcTex, upsampleRatio);
            bool[,] ret = Tex2BoolMap(upTex, true, threshold);
            return ret;
        }

        public static Texture2D Binarize(Texture src, float threshold) {
            Shader thresholdShader = Shader.Find("MicroUniverse/Threshold");
            if (thresholdShader == null) {
                throw new Exception("Threshold shader not found.");
            }
            FilterMode ogFilterMode = src.filterMode;
            src.filterMode = FilterMode.Point;
            Material mat = new Material(thresholdShader);
            mat.SetFloat("_Threshold", threshold);
            RenderTexture rt, prevRT;
            rt = RenderTexture.GetTemporary(src.width, src.height, 0);
            prevRT = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = prevRT;
            Texture2D ret = RT2Tex(rt);
            ret.filterMode = FilterMode.Point;
            RenderTexture.ReleaseTemporary(rt);
            // src.filterMode = ogFilterMode;
            return ret;
        }

        public static long PowerInt(int a, int n) {
            long product = 1;
            for (int i = 0; i < n; i++) product *= a;
            return product;
        }

    }

    public static class ClassExtension {

        public static Vector2 Rotate(this Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 Mirror(this Vector2 p, Vector2 center, Vector2 directionNormalized) {
            Vector2 centerToSelf = p - center;
            float distance = centerToSelf.magnitude;
            Vector2 centerToSelfDir = centerToSelf / distance; // normalized.
            float cosTheta = Vector2.Dot(centerToSelfDir, directionNormalized);
            float centerToMidPointDistance = cosTheta * distance;
            Vector2 midPoint = center + directionNormalized * centerToMidPointDistance;
            Vector2 selfToMidpoint = midPoint - p;
            return midPoint + selfToMidpoint; // mirroring
        }

        /// <summary>
        /// Shuffle the element in a list.
        /// </summary>
        public static List<T> Shuffle<T>(this IEnumerable<T> iter) {
            return iter.OrderBy(arg => Guid.NewGuid()).ToList();
        }

        /// <summary>
        /// Expand (posibilly shrink) a list.
        /// </summary>
        public static List<T> Expand<T>(this IEnumerable<T> iter, int count, bool randomize = false) { // TODO: impl!
            if (count <= 0) {
                return new List<T>();
            }
            List<T> list = iter.ToList();
            if (count < list.Count) {
                list = list.Take(count).ToList();
            } else if (count > list.Count) {
                int ogCount = list.Count;
                int left = count - ogCount;
                for (int i = 0; i < left; i++) {
                    list.Add(list[UnityEngine.Random.Range(0, ogCount)]);
                }
            }
            return list;
        }

    }


}


