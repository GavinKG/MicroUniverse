using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class TestCityWallGen : MonoBehaviour {

        public enum DownsampleSize {
            One, Two, Four, Eight, Sixteen
        }


        public Texture2D cityTex;
        public int downsampleRatio = 4;

        public GameObject coverGO;
        public GameObject wallGO;

        public float wallLength = 20f;
        public float wallHeight = 2f;
        [Range(0, 4)]
        public int smoothCount = 2;

        public void Generate() {
            Texture2D downsampled;
            if (downsampleRatio != 1) {
                downsampled = Util.Downsample(cityTex, downsampleRatio);
            } else {
                downsampled = cityTex;
            }
            
            bool[,] map = Util.Tex2BoolMap(downsampled, brighterEquals: true);

            MarchingSquare cityWallGenerator = new MarchingSquare();
            cityWallGenerator.GenerateMesh(map, wallLength / (cityTex.width / downsampleRatio), wallHeight, smoothCount);

            coverGO.transform.position = Vector3.zero;
            coverGO.transform.rotation = Quaternion.identity;
            wallGO.transform.position = Vector3.zero;
            wallGO.transform.rotation = Quaternion.identity;

            coverGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.CoverMesh;
            wallGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.WallMesh;

            print("Success.");

        }

    }

}