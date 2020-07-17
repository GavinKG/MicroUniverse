using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class TestCityWallGen : MonoBehaviour {

        public enum DownsampleSize {
            One, Two, Four, Eight, Sixteen
        }


        public Texture2D cityTex;
        public DownsampleSize downsampleSize;

        public GameObject coverGO;
        public GameObject wallGO;

        public float wallLength = 20f;
        public float wallHeight = 2f;
        [Range(0, 4)]
        public int smoothCount = 2;

        public void Generate() {

            RenderTexture rt, prevRT;

            int newWidth = cityTex.width, newHeight = cityTex.height;

            if (downsampleSize == DownsampleSize.Two) {
                newWidth = cityTex.width / 2;
                newHeight = cityTex.height / 2;
            } else if (downsampleSize == DownsampleSize.Four) {
                newWidth = cityTex.width / 4;
                newHeight = cityTex.height / 4;
            } else if (downsampleSize == DownsampleSize.Eight) {
                newWidth = cityTex.width / 8;
                newHeight = cityTex.height / 8;
            } else if (downsampleSize == DownsampleSize.Sixteen) {
                newWidth = cityTex.width / 16;
                newHeight = cityTex.height / 16;
            }

            rt = new RenderTexture(newWidth, newHeight, 0);
            prevRT = RenderTexture.active;
            Graphics.Blit(cityTex, rt);
            RenderTexture.active = prevRT;

            bool[,] map = Util.Tex2BoolMap(rt, brighterEquals: false);

            MeshFilter meshFilter = GetComponent<MeshFilter>();

            CityWallGenerator cityWallGenerator = new CityWallGenerator();
            cityWallGenerator.GenerateMesh(map, wallLength / newWidth, wallHeight, smoothCount);

            coverGO.transform.position = Vector3.zero;
            coverGO.transform.rotation = Quaternion.identity;
            wallGO.transform.position = Vector3.zero;
            wallGO.transform.rotation = Quaternion.identity;

            coverGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.CoverMesh;
            wallGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.WallMesh;

            rt.Release();

            print("Success.");

        }

    }

}