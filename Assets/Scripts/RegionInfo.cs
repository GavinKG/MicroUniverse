using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class RegionInfo : IGraphNode {

        public Vector2Int Center { get { return fillResult.FilledAreaCenterPoint; } }
        public int TileCount { get { return fillResult.FilledPoints.Count; } }

        // TODO: Lazy fetch!!!!!

        // Ring border
        public float BorderSectorLeftAngle { get; private set; } = float.MaxValue; // Angle is in degrees, with range (-180, 180)
        public float BorderSectorRightAngle { get; private set; } = float.MinValue; // Angle is in degrees, with range (-180, 180)
        public float BorderSectorNearRadius { get; private set; } = float.MaxValue;
        public float BorderSectorFarRadius { get; private set; } = float.MinValue;

        public float FlattenedHeight { get; private set; }
        public float FlattenedWidth { get; private set; }

        public bool[,] Map { get; private set; }
        public bool[,] SubMap { get; private set; } // clipped region area.
        public bool[,] FlattenedMap { get; private set; }// flattened clipped region area. true = ground, false = wall
        public byte[,] FlattenedMapWFC { get; private set; }
        public Texture2D MapTex { get; private set; }
        public Texture2D SubMapTex { get; private set; }
        public Texture2D FlattenedMapTex { get; private set; }

        public List<RegionInfo> ConnectedRegion { get; private set; } = new List<RegionInfo>();

        // debug:
        public Texture2D debugTex1;
        // public Texture2D debugTex2;
        // public Texture2D debugTex3;
        // public Texture2D debugTex4;
        // ---

        private int flattenedMapHeight, flattenedMapWidth; // float -> int

        FloodFill.FillResult fillResult;

        // transform record:
        float angleFromFilledCenterToRight; // in degrees
        Vector2 filledCenterToMapCenter;
        float moveRightLength;
        Vector2 mapCenter;
        float occupiedAngle;

        // ID rules:
        const int empty = 0;
        const int road = 1;
        const int fountainRoad = 2;
        const int pillarRoad = 3;
        const int wall = 4;
        const int building = 5;



        public RegionInfo(FloodFill.FillResult _fillResult) {
            if (!_fillResult.Finished) {
                throw new System.Exception("Fill result not finished");
            }
            fillResult = _fillResult;
            DoCalc();
        }


        // ---------- public interface ----------

        // used in MST
        public void RegisterConnected(IGraphNode other) {
            ConnectedRegion.Add(other as RegionInfo);
        }

        public void DoWFC(WFC wfc, int seed) {


            FlattenedMapWFC = wfc.Run(flattenedMapWidth, flattenedMapHeight, seed);

            // expand road alongside border (for safety reason)
            int width = FlattenedMap.GetLength(0), height = FlattenedMap.GetLength(1);
            byte[,] mask = new byte[width, height];
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    if (FlattenedMap[x, y]) { // ground
                        if (x == 0 || x == width - 1 || y == 0 || y == height - 1) { // on the edge
                            mask[x, y] = road;
                        } else {
                            if (FlattenedMap[x - 1, y] &&
                                FlattenedMap[x + 1, y] &&
                                FlattenedMap[x, y - 1] &&
                                FlattenedMap[x, y + 1] &&
                                FlattenedMap[x - 1, y + 1] &&
                                FlattenedMap[x - 1, y - 1] &&
                                FlattenedMap[x + 1, y + 1] &&
                                FlattenedMap[x - 1, y + 1]) {
                                mask[x, y] = empty;
                            } else {
                                mask[x, y] = road;
                            }
                        }
                    } else { // wall
                        mask[x, y] = wall;
                    }
                }
            }

            // apply mask
            for (int y = 0; y < flattenedMapHeight; ++y) {
                for (int x = 0; x < flattenedMapWidth; ++x) {
                    if (mask[x, y] != 0) { // mask not empty(0) -> use mask value
                        FlattenedMapWFC[x, y] = mask[x, y];
                    }
                }
            }

            //debug:
            // Debug.Log(Util.ByteMapWithSingleDigitToString(FlattenedMapWFC));
            HashSet<int> maskSet = new HashSet<int> {
                road,
                fountainRoad,
                pillarRoad
            };
            debugTex1 = Util.BoolMap2Tex(Util.ByteMapToBoolMap(FlattenedMapWFC, maskSet), true);
        }

        public void PlantProps(float scaleFactor, GameObject emptyPrefab, GameObject fountainPrefab, GameObject buildingPrefab, GameObject pillarPrefab, Transform propRoot) {

            // Step.1: analyze where to place building (alongside road):
            int width = FlattenedMapWFC.GetLength(0), height = FlattenedMapWFC.GetLength(1);
            for (int x = 1; x < width - 1; ++x) {
                for (int y = 1; y < height - 1; ++y) {
                    if (FlattenedMapWFC[x, y] == empty && (
                        IsRoad(FlattenedMapWFC[x - 1, y]) || IsRoad(FlattenedMapWFC[x, y - 1]) || IsRoad(FlattenedMapWFC[x + 1, y]) || IsRoad(FlattenedMapWFC[x, y + 1]))) {
                        FlattenedMapWFC[x, y] = building;
                    }
                }
            }

            // Step.2: place actual props (in FlattenedMap coord, 1 pixel = 1 unity unit):
            List<CityProp> spawnedList = new List<CityProp>();
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    GameObject spawned = null;
                    Vector3 flattenSpacePos = new Vector3(x, 0, y);
                    switch (FlattenedMapWFC[x, y]) {
                        case fountainRoad:
                            spawned = GameObject.Instantiate(fountainPrefab, flattenSpacePos, Quaternion.identity, propRoot);
                            break;
                        case building:
                            spawned = GameObject.Instantiate(buildingPrefab, flattenSpacePos, Quaternion.identity, propRoot);
                            break;
                        case pillarRoad:
                            spawned = GameObject.Instantiate(pillarPrefab, flattenSpacePos, Quaternion.identity, propRoot);
                            break;
                        case empty:
                            spawned = GameObject.Instantiate(emptyPrefab, flattenSpacePos, Quaternion.identity, propRoot);
                            break;
                        default:
                            break;
                    }
                    if (spawned != null) {
                        spawnedList.Add(spawned.GetComponent<CityProp>());
                    }
                }
            }

            // Step.3: transform back:
            foreach (CityProp prop in spawnedList) {

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

                
                Vector3 newFilledBoolmapPos = TransformBack(prop.transform.position);
                Vector3 newWorldPos = new Vector3(newFilledBoolmapPos.x - fillResult.MapWidth / 2 , newFilledBoolmapPos.y, newFilledBoolmapPos.z - fillResult.MapHeight / 2);
                newWorldPos *= scaleFactor;
                prop.transform.position = newWorldPos; // for shader center point, uhhhhhhhh
                // prop.transform.localScale *= scaleFactor;


                for (int i = 0; i < prop.meshesToTransform.Count; ++i) {
                    Vector3[] verts = tempRingPosVerts[i];
                    for (int j = 0; j < verts.Length; ++j) {
                        verts[j] = prop.meshesToTransform[i].transform.InverseTransformPoint(verts[j]); // post-process: world (ring) position -> local position (with newly placed root)
                    }
                    prop.meshesToTransform[i].mesh.SetVertices(verts);
                    prop.meshesToTransform[i].mesh.RecalculateNormals();
                    prop.meshesToTransform[i].mesh.RecalculateBounds();
                }

                // debugging:
                Vector3 localScale = prop.transform.localScale;
                localScale.y = Random.Range(0.2f, 1f);
                prop.transform.localScale = localScale;

            }

        }

        public Texture2D DebugTransformBackToTex() {
            List<Vector2> vec2s = new List<Vector2>();
            for (int x = 0; x < FlattenedMapWFC.GetLength(0); ++x) {
                for (int y = 0; y < FlattenedMapWFC.GetLength(1); ++y) {
                    if (IsRoad(FlattenedMapWFC[x, y])) {
                        Vector2 v = TransformBack(new Vector2(x, y));
                        vec2s.Add(v);
                    }
                }
            }
            bool[,] debugBoolMap = Util.PlotPointsToBoolMap(vec2s, MapTex.width, MapTex.height);
            return Util.BoolMap2Tex(debugBoolMap, true);
        }

        // ---------- public interface ---------- [END]

        bool IsRoad(byte id) {
            return id == road || id == fountainRoad || id == pillarRoad;
        }


        void DoCalc() {
            GenerateMap();
            GenerateSubMap();
            Ring2FlattenTransform();
            MapTex = Util.BoolMap2Tex(Map, brighterEquals: true);
            SubMapTex = Util.BoolMap2Tex(SubMap, brighterEquals: true);
            FlattenedMapTex = Util.BoolMap2Tex(FlattenedMap, brighterEquals: true);
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
