using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {
    public class LoadingJob : MonoBehaviour {

        // --------------------

        [Header("Source")]
        public Texture2D source;

        [Header("Step.2: Flood Fill")]
        public int smallRegionThreshold = 20;

        [Header("Step.4: Marching Square")]
        public GameObject coverGO;
        public GameObject wallGO;
        public float wallLength = 20f;
        public float wallHeight = 2f;
        [Range(0, 4)]
        public int smoothCount = 2;

        [Header("Debug")]
        public RawImage debugImage;

        // --------------------

        // Step.1:
        private Texture2D texAfterStep1;

        // Step.2:
        private bool[,] wallMap;
        private List<RegionInfo> regionInfos;

        // Step.3:
        private RegionInfo rootRegion;

        // --------------------

        void DebugTex(Texture tex) {
            debugImage.texture = tex;
            print("DebugTex gets a texture: " + tex.width.ToString() + "x" + tex.height.ToString());
        }

        void DebugTex(bool[,] map) {
            DebugTex(Util.BoolMap2Tex(map, true));
        }

        // --------------------

        public void Load() {

            
            print("Loading Level...");
            print("Starting at " + Time.time.ToString());


            // ----------
            // Step.0
            print("Step.0: Sanity Check.");
            if (source.width != source.height) {
                throw new System.Exception("Source texture should be a square!");
            }


            // ----------
            // Step.1
            print("Step.1: binarize, downsample.");

            Texture afterBinarize = Util.Binarize(source, 0.5f);
            texAfterStep1 = Util.Downsample(afterBinarize, 8); // should be from 1024x1024 to 128x128
            // stroke should be in white, background black.


            // ----------
            // Step.2
            print("Step.2: flood fill.");
            bool[,] fillMap = Util.Tex2BoolMap(texAfterStep1, brighterEquals: true);
            wallMap = (bool[,])fillMap.Clone(); // shallow copy.

            int fillMapRowCount = fillMap.GetLength(0), fillMapColCount = fillMap.GetLength(1);
            FloodFill floodFiller = new FloodFill();

            // first fill four corners / center region to white since they are useless in further process.
            floodFiller.Fill(ref fillMap, 0, 0, fillValue: false);
            floodFiller.Fill(ref fillMap, 0, fillMapColCount - 1, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapRowCount - 1, 0, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapRowCount - 1, fillMapColCount - 1, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapRowCount / 2, fillMapColCount / 2, fillValue: false);

            List<FloodFill.FillResult> fillResults = floodFiller.FindAndFill(ref fillMap, false);
            regionInfos = new List<RegionInfo>();
            foreach (FloodFill.FillResult fillResult in fillResults) {
                if (fillResult.FilledPoints.Count >= smallRegionThreshold) {
                    regionInfos.Add(new RegionInfo(fillResult));
                }
            }
            print("RegionInfo list contains " + regionInfos.Count.ToString() + " regions");


            // ----------
            // Step.3
            print("Step.3: MST.");
            List<IGraphNode> graphNodes = new List<IGraphNode>(regionInfos.Count);
            foreach (RegionInfo regionInfo in regionInfos) {
                graphNodes.Add(regionInfo as IGraphNode);
            }
            rootRegion = MST.Run(graphNodes, registerBidirectional: false) as RegionInfo;

            // probably do some wall diggings here.


            // ----------
            // Step.4
            print("Step.4: Marching Square");
            CityWallGenerator cityWallGenerator = new CityWallGenerator();
            cityWallGenerator.GenerateMesh(wallMap, wallLength / fillMap.GetLength(0), wallHeight, smoothCount);

            coverGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.CoverMesh;
            wallGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.WallMesh;


            // ----------


            print("Loading finished at " + Time.time.ToString());

        }

    }

}