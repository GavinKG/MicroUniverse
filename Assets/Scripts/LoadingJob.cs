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

        [Header("Random Core")]
        [Range(0, 100)] public int seed;

        [Header("General Settings")]
        public int cityWH = 128;

        [Header("Step.2: Marching Square")]
        public GameObject coverGO;
        public GameObject wallGO;
        [Range(1, 16)] public int msDownsampleRate = 8;
        public float wallHeight = 2f;
        [Range(0, 4)] public int cityWallSmoothCount = 4;
        [Range(0, 1)] public float cityWallSmoothRatio = 0.5f; // 1: shrink, -1: expand

        [Header("Step.3: Recapture")]
        public CaptureOverviewMask capturer;
        public int recaptureResolution = 64;

        [Header("Step.4: Flood Fill")]
        
        public int smallRegionThreshold = 20; // pixel

        [Header("Step.6: WFC")]
        public string sampleFilePath = "WFCSample.txt";
        [Range(1, 3)] public int N = 3;
        [Range(1, 7)] public int symmetryVariantCount = 7;

        [Header("Step.7: Planting")]
        [Range(0.05f, 0.5f)] public float perlinFreq = 0.1f;
        public GameObject emptyGOPrefab;
        public PropCollection propCollection;
        public Transform propRoot;
        [Range(0f, 1f)] public float companionSpawnRatio = 0.4f;
        [Range(0f, 1f)] public float badPillarRatio = 0.3f;
        public List<Theme> themes;
        public GameObject regionMaskPrefab;

        // Material templates:
        public Material buildingMat;
        public Material baseMat;
        public Material emptyMat;
        public Material plantMat;

        [Header("Step.8 AO")]
        public CaptureOverviewMask aoCapturer;
        public int aoCaptureResolution = 2048;
        public int aoResolution = 256;
        public int blurSpreadSize = 5;
        public int blurIterations = 4;

        [Header("Step.9 Raycast portal")]
        public GameObject portalPrefab;

        [Header("Step.10: coloring")]
        public MeshRenderer groundMeshRenderer; // for changing its world-UV sampled diffuse tex.

        [Header("Debug")]
        public GameObject debugBall;
        public List<RawImage> debugImages;
        public Text cityGenTimeText;

        // --------------------

        // Step.1: binarize, downsample
        private Texture2D texAfterPrepare;
        private bool[,] wallMap;

        // Step.3: recapture
        private Texture2D texAfterRecapture;

        // Step.3:
        private List<RegionInfo> regionInfos; // SHOULD FOLLOW: index = region id

        // Step.4:
        private RegionInfo rootRegion;

        // Step.6:
        private WFC wfc;

        // Step.8:
        private Texture aoTex;

        // Step.10:
        private Texture2D coloredTransparentTex;

        private bool loaded = false;

        // --------------------

        private void Start() {
            
        }

        void DebugTex(Texture tex, int index, bool shouldTerminate = false) {
            debugImages[index].texture = tex ?? throw new Exception("Texture not generated!");
            debugImages[index].SetNativeSize();
            print("DebugTex gets a texture: " + tex.width.ToString() + "x" + tex.height.ToString());
            if (shouldTerminate) {
                throw new Exception("Debugging finished!");
            }
        }

        void DebugTex(bool[,] map, int index, bool shouldTerminate = false) {
            DebugTex(Util.BoolMap2Tex(map, true), index, shouldTerminate);
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

            firstStamp = -1;

            if (loaded) {
                throw new Exception("City already loaded.");
            }

            print("[LoadingJob] Loading Level...");

            // ----------
            // Step.0
            print("Step.0: Prep.");
            if (source.width != source.height) {
                throw new System.Exception("Source texture should be a fucking square.");
            }
            int currResolution = source.width;
            Shader.SetGlobalFloat("CityWH", cityWH);

            // ----------
            // Step.1
            print("Step.1: binarize, downsample." + Timestamp);
            Texture afterBinarize = Util.Binarize(source, 0.5f);
            texAfterPrepare = Util.Downsample(afterBinarize, msDownsampleRate); // should be from 1024x1024 to 128x128, stroke should be in white, background black.
            wallMap = Util.Tex2BoolMap(texAfterPrepare, false); // borders are black...
            currResolution /= msDownsampleRate;

            // ----------
            // Step.2
            print("Step.2: Marching Square for city wall" + Timestamp);
            MarchingSquare cityWallGenerator = new MarchingSquare();
            cityWallGenerator.GenerateMesh(wallMap, (float)cityWH / currResolution, wallHeight, cityWallSmoothCount, cityWallSmoothRatio);

            coverGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.CoverMesh;
            wallGO.GetComponent<MeshFilter>().mesh = cityWallGenerator.WallMesh;
            wallGO.GetComponent<MeshCollider>().sharedMesh = cityWallGenerator.WallMesh;

            Vector3 coverWallPos = new Vector3(0f, wallHeight, 0f);
            coverGO.transform.position = coverWallPos;
            wallGO.transform.position = coverWallPos;


            // ----------
            // Step.3
            print("Recapture" + Timestamp);
            texAfterRecapture = capturer.Capture(FilterMode.Point, cityWH, recaptureResolution);
            currResolution = recaptureResolution;

            // ----------
            // Step.4
            print("Step.4: flood fill." + Timestamp);
            bool[,] fillMap = Util.Tex2BoolMap(texAfterRecapture, brighterEquals: true);
            int fillMapWidth = fillMap.GetLength(0), fillMapHeight = fillMap.GetLength(1);
            FloodFill floodFiller = new FloodFill();

            // first fill four corners / center region to white since they are useless in further process.
            floodFiller.Fill(ref fillMap, 0, 0, fillValue: false);
            floodFiller.Fill(ref fillMap, 0, fillMapHeight - 1, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapWidth - 1, 0, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapWidth - 1, fillMapHeight - 1, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapWidth / 2, fillMapHeight / 2, fillValue: false);
            
            List<FloodFill.FillResult> fillResults = floodFiller.FindAndFill(ref fillMap, false);
            regionInfos = new List<RegionInfo>();
            foreach (FloodFill.FillResult fillResult in fillResults) {
                if (fillResult.FilledPoints.Count >= smallRegionThreshold) {
                    regionInfos.Add(new RegionInfo(fillResult));
                }
            }
            print("RegionInfo list contains " + regionInfos.Count.ToString() + " regions");
            if (regionInfos.Count == 0) {
                throw new Exception("No region to proceed, quitting...");
            }
            // DebugTex(regionInfos[0].MapTex, 0);
            // DebugTex(regionInfos[0].FlattenedMapTex, 1);

            

            // ----------
            // Step.5
            print("Step.5: MST." + Timestamp);
            List<IGraphNode> graphNodes = new List<IGraphNode>(regionInfos.Count);
            foreach (RegionInfo regionInfo in regionInfos) {
                graphNodes.Add(regionInfo as IGraphNode);
            }
            rootRegion = MST.Run(graphNodes, registerBidirectional: true) as RegionInfo;

            // ----------
            // Step.6
            print("Step.6: WFC." + Timestamp);
            string sampleString = Util.ReadStringFromResource(sampleFilePath);
            byte[,] sample = Util.StringToByteMapWithSingleDigit(sampleString, '#');
            wfc = new WFC(sample, N, false, false, symmetryVariantCount);
            foreach (RegionInfo regionInfo in regionInfos) {
                regionInfo.DoWFC(wfc, seed);
            }
            // DebugTex(regionInfos[0].debugTex1, 2);


            // ----------
            // Step.7: construct regions.
            print("Step.7: Construct regions." + Timestamp);

            // warm themes
            List<ThemeMaterialHolder> themeMatHolders = new List<ThemeMaterialHolder>(themes.Count);
            foreach(Theme theme in themes) {
                ThemeMaterialHolder holder = new ThemeMaterialHolder(theme);
                holder.CreateMaterialInstance(buildingMat, baseMat, emptyMat, plantMat);
                themeMatHolders.Add(holder);
            }

            // random theme:
            List<ThemeMaterialHolder> themeAssigned = themeMatHolders.Shuffle().Expand(regionInfos.Count).ToList();

            float scaleFactor = (float)cityWH / (float)currResolution;
            for (int i = 0; i < regionInfos.Count; ++i) {
                regionInfos[i].RegionID = i;

                GameObject subRootGO = Instantiate(emptyGOPrefab, Vector3.zero, Quaternion.identity, propRoot);
                subRootGO.name = "Region #" + i.ToString();

                GameObject propRootGO = Instantiate(emptyGOPrefab, Vector3.zero, Quaternion.identity, subRootGO.transform);
                propRootGO.name = "Props";

                GameObject autoBallRootGO = Instantiate(emptyGOPrefab, Vector3.zero, Quaternion.identity, subRootGO.transform);
                autoBallRootGO.name = "Auto Balls";

                ThemeMaterialHolder themeMaterialHolder = themeAssigned[i];
                print("Region #" + i.ToString() + " uses theme: " + themeMaterialHolder.theme.gameObject.name);
                
                // CONSTRUCT:
                regionInfos[i].ConstructRegion(scaleFactor, propCollection, propRootGO.transform, autoBallRootGO.transform, perlinFreq, companionSpawnRatio, badPillarRatio, themeMaterialHolder);

                // AFTER CONSTRUCT:
                GameObject regionMask = Instantiate(regionMaskPrefab, regionInfos[i].CenterWS, regionMaskPrefab.transform.rotation, subRootGO.transform);
                MeshRenderer meshRenderer = regionMask.GetComponent<MeshRenderer>();
                meshRenderer.material.SetTexture("_MainTex", regionInfos[i].TransparentSubMapTex);

            }

            // DebugTex(regionInfos[0].DebugTransformBackToTex(), 3);
            DebugTex(regionInfos[0].TransparentSubMapTex, 0);
            DebugTex(regionInfos[1].TransparentSubMapTex, 1);
            DebugTex(regionInfos[2].TransparentSubMapTex, 2);
            DebugTex(regionInfos[3].TransparentSubMapTex, 3);

            // ----------
            // Step.8 AO
            aoTex = aoCapturer.Capture(FilterMode.Bilinear, cityWH, aoCaptureResolution);
            aoTex = GaussianBlur.Blur(aoTex, aoCaptureResolution / aoResolution, blurSpreadSize, blurIterations);
            Shader.SetGlobalTexture("FloorAO", aoTex);
            // DebugTex(aoTex, 4);


            // ---------
            // Step.9 Raycast & construct portal
            int cityWallLayerMask = LayerMask.GetMask(new string[] { "CityWall" });
            foreach (RegionInfo currRegion in regionInfos) {
                GameObject portalRoot = Instantiate(emptyGOPrefab, Vector3.zero, Quaternion.identity, propRoot.GetChild(currRegion.RegionID));
                portalRoot.name = "Portals";
                currRegion.portals = new List<RegionPortal>(currRegion.ConnectedRegion.Count);
                foreach (RegionInfo toRegion in currRegion.ConnectedRegion) {
                    RaycastHit hit;
                    bool result = Physics.Raycast(currRegion.CenterWS, toRegion.CenterWS - currRegion.CenterWS, out hit, Mathf.Infinity, cityWallLayerMask);
                    if (!result) {
                        throw new Exception("PORTAL RAYCAST FAILED...");
                    }
                    Vector3 position = hit.point;
                    Quaternion rotation = Quaternion.LookRotation(hit.normal);
                    GameObject portal = Instantiate(portalPrefab, position, rotation, portalRoot.transform);
                    portal.SetActive(false);
                    portal.name = "Portal: #" + currRegion.RegionID.ToString() +  " -> #" + toRegion.RegionID.ToString();
                    RegionPortal regionPortal = portal.GetComponent<RegionPortal>();
                    regionPortal.currRegionId = currRegion.RegionID;
                    regionPortal.toRegionId = toRegion.RegionID;
                    currRegion.portals.Add(regionPortal);
                }
            }


            // ---------
            // Step.10 Combine ColoredTex -> floor
            RenderTexture rt0 = RenderTexture.GetTemporary(fillMapWidth, fillMapHeight);
            RenderTexture rt1 = RenderTexture.GetTemporary(fillMapWidth, fillMapHeight);
            RenderTexture prevRT = RenderTexture.active;
            Material texAddMat = new Material(Shader.Find("MicroUniverse/TexAdd"));
            int ppIndex = 0;
            RenderTexture[] pp = new RenderTexture[2] { rt0, rt1 };
            Graphics.Blit(regionInfos[0].ColoredTransparentMapTex, rt0);

            for (int i = 1; i < regionInfos.Count; ++i) {
                texAddMat.SetTexture("_AddTex", regionInfos[i].ColoredTransparentMapTex);
                Graphics.Blit(pp[ppIndex], pp[1 - ppIndex], texAddMat);
                ppIndex = 1 - ppIndex;
            }

            coloredTransparentTex = Util.RT2Tex(pp[ppIndex]);
            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt0);
            RenderTexture.ReleaseTemporary(rt1);
            groundMeshRenderer.material.SetTexture("_DiffuseTex", coloredTransparentTex);
            // DebugTex(coloredTex, 0);

            // ---------
            // Last: pass on all useful data to game controller...
            MainGameplayController controller =  GameManager.Instance.CurrController as MainGameplayController;
            controller.RegionInfos = regionInfos;
            controller.StartRegion = rootRegion;



            print("[LoadingJob] Loading finished." + Timestamp);
            cityGenTimeText.text = "CityGen Time: " + Timestamp.ToString();
            loaded = true;
        }

    }

}