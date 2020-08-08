using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class RegionInfo : IGraphNode {

        // ID rules:
        const byte id_empty = 0;
        const byte id_road = 1;
        const byte id_pillarRoad = 2;
        const byte id_masterPillarRoad = 3;
        const byte id_wall = 4;
        const byte id_building = 5;
        const byte id_roadNotWalkable = 6; // roads that will not allow auto-ball to step on, and don't want to be populated with grass block.

        // ------------ PUBLIC PROPERTIES:

        // ------- Loading time:

        public int RegionID { get; set; }

        /// <summary>
        /// Region center point in filled space.
        /// </summary>
        public Vector2 Center { get { return fillResult.FilledAreaCenterPoint; } }

        /// <summary>
        /// Region center point in world space (XZ).
        /// </summary>
        public Vector2 CenterWS2 { get { return new Vector2(Center.x - fillResult.MapWidth / 2, Center.y - fillResult.MapHeight / 2); } }

        /// <summary>
        /// Region center point in world space.
        /// </summary>
        public Vector3 CenterWS { get { return new Vector3(CenterWS2.x, 0f, CenterWS2.y); } }

        // Ring border
        public float BorderSectorLeftAngle { get; private set; } = float.MaxValue; // Angle is in degrees, with range (-180, 180)
        public float BorderSectorRightAngle { get; private set; } = float.MinValue; // Angle is in degrees, with range (-180, 180)
        public float BorderSectorNearRadius { get; private set; } = float.MaxValue;
        public float BorderSectorFarRadius { get; private set; } = float.MinValue;

        public float FlattenedHeight { get; private set; }
        public float FlattenedWidth { get; private set; }

        public bool[,] Map { get; private set; } // filled map
        public bool[,] SubMap { get; private set; } // clipped filled region area.
        public bool[,] FlattenedMap { get; private set; }// flattened clipped filled region area. true = ground, false = wall
        public byte[,] FlattenedMapId { get; private set; } // flattened region with prop_id, see id rules below...
        // public CityProp[,] PropMap { get; private set; }

        private Texture2D mapTex;
        public Texture2D MapTex {
            get {
                if (mapTex == null) {
                    mapTex = Util.BoolMap2Tex(Map, brighterEquals: true);
                }
                return mapTex;
            }
        }
        private Texture2D submapTex;
        public Texture2D SubMapTex {
            get {
                if (submapTex == null) {
                    submapTex = Util.BoolMap2Tex(SubMap, brighterEquals: true);
                }
                return submapTex;
            }
        }
        private Texture2D flattenedMapTex;
        public Texture2D FlattenedMapTex {
            get {
                if (flattenedMapTex == null) {
                    flattenedMapTex = Util.BoolMap2Tex(FlattenedMap, brighterEquals: true);
                }
                return flattenedMapTex;
            }
        }

        /// <summary>
        /// Color added on a pure transparent tex, for combining into a big colored kaleido tex.
        /// </summary>
        public Texture2D ColoredTransparentMapTex { get; private set; }

        /// <summary>
        /// Used for region mask.
        /// </summary>
        public Texture2D TransparentSubMapTex { get; private set; }

        public List<RegionInfo> ConnectedRegion { get; private set; } = new List<RegionInfo>();

        public Color MainColor { get { return themeMaterialHolder.theme.main; } } // TODO: check null

        public int NormalPillarCount { get; private set; } = 0;
        public int MasterPillarCount { get; private set; } = 0;
        public int BuildingCount { get { return buildingProps?.Count ?? -1; } }
        public int RoadCount { get; private set; } = 0;
        public int AllPillarCount { get { return pillarProps.Count; } }
        public int CompanionBallCount { get; private set; } = 0;

        public List<BuildingProp> buildingProps;
        public List<PillarProp> pillarProps;
        public List<RegionPortal> portals;
        public List<PillarProp> badPillars;
        public List<CityProp> props;

        public Transform PropRoot { get; private set; }
        public Transform AutoBallRoot { get; private set; }

        // public GameObject RegionMaskGO { get; set; }

        // debug:
        // public Texture2D debugTex1;
        // public Texture2D debugTex2;
        // public Texture2D debugTex3;
        // public Texture2D debugTex4;
        // ---

        // ------- Gameplay:

        // Actual FSM is in MainGameplayController
        public enum RegionState {
            Uninitialized,
            Dark,
            Unlocking, // playing animations.
            Unlocked
        }
        public RegionState currState = RegionState.Uninitialized;

        public int unlockedPillarCount = 0;

        // ------------ PUBLIC PROPERTIES END

        int flattenedMapHeight, flattenedMapWidth; // float -> int

        FloodFill.FillResult fillResult;

        ThemeMaterialHolder themeMaterialHolder;

        // transform record:
        float angleFromFilledCenterToRight; // in degrees
        Vector2 filledCenterToMapCenter;
        float moveRightLength;
        Vector2 mapCenter;
        float occupiedAngle;

        // spawn params (for cross-function spawn):
        float[,] heatmap;
        PropCollection collection;

        // ---------- public interface ----------

        public RegionInfo(FloodFill.FillResult _fillResult) {
            if (!_fillResult.Finished) {
                throw new System.Exception("Fill result not finished");
            }
            fillResult = _fillResult;
            Init();
        }

        // used in MST
        public void RegisterConnected(IGraphNode other) {
            RegionInfo otherRegion = other as RegionInfo;
            ConnectedRegion.Add(otherRegion);
        }

        public void DoWFC(WFC wfc, int seed) {


            FlattenedMapId = wfc.Run(flattenedMapWidth, flattenedMapHeight, seed);

            // Generate WFC mask:
            // expand road alongside border (for safety reason)
            int width = FlattenedMap.GetLength(0), height = FlattenedMap.GetLength(1);
            byte[,] mask = new byte[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    if (FlattenedMap[x, y]) { // true = ground, false = wall
                        if (x == 0 || x == width - 1 || y == 0 || y == height - 1) { // on the edge (and to prevent out-of-range)
                            mask[x, y] = id_roadNotWalkable;
                        } else {
                            if (FlattenedMap[x - 1, y] &&
                                FlattenedMap[x + 1, y] &&
                                FlattenedMap[x, y - 1] &&
                                FlattenedMap[x, y + 1] &&
                                FlattenedMap[x - 1, y + 1] &&
                                FlattenedMap[x - 1, y - 1] &&
                                FlattenedMap[x + 1, y + 1] &&
                                FlattenedMap[x - 1, y + 1]) {
                                mask[x, y] = 0; // no mask
                            } else {
                                mask[x, y] = id_roadNotWalkable; // expand road
                            }
                        }
                    } else { // wall
                        mask[x, y] = id_wall;
                    }
                }
            }


            // Added: expand another round of road to compensate ring2flatten inaccuracy
            byte[,] oldMask = new byte[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    oldMask[x, y] = mask[x, y];
                }
            }
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    if (oldMask[x, y] == id_roadNotWalkable) { // previously generated DUMMY road block
                        // expand around 3x3
                        for (int offsetX = -1; offsetX <= 1; ++offsetX) {
                            int subX = x + offsetX;
                            if (subX < 0 || subX >= width) continue;
                            for (int offsetY = -1; offsetY <= 1; ++offsetY) {
                                int subY = y + offsetY;
                                if (subY < 0 || subY >= height) continue;

                                // expand:
                                if (oldMask[subX, subY] == 0) { // if wild land
                                    mask[subX, subY] = id_road;
                                }
                            }
                        }
                    }
                }
            }
            

            // apply mask
            for (int y = 0; y < flattenedMapHeight; ++y) {
                for (int x = 0; x < flattenedMapWidth; ++x) {
                    if (mask[x, y] != 0) { // mask not empty(0) -> use mask value
                        FlattenedMapId[x, y] = mask[x, y];
                    }
                }
            }

            //debug:
            /*
            // Debug.Log(Util.ByteMapWithSingleDigitToString(FlattenedMapWFC));
            HashSet<int> maskSet = new HashSet<int> {
                id_road,
                id_masterPillarRoad,
                id_pillarRoad
            };
            // debugTex1 = Util.BoolMap2Tex(Util.ByteMapToBoolMap(FlattenedMapWFC, maskSet), true);
            */
        }

        public void ConstructRegion(float scaleFactor, PropCollection collection, Transform propRoot, Transform autoBallRoot, float perlinFreq, float companionSpawnRatio, float badPillarRatio, ThemeMaterialHolder themeMaterialHolder) {

            this.collection = collection;
            this.PropRoot = propRoot;
            this.AutoBallRoot = autoBallRoot;
            this.themeMaterialHolder = themeMaterialHolder;

            // Step.1: Generate city heat map using Perlin Noise: 0(black) -> less urbanized, 1(white) -> urbanized
            float xOffset = Random.Range(0, 100);
            float yOffset = Random.Range(0, 100);
            heatmap = new float[flattenedMapWidth, flattenedMapHeight];
            for (int x = 0; x < flattenedMapWidth; ++x) {
                for (int y = 0; y < flattenedMapHeight; ++y) {
                    heatmap[x, y] = Mathf.PerlinNoise(x * perlinFreq + xOffset, y * perlinFreq + yOffset);
                }
            }

            // Step.2: analyze where to place building (alongside road):
            int width = FlattenedMapId.GetLength(0), height = FlattenedMapId.GetLength(1);
            for (int x = 1; x < width - 1; ++x) {
                for (int y = 1; y < height - 1; ++y) {
                    if (FlattenedMapId[x, y] == id_empty && (
                        IsRoad(FlattenedMapId[x - 1, y]) || IsRoad(FlattenedMapId[x, y - 1]) || IsRoad(FlattenedMapId[x + 1, y]) || IsRoad(FlattenedMapId[x, y + 1]))) {
                        FlattenedMapId[x, y] = id_building;
                    }
                }
            }

            // Step.3: change crossroad-pillar to id_masterPillarRoad, for spawning master pillar (a kind of pillar that spit out multiple balls..)
            for (int x = 0; x < flattenedMapWidth; ++x) {
                for (int y = 0; y < flattenedMapHeight; ++y) {
                    if (FlattenedMapId[x, y] == id_pillarRoad) {
                        int counter = CountSurroundingRoad(FlattenedMapId, x, y);
                        if (counter == 4) {
                            FlattenedMapId[x, y] = id_masterPillarRoad;
                        } else if (counter == 3) {
                            FlattenedMapId[x, y] = Random.Range(0f, 1f) > 0.5f ? id_masterPillarRoad : id_pillarRoad; // half-half
                        }
                    }
                }
            }

            // Step.4: place actual props:
            // List<CityProp> spawnedList = new List<CityProp>();

            // PropMap = new CityProp[width, height];
            buildingProps = new List<BuildingProp>();
            pillarProps = new List<PillarProp>();
            badPillars = new List<PillarProp>();
            props = new List<CityProp>();
            RoadProp[,] roadPropMap = new RoadProp[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    GameObject spawned = null;
                    switch (FlattenedMapId[x, y]) {
                        case id_masterPillarRoad:
                            spawned = GameObject.Instantiate(collection.GetMasterPillarPrefab(), Vector3.zero, Quaternion.identity);
                            MasterPillarProp masterPillarProp = spawned.GetComponent<MasterPillarProp>();
                            pillarProps.Add(masterPillarProp);
                            masterPillarProp.withCompanionBall = Random.Range(0f, 1f) < companionSpawnRatio;
                            if (masterPillarProp.withCompanionBall) {
                                ++CompanionBallCount;
                            }
                            
                            if (Random.Range(0f, 1f) < badPillarRatio) {
                                badPillars.Add(masterPillarProp);
                            }
                            roadPropMap[x, y] = masterPillarProp;
                            ++MasterPillarCount;
                            break;
                        case id_building:
                            spawned = SpawnBuilding(FlattenedMapId, x, y);
                            buildingProps.Add(spawned.GetComponent<BuildingProp>()); // used by the big boss
                            break;
                        case id_pillarRoad:
                            spawned = GameObject.Instantiate(collection.GetPillarPrefab(), Vector3.zero, Quaternion.identity);
                            PillarProp pillarProp = spawned.GetComponent<PillarProp>();
                            pillarProps.Add(pillarProp);
                            if (Random.Range(0f, 1f) < badPillarRatio) {
                                badPillars.Add(pillarProp);
                            }
                            roadPropMap[x, y] = pillarProp;
                            ++NormalPillarCount;
                            break;
                        case id_road:
                            spawned = GameObject.Instantiate(collection.GetRoadPrefab(), Vector3.zero, Quaternion.identity);
                            RoadProp roadProp = spawned.GetComponent<RoadProp>();
                            roadPropMap[x, y] = roadProp;
                            break;
                        case id_empty:
                            spawned = GameObject.Instantiate(collection.GetEmptyPrefab(), Vector3.zero, Quaternion.identity);
                            break;
                        default:
                            break;
                    }

                    if (spawned == null) {
                        continue;
                    }

                    spawned.transform.position = new Vector3(x, 0, y);
                    spawned.transform.SetParent(propRoot);

                    props.Add(spawned.GetComponent<CityProp>());

                }
            }


            // Step.5: transform back
            foreach (CityProp prop in props) {

                // 1: Prepare per-vertex transform to world position
                List<Vector3[]> tempRingPosVerts = new List<Vector3[]>(prop.meshesToTransform.Count);
                for (int i = 0; i < prop.meshesToTransform.Count; ++i) {
                    Vector3[] modelVerts = prop.meshesToTransform[i].sharedMesh.vertices;
                    for (int j = 0; j < modelVerts.Length; ++j) {
                        modelVerts[j] = prop.meshesToTransform[i].transform.TransformPoint(modelVerts[j]); // pre-process: local position -> flattenmap coord (also current world coord)
                        modelVerts[j] = TransformBack(modelVerts[j]); // flattenmap coord -> ring coord
                        modelVerts[j] = new Vector3(modelVerts[j].x - fillResult.MapWidth / 2, modelVerts[j].y, modelVerts[j].z - fillResult.MapHeight / 2); // filled map -> actual world pos
                        modelVerts[j] *= scaleFactor;
                    }
                    tempRingPosVerts.Add(modelVerts);
                }

                // 2. Transform prop root pos/rot
                Vector3 propOriginalPos = prop.transform.position;
                Vector3 propOriginalPosForwardOne = prop.transform.TransformPoint(Vector3.forward); // move towards +Z for one unit.

                Vector3 propFilledBoolmapPos = TransformBack(propOriginalPos);
                Vector3 propFilledBoolmapPosForwardOne = TransformBack(propOriginalPosForwardOne);

                if (prop.compensateScale) {
                    Vector3 propOriginalPosRightOne = prop.transform.position + Vector3.right; // move towards +X for one unit.
                    Vector3 propFilledBoolmapPosRightOne = TransformBack(propOriginalPosRightOne);
                    float forwardOneDistance = Vector3.Distance(propFilledBoolmapPos, propFilledBoolmapPosForwardOne);
                    float rightOneDistance = Vector3.Distance(propFilledBoolmapPos, propFilledBoolmapPosRightOne);
                    prop.transform.localScale = new Vector3(rightOneDistance, 1f, forwardOneDistance); // scale compensation
                }


                Vector3 propWorldPos = new Vector3(propFilledBoolmapPos.x - fillResult.MapWidth / 2, propFilledBoolmapPos.y, propFilledBoolmapPos.z - fillResult.MapHeight / 2);
                Vector3 propWorldPosForwardOne = new Vector3(propFilledBoolmapPosForwardOne.x - fillResult.MapWidth / 2, propFilledBoolmapPosForwardOne.y, propFilledBoolmapPosForwardOne.z - fillResult.MapHeight / 2);
                //Vector3 propWorldPosRightOne = new Vector3(propOriginalPosRightOne.x - fillResult.MapWidth / 2 , propOriginalPosRightOne.y, propOriginalPosRightOne.z - fillResult.MapHeight / 2);

                propWorldPos *= scaleFactor;
                propWorldPosForwardOne *= scaleFactor;
                prop.transform.position = propWorldPos;
                prop.transform.LookAt(propWorldPosForwardOne);

                // 3. Transform vertex position from world to already transformed local
                for (int i = 0; i < prop.meshesToTransform.Count; ++i) {
                    Vector3[] verts = tempRingPosVerts[i];
                    for (int j = 0; j < verts.Length; ++j) {
                        verts[j] = prop.meshesToTransform[i].transform.InverseTransformPoint(verts[j]); // post-process: world (ring) position -> local position (with newly placed root)
                    }
                    prop.meshesToTransform[i].mesh.SetVertices(verts);
                    prop.meshesToTransform[i].mesh.RecalculateNormals();
                    prop.meshesToTransform[i].mesh.RecalculateBounds();
                }
            }

            // Step.6 construct road network:
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    RoadProp roadProp = roadPropMap[x, y];
                    if (roadProp == null) {
                        continue;
                    }

                    if (x != 0) {
                        roadProp.left = roadPropMap[x - 1, y];
                    }
                    if (x != width - 1) {
                        roadProp.right = roadPropMap[x + 1, y];
                    }
                    if (y != 0) {
                        roadProp.top = roadPropMap[x, y - 1];
                    }
                    if (y != height - 1) {
                        roadProp.bottom = roadPropMap[x, y + 1];
                    }
                }
            }

            // Step.7: Generate bad balls
            foreach (PillarProp pillar in badPillars) {
                GameObject go = GameObject.Instantiate(collection.badBallPrefab, pillar.transform.position, Quaternion.identity, autoBallRoot);
                AutoBallController autoBallController = go.GetComponent<AutoBallController>();
                autoBallController.currRoadProp = pillar;
            }
            autoBallRoot.gameObject.SetActive(false);

            // Step.8: apply theme:
            foreach (CityProp prop in props) {
                prop.SetThemeMaterial(themeMaterialHolder);
            }

            // Step.9: Color Map tex:
            Material colorMat = new Material(Shader.Find("MicroUniverse/ColorRegion"));
            Color color = themeMaterialHolder.theme.main;
            colorMat.SetColor("_Color", color.linear); // gamma correction...
            RenderTexture rt = RenderTexture.GetTemporary(Map.GetLength(0), Map.GetLength(1), 0);
            Graphics.Blit(MapTex, rt, colorMat);
            ColoredTransparentMapTex = Util.RT2Tex(rt);
            RenderTexture.ReleaseTemporary(rt);

            Material b2tMat = new Material(Shader.Find("MicroUniverse/BlackToTransparent"));
            rt = RenderTexture.GetTemporary(SubMap.GetLength(0), SubMap.GetLength(1));
            Graphics.Blit(SubMapTex, rt, b2tMat);
            TransparentSubMapTex = Util.RT2Tex(rt);
            RenderTexture.ReleaseTemporary(rt);
        }

        public RegionPortal FindPortalFromHereTo(int regionId) {
            foreach (RegionPortal portal in portals) {
                if (portal.toRegionId == regionId) {
                    return portal;
                }
            }
            return null;
        }

        public void SetAutoBallRootActive(bool active) {
            AutoBallRoot.gameObject.SetActive(active);
        }

        public void SetPortalActive(bool active) {
            foreach (RegionPortal regionPortal in portals) {
                regionPortal.gameObject.SetActive(true);
                // Play some timeline.
            }
        }

        public void DestroyAutoBalls() {
            foreach (Transform t in AutoBallRoot) {
                GameObject.Destroy(t.gameObject);
            }
            SetAutoBallRootActive(false);
        }

        public void SetAllPillarsActiveWithoutNotifyingController(bool active) {
            foreach (PillarProp prop in pillarProps) {
                if (active) {
                    prop.Activate(notifyController: false);
                } else {
                    prop.Deactivate(notifyController: false);
                }
            }
        }

        public Texture2D DebugTransformBackToTex() {
            List<Vector2> vec2s = new List<Vector2>();
            for (int x = 0; x < FlattenedMapId.GetLength(0); ++x) {
                for (int y = 0; y < FlattenedMapId.GetLength(1); ++y) {
                    if (IsRoad(FlattenedMapId[x, y])) {
                        Vector2 v = TransformBack(new Vector2(x, y));
                        vec2s.Add(v);
                    }
                }
            }
            bool[,] debugBoolMap = Util.PlotPointsToBoolMap(vec2s, MapTex.width, MapTex.height);
            return Util.BoolMap2Tex(debugBoolMap, true);
        }

        public void DebugConnectedRegions() {
            foreach (RegionInfo connected in ConnectedRegion) {
                Debug.Log("Connected: " + RegionID.ToString() + "->" + connected.RegionID.ToString());
            }
        }


        // ---------- public interface ---------- [END]


        GameObject SpawnBuilding(byte[,] map, int x, int y) {
            GameObject spawned;
            Vector3 flattenSpacePos = new Vector3(x, 0, y);
            float heat = heatmap[x, y];

            // building type detection:
            BuildingProp.BuildingType buildingType = BuildingProp.BuildingType.DontCare;
            int turn = 0;

            int surrRoadCount = CountSurroundingRoad(map, x, y);
            if (surrRoadCount == 4) {
                buildingType = BuildingProp.BuildingType.Alone;
                turn = Random.Range(0, 3); // whatever the rotation is...
            } else if (surrRoadCount == 3) {
                buildingType = BuildingProp.BuildingType.Stub;
                turn =
                    (!IsRoad(map, x, y + 1)) ? 2 :
                    (!IsRoad(map, x + 1, y)) ? 3 :
                    (!IsRoad(map, x, y - 1)) ? 0 :
                    1;
            } else if (surrRoadCount == 2) { // corner or alongside both parallel road.
                if (IsRoad(map, x, y + 1)) { // up (3 cases)
                    if (IsRoad(map, x + 1, y)) { // right -> corner
                        buildingType = BuildingProp.BuildingType.Corner;
                        turn = 0;
                    } else if (IsRoad(map, x, y - 1)) { // down -> alongside 2 parallel roads
                        buildingType = BuildingProp.BuildingType.AlongsideTwoRoads;
                        turn = Random.value > 0.5f ? 0 : 2; // facing up (default) or down
                    } else { // left -> corner
                        buildingType = BuildingProp.BuildingType.Corner;
                        turn = 3;
                    }
                } else if (IsRoad(map, x + 1, y)) { // right (2 cases left)
                    if (IsRoad(map, x, y - 1)) { // down -> corner
                        buildingType = BuildingProp.BuildingType.Corner;
                        turn = 1;
                    } else { // left -> alongside 2 parallel roads
                        buildingType = BuildingProp.BuildingType.AlongsideTwoRoads;
                        turn = Random.value > 0.5f ? 1 : 3; // facing right or left

                    }
                } else { // left and down: corner
                    buildingType = BuildingProp.BuildingType.Corner;
                    turn = 2;
                }
            } else { // surrounding road count = 1, must be BuildingType.AlongsideRoad
                buildingType = BuildingProp.BuildingType.AlongsideRoad;
                turn =
                    (IsRoad(map, x, y + 1)) ? 0 :
                    (IsRoad(map, x + 1, y)) ? 1 :
                    (IsRoad(map, x, y - 1)) ? 2 :
                    3;
            }


            GameObject prefab = collection.GetBuildingPrefab(heat, buildingType);
            spawned = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            spawned.transform.Rotate(new Vector3(0f, turn * 90f, 0f));

            return spawned;

        }

        int CountSurroundingRoad(byte[,] map, int x, int y) {
            return (IsRoad(map, x - 1, y) ? 1 : 0) +
                   (IsRoad(map, x + 1, y) ? 1 : 0) +
                   (IsRoad(map, x, y + 1) ? 1 : 0) +
                   (IsRoad(map, x, y - 1) ? 1 : 0);
        }

        bool IsRoad(byte id) {
            return id == id_road || id == id_masterPillarRoad || id == id_pillarRoad;
        }

        bool IsRoad(byte[,] map, int x, int y) {
            if (IsOutside(map, x, y)) return false;
            return IsRoad(map[x, y]);
        }


        bool IsOutside<T>(T[,] map, int x, int y) {
            return x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1);
        }

        void Init() {
            GenerateMap();
            GenerateSubMap();
            Ring2FlattenTransform();
        }

        void GenerateSubMap(bool fillValue = true) {
            int subHeight = fillResult.BorderYMax - fillResult.BorderYMin + 1;
            int subWidth = fillResult.BorderXMax - fillResult.BorderXMin + 1;
            SubMap = new bool[subWidth, subHeight];

            for (int y = 0; y < subHeight; ++y) {
                for (int x = 0; x < subWidth; ++x) {
                    SubMap[x, y] = !fillValue;
                }
            }

            foreach (Vector2Int p in fillResult.FilledPoints) {
                int subX = p.x - fillResult.BorderXMin;
                int subY = p.y - fillResult.BorderYMin;
                SubMap[subX, subY] = fillValue;
            }
        }

        void GenerateMap(bool fillValue = true) {
            Map = new bool[fillResult.MapWidth, fillResult.MapHeight];
            foreach (Vector2Int p in fillResult.FilledPoints) {
                Map[p.x, p.y] = fillValue;
            }
        }


        /// <summary>
        /// First find out how to construct a flattened map, (Pass 1)
        /// Then transform point from filled points to flattened map (Pass 2)
        /// </summary>
        void Ring2FlattenTransform() {

            // see sketch for details.
            // all those fancy transforms are to prevent one thing: sudden change of angle like -180 -> 180

            mapCenter = new Vector2(fillResult.MapCenterPoint.x, fillResult.MapCenterPoint.y);

            // use float based Vector2 for highter accuracy.
            filledCenterToMapCenter = mapCenter - fillResult.FilledAreaCenterPoint;

            Vector2 mapCenterToFilledCenter = -filledCenterToMapCenter;
            Vector2 mapCenterToFilledCenterDirection = mapCenterToFilledCenter.normalized;
            moveRightLength = filledCenterToMapCenter.magnitude;


            // should be kept.
            angleFromFilledCenterToRight = Vector2.SignedAngle(mapCenterToFilledCenterDirection, Vector2.right); // in degree, [-180, 180]

            List<Vector2> transformed = new List<Vector2>(fillResult.FilledPoints.Count);
            for (int i = 0; i < fillResult.FilledPoints.Count; ++i) {
                transformed.Add(fillResult.FilledPoints[i]);
            }

            // ------------------------
            // Pass 1: Iterate all points in 2D world space inside a ring, transform them to radial coord and find out flattened texture's width and height.
            for (int i = 0; i < fillResult.FilledPoints.Count; ++i) {

                // step.0: let map center point be in origin!
                transformed[i] -= mapCenter;

                // step.1: update sector near/far radius.
                float radius = transformed[i].magnitude;
                if (radius > BorderSectorFarRadius) {
                    BorderSectorFarRadius = radius;
                } else if (radius < BorderSectorNearRadius) {
                    BorderSectorNearRadius = radius;
                }

                // step.2: 2D rotation. TODO: consider using matrix.
                // move to center:
                transformed[i] += filledCenterToMapCenter;
                // 2D rotation to move the pattern to the right:
                transformed[i] = transformed[i].Rotate(angleFromFilledCenterToRight);
                // move to the right:
                transformed[i] += Vector2.right * moveRightLength;

                // step.3: Calc left/right angle boundary.
                float theta = Vector2.SignedAngle(Vector2.right, transformed[i]); // in degrees
                if (theta > BorderSectorRightAngle) {
                    BorderSectorRightAngle = theta;
                } else if (theta < BorderSectorLeftAngle) {
                    BorderSectorLeftAngle = theta;
                }

                // write (radius, angleInDegree) to transformed
                transformed[i] = new Vector2(radius, theta);

            }

            // debugTex1 = Util.BoolMap2Tex(Util.PlotPointsToBoolMap(debug1, fillResult.RowSize, fillResult.ColSize, true), true);

            // double check
            /*
            Debug.Log("BorderSectorNearRadius: " + BorderSectorNearRadius.ToString());
            Debug.Log("BorderSectorFarRadius: " + BorderSectorFarRadius.ToString());
            Debug.Log("BorderSectorLeftAngle: " + BorderSectorLeftAngle.ToString());
            Debug.Log("BorderSectorRightAngle: " + BorderSectorRightAngle.ToString());
            */
            if (BorderSectorNearRadius >= BorderSectorFarRadius || BorderSectorLeftAngle >= BorderSectorRightAngle || BorderSectorLeftAngle >= 0f || BorderSectorRightAngle <= 0f) {
                throw new System.Exception("Something went wrong...");
            }

            // calc flattened texture width / height
            FlattenedWidth = BorderSectorFarRadius - BorderSectorNearRadius;
            float middleRadius = (BorderSectorFarRadius + BorderSectorNearRadius) / 2f;
            occupiedAngle = BorderSectorRightAngle - BorderSectorLeftAngle;
            FlattenedHeight = 2f * Mathf.PI * middleRadius * occupiedAngle / 360f;

            // ------------------------
            // Pass 2: Transform all points in radial coord (done in pass 1) to flattened's coord.
            // currently elements in `transformed` are in radial coord (r, theta).
            for (int i = 0; i < transformed.Count; ++i) {
                float r = transformed[i].x, theta = transformed[i].y;
                float flattenedX = r - BorderSectorNearRadius;
                float flattenedY = (theta - BorderSectorLeftAngle) / occupiedAngle * FlattenedHeight;
                transformed[i] = new Vector2(flattenedX, flattenedY);
            }

            // Convert point list to bool map (can be optimized. seperate for-loop for easy understanding)
            // Lose accuracy!! Pay attention!
            flattenedMapWidth = Mathf.RoundToInt(FlattenedWidth);
            flattenedMapHeight = Mathf.RoundToInt(FlattenedHeight);
            FlattenedMap = Util.PlotPointsToBoolMap(transformed, flattenedMapWidth, flattenedMapHeight, true);

        }

        /// <summary>
        /// Inverse transform of Ring2FlattenTransform
        /// transform flatten space (r/c) to fill space (r/c)
        /// </summary>
        private Vector2 TransformBack(Vector2 pos) {

            /*
            // Step.1: correct
            pos.x = pos.x / flattenedMapWidth * FlattenedWidth;
            pos.y = pos.y / flattenedMapHeight * FlattenedHeight;
            */

            // Step.2: 2D -> radial coord r/theta
            float r = pos.x + BorderSectorNearRadius;
            float theta = pos.y / FlattenedHeight * occupiedAngle + BorderSectorLeftAngle;

            // step.3: radial coord -> to-the-right euler coord.
            pos.x = r * Mathf.Cos(theta * Mathf.Deg2Rad);
            pos.y = r * Mathf.Sin(theta * Mathf.Deg2Rad);

            // step.4: rotate back (move left, rotate back, move back)
            pos -= Vector2.right * moveRightLength;
            pos *= 0.95f; // The precision is not good, so shrink it down a little bit...It just works.
            pos = pos.Rotate(-angleFromFilledCenterToRight);
            pos -= filledCenterToMapCenter;
            pos += mapCenter;

            return pos;
        }

        private Vector3 TransformBack(Vector3 pos) {

            Vector2 pos2d = new Vector2(pos.x, pos.z);
            Vector2 transformed = TransformBack(pos2d);
            Vector3 ret = new Vector3(transformed.x, pos.y, transformed.y);
            return ret;
        }
    }

}
