﻿using System;
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

        [Header("Random Core")]
        [Range(0, 100)] public int seed;

        [Header("Step.2: Marching Square")]
        public GameObject coverGO;
        public GameObject wallGO;
        public float wallLength = 20f;
        public float wallHeight = 2f;
        [Range(0, 4)] public int cityWallSmoothCount = 4;
        [Range(0, 1)] public float cityWallSmoothRatio = 0.5f; // 1: shrink, -1: expand

        [Header("Step.3: Recapture")]
        public CaptureOverviewMask capturer;

        [Header("Step.4: Flood Fill")]
        public int smallRegionThreshold = 20;

        [Header("Step.N: WFC")]
        public string sampleFilePath = "WFCSample.txt";
        [Range(1, 3)] public int N = 3;
        [Range(1, 7)] public int symmetryVariantCount = 7;

        [Header("Step:7 Gen road network")]
        public GameObject debugGO;
        [Range(1, 4)] public int roadNetworkUpscale = 1;
        [Range(0, 4)] public int roadNetworkSmoothCount = 1;
        [Range(0, 1)] public float roadNetworkSmoothRatio = 0.1f;
        [Range(0, 1)] public float roadNetworkWidthRatio = 0.5f;


        [Header("Debug")]
        public RawImage debugImage;

        // --------------------

        // Step.1: binarize, downsample
        private Texture2D texAfterPrepare;
        private bool[,] wallMap;

        // Step.3: recapture
        private Texture2D texAfterRecapture;

        // Step.3:
        private List<RegionInfo> regionInfos;

        // Step.4:
        private RegionInfo rootRegion;

        // Step.6:
        private WFC wfc;

        // --------------------

        void DebugTex(Texture tex) {
            debugImage.texture = tex;
            debugImage.SetNativeSize();
            print("DebugTex gets a texture: " + tex.width.ToString() + "x" + tex.height.ToString());
        }

        void DebugTex(bool[,] map) {
            DebugTex(Util.BoolMap2Tex(map, true));
        }

        float firstStamp = -1;
        string Timestamp {
            get {
                if (firstStamp == -1) {
                    firstStamp = Time.realtimeSinceStartup;
                }
                return " [t=" + (Time.realtimeSinceStartup - firstStamp).ToString() + "]";
            }
        }

        // --------------------

        public void Load() {


            print("[LoadingJob] Loading Level...");


            // ----------
            // Step.0
            print("Step.0: Sanity Check.");
            if (source.width != 1024 || source.height != 1024) {
                throw new System.Exception("Source texture should be in 1024x1024!");
            }


            // ----------
            // Step.1
            print("Step.1: binarize, downsample." + Timestamp);
            Texture afterBinarize = Util.Binarize(source, 0.5f);
            texAfterPrepare = Util.Downsample(afterBinarize, 8); // should be from 1024x1024 to 128x128, stroke should be in white, background black.
            wallMap = Util.Tex2BoolMap(texAfterPrepare, true);


            // ----------
            // Step.2
            print("Step.2: Marching Square for city wall" + Timestamp);
            MarchingSquare cityWallGenerator = new MarchingSquare();
            cityWallGenerator.GenerateMesh(wallMap, wallLength / wallMap.GetLength(0), wallHeight, cityWallSmoothCount, cityWallSmoothRatio);

            coverGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.CoverMesh;
            wallGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.WallMesh;


            // ----------
            // Step.3
            print("Recapture" + Timestamp);
            texAfterRecapture = capturer.Capture(FilterMode.Point);


            // ----------
            // Step.4
            print("Step.4: flood fill." + Timestamp);
            bool[,] fillMap = Util.Tex2BoolMap(texAfterRecapture, brighterEquals: true);
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
            // Step.5
            print("Step.5: MST." + Timestamp);
            List<IGraphNode> graphNodes = new List<IGraphNode>(regionInfos.Count);
            foreach (RegionInfo regionInfo in regionInfos) {
                graphNodes.Add(regionInfo as IGraphNode);
            }
            rootRegion = MST.Run(graphNodes, registerBidirectional: false) as RegionInfo;


            // ----------
            // Step.6
            print("Step.6: WFC." + Timestamp);

            // ID rules:
            // Empty = 0
            // Road = 1
            // FountainRoad = 2
            // PillarRoad = 3
            // *Wall = 4

            string sampleString = Util.ReadStringFromResource(sampleFilePath);
            byte[,] sample = Util.StringToByteMapWithSingleDigit(sampleString);
            wfc = new WFC(sample, N, false, false, symmetryVariantCount);
            foreach (RegionInfo regionInfo in regionInfos) {
                regionInfo.DoWFC(wfc, seed);
            }


            // ----------
            // Step.7
            print("Step.7: Marching Square for road network." + Timestamp);

            foreach (RegionInfo regionInfo in regionInfos) {
                regionInfo.MarchingSquareRoadnetwork(roadNetworkUpscale, 1f, roadNetworkSmoothCount, roadNetworkSmoothRatio, roadNetworkWidthRatio);
            }



            // ----------
            // Step.8
            print("Step.8: Road network transform back." + Timestamp);
            foreach (RegionInfo regionInfo in regionInfos) {
                regionInfo.VertexTransformBack();
            }


            RegionInfo debugRI = regionInfos[0];
            debugGO.GetComponent<MeshFilter>().mesh = debugRI.RoadNetworkCoverMesh;
            // DebugTex(debugRI.debugTex3);


            print("[LoadingJob] Loading finished." + Timestamp);
        }

    }

}