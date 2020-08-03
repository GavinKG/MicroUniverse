using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class RegionInfo : IGraphNode {

        // ID rules:
        const int id_empty = 0;
        const int id_road = 1;
        const int id_pillar = 2;
        const int id_crossroad = 3; // crossroad must have a super-pillar
        const int id_wall = 4;
        const int id_building = 5;

        // ------------ PUBLIC PROPERTIES:

        // ------- Loading time:

        public int RegionID { get; set; }

        /// <summary>
        /// Region center point in filled space.
        /// </summary>
        public Vector2Int Center { get { return fillResult.FilledAreaCenterPoint; } }

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

        public List<RegionInfo> ConnectedRegion { get; private set; } = new List<RegionInfo>();

        public int PillarCount { get; private set; } = 0;
        public int MasterPillarCount { get; private set; } = 0;
        public int BuildingCount { get; private set; } = 0;
        public int RoadCount { get; private set; } = 0;
        public int AllPillarCount { get { return PillarCount + MasterPillarCount; } }

        // debug:
        // public Texture2D debugTex1;
        // public Texture2D debugTex2;
        // public Texture2D debugTex3;
        // public Texture2D debugTex4;
        // ---

        // ------- Gameplay:

        public enum RegionState {
            Locked, Unlocking, Unlocked
        }
        public RegionState currState = RegionState.Locked;
        public List<RegionPortal> portals;
        public int unlockedPillar = 0;


        // ------------ PUBLIC PROPERTIES END

        int flattenedMapHeight, flattenedMapWidth; // float -> int

        FloodFill.FillResult fillResult;

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

            // expand road alongside border (for safety reason)
            int width = FlattenedMap.GetLength(0), height = FlattenedMap.GetLength(1);
            byte[,] mask = new byte[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    if (FlattenedMap[x, y]) { // ground
                        if (x == 0 || x == width - 1 || y == 0 || y == height - 1) { // on the edge
                            mask[x, y] = id_road;
                        } else {
                            if (FlattenedMap[x - 1, y] &&
                                FlattenedMap[x + 1, y] &&
                                FlattenedMap[x, y - 1] &&
                                FlattenedMap[x, y + 1] &&
                                FlattenedMap[x - 1, y + 1] &&
                                FlattenedMap[x - 1, y - 1] &&
                                FlattenedMap[x + 1, y + 1] &&
                                FlattenedMap[x - 1, y + 1]) {
                                mask[x, y] = id_empty;
                            } else {
                                mask[x, y] = id_road;
                            }
                        }
                    } else { // wall
                        mask[x, y] = id_wall;
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
            // Debug.Log(Util.ByteMapWithSingleDigitToString(FlattenedMapWFC));
            HashSet<int> maskSet = new HashSet<int> {
                id_road,
                id_crossroad,
                id_pillar
            };
            // debugTex1 = Util.BoolMap2Tex(Util.ByteMapToBoolMap(FlattenedMapWFC, maskSet), true);
        }

        public void PlantProps(float scaleFactor, PropCollection collection, Transform propRoot, float perlinFreq, float companionSpawnRatio) {

            this.collection = collection;

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

            // Step.3: change crossroad-pillar to id_crossroad, for spawning master pillar (a kind of pillar that spit out multiple balls..)
            for (int x = 0; x < flattenedMapWidth; ++x) {
                for (int y = 0; y < flattenedMapHeight; ++y) {
                    if (FlattenedMapId[x, y] == id_pillar && CountSurroundingRoad(FlattenedMapId, x, y) == 4) {
                        FlattenedMapId[x, y] = id_crossroad;
                    }
                }
            }

            // Step.4: place actual props:
            List<CityProp> spawnedList = new List<CityProp>();
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    GameObject spawned = null;
                    switch (FlattenedMapId[x, y]) {
                        case id_crossroad:
                            spawned = GameObject.Instantiate(collection.GetMasterPillarPrefab(), Vector3.zero, Quaternion.identity);
                            MasterPillarProp masterPillarProp = spawned.GetComponent<MasterPillarProp>();
                            masterPillarProp.companionBallCount = Random.Range(0f, 1f) < companionSpawnRatio ? 1 : 0;
                            ++MasterPillarCount;
                            break;
                        case id_building:
                            spawned = SpawnBuilding(FlattenedMapId, x, y);
                            ++BuildingCount;
                            break;
                        case id_pillar:
                            spawned = GameObject.Instantiate(collection.GetPillarPrefab(), Vector3.zero, Quaternion.identity);
                            ++PillarCount;
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
                    spawnedList.Add(spawned.GetComponent<CityProp>());

                }
            }

            // Step.5: transform back:
            foreach (CityProp prop in spawnedList) {

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

        }

        public RegionPortal FindPortalFromHereTo(int regionId) {
            foreach (RegionPortal portal in portals) {
                if (portal.toRegionId == regionId) {
                    return portal;
                }
            }
            return null;
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
            return id == id_road || id == id_crossroad || id == id_pillar;
        }

        bool IsRoad(byte[,] map, int x, int y) {
            if (IsOutside(map, x, y)) return false;
            return IsRoad(map[x, y]);
        }

        bool IsOutside(byte[,] map, int x, int y) {
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
            flattenedMapWidth = Mathf.CeilToInt(FlattenedWidth);
            flattenedMapHeight = Mathf.CeilToInt(FlattenedHeight);
            FlattenedMap = Util.PlotPointsToBoolMap(transformed, flattenedMapWidth, flattenedMapHeight, true);

        }

        /// <summary>
        /// Inverse transform of Ring2FlattenTransform
        /// transform flatten space (r/c) to fill space (r/c)
        /// </summary>
        private Vector2 TransformBack(Vector2 pos) {

            // Step.2: 2D -> radial coord r/theta
            float r = pos.x + BorderSectorNearRadius;
            float theta = pos.y / FlattenedHeight * occupiedAngle + BorderSectorLeftAngle;

            // step.3: radial coord -> to-the-right euler coord.
            pos.x = r * Mathf.Cos(theta * Mathf.Deg2Rad);
            pos.y = r * Mathf.Sin(theta * Mathf.Deg2Rad);

            // step.4: rotate back (move left, rotate back, move back)
            pos -= Vector2.right * moveRightLength;
            pos *= 0.9f; // The precision is not good, so shrink it down a little bit...It just works.
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
