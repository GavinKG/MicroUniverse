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

        public float FlattenedWidth { get; private set; }
        public float FlattenedHeight { get; private set; }

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
        public Texture2D debugTex2;
        public Texture2D debugTex3;
        public Texture2D debugTex4;
        // ---

        private int flattenedTexWidth, flattenedTexHeight; // float -> int

        FloodFill.FillResult fillResult;

        // transform record:
        float angleFromFilledCenterToRight; // in degrees
        Vector2 filledCenterToMapCenter;
        readonly static Vector2 right = new Vector2(0, 1); // not Vector2.right!
        float moveRightLength;

        // generated mesh vertices/indices (deprecieated)
        /*
        public List<Vector3> RoadNetworkCoverVertices { get; private set; }
        public List<Vector3> RoadNetworkWallVertices { get; private set; }
        public List<int> RoadNetworkCoverIndices { get; private set; }
        public List<int> RoadNetworkWallIndices { get; private set; }
        public Mesh RoadNetworkCoverMesh { get; private set; }
        public Mesh RoadNetworkWallMesh { get; private set; }
        */

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

            // ID rules:
            const int empty = 0;
            const int road = 1;
            const int fountainRoad = 2;
            const int pillarRoad = 3;
            const int wall = 4;

            FlattenedMapWFC = wfc.Run(flattenedTexHeight, flattenedTexWidth, seed); // here we follow (row, col) convension

            // expand road alongside border (for safety reason)
            int rowSize = FlattenedMap.GetLength(0), colSize = FlattenedMap.GetLength(1);
            byte[,] mask = new byte[rowSize, colSize];
            for (int r = 0; r < FlattenedMap.GetLength(0); ++r) {
                for (int c = 0; c < FlattenedMap.GetLength(1); ++c) {
                    if (FlattenedMap[r, c]) { // ground
                        if (r == 0 || r == rowSize - 1 || c == 0 || c == colSize - 1) { // on the edge
                            mask[r, c] = road;
                        } else {
                            if (FlattenedMap[r - 1, c] &&
                                FlattenedMap[r + 1, c] &&
                                FlattenedMap[r, c - 1] &&
                                FlattenedMap[r, c + 1] &&
                                FlattenedMap[r - 1, c + 1] &&
                                FlattenedMap[r - 1, c - 1] &&
                                FlattenedMap[r + 1, c + 1] &&
                                FlattenedMap[r - 1, c + 1]) {
                                mask[r, c] = empty;
                            } else {
                                mask[r, c] = road;
                            }
                        }
                    } else { // wall
                        mask[r, c] = wall;
                    }
                }
            }

            // apply mask
            for (int r = 0; r < flattenedTexHeight; ++r) {
                for (int c = 0; c < flattenedTexWidth; ++c) {
                    if (mask[r, c] != 0) { // mask not empty(0) -> use mask value
                        FlattenedMapWFC[r, c] = mask[r, c];
                    }
                }
            }

            //debug:
            // Debug.Log(Util.ByteMapWithSingleDigitToString(FlattenedMapWFC));
            HashSet<int> maskSet = new HashSet<int>();
            maskSet.Add(road);
            maskSet.Add(fountainRoad);
            maskSet.Add(pillarRoad);
            debugTex1 = Util.BoolMap2Tex(Util.ByteMapToBoolMap(FlattenedMapWFC, maskSet), true);
        }

        public void PlantProps() {

        }

        // ---------- public interface ----------




        void DoCalc() {
            GenerateMap();
            GenerateSubMap();
            Ring2FlattenTransform();
            MapTex = Util.BoolMap2Tex(Map, brighterEquals: true);
            SubMapTex = Util.BoolMap2Tex(SubMap, brighterEquals: true);
            FlattenedMapTex = Util.BoolMap2Tex(FlattenedMap, brighterEquals: true);
        }

        void GenerateSubMap(bool fillValue = true) {
            int subWidth = fillResult.BorderColMax - fillResult.BorderColMin + 1;
            int subHeight = fillResult.BorderRowMax - fillResult.BorderRowMin + 1;
            SubMap = new bool[subHeight, subWidth];

            for (int r = 0; r < subHeight; ++r) {
                for (int c = 0; c < subWidth; ++c) {
                    SubMap[r, c] = !fillValue;
                }
            }

            foreach (Vector2Int p in fillResult.FilledPoints) {
                int subRow = p.x - fillResult.BorderRowMin;
                int subCol = p.y - fillResult.BorderColMin;
                SubMap[subRow, subCol] = fillValue;
            }
        }

        void GenerateMap(bool fillValue = true) {
            Map = new bool[fillResult.RowSize, fillResult.ColSize];
            foreach (Vector2Int p in fillResult.FilledPoints) {
                Map[p.x, p.y] = fillValue;
            }
        }

        void Ring2FlattenTransform() {

            // see sketch for details.
            // all those fancy transforms are to prevent one thing: sudden change of angle like -180 -> 180

            Vector2 mapCenter = new Vector2(fillResult.MapCenterPoint.x, fillResult.MapCenterPoint.y);

            // use float based Vector2 for highter accuracy.
            filledCenterToMapCenter = new Vector2(
                mapCenter.x - fillResult.FilledAreaCenterPoint.x,
                mapCenter.y - fillResult.FilledAreaCenterPoint.y
            );
            Vector2 mapCenterToFilledCenter = -filledCenterToMapCenter;
            Vector2 mapCenterToFilledCenterDirection = filledCenterToMapCenter.normalized;
            moveRightLength = filledCenterToMapCenter.magnitude;


            // should be kept.
            angleFromFilledCenterToRight = Vector2.SignedAngle(mapCenterToFilledCenterDirection, right); // in degree, [-180, 180]

            List<Vector2> transformed = new List<Vector2>(fillResult.FilledPoints.Count);
            for (int i = 0; i < fillResult.FilledPoints.Count; ++i) {
                transformed.Add(fillResult.FilledPoints[i]);
            }

            // ------------------------
            // Pass 1: Iterate all points in 2D world space inside a ring, transform them to radial coord and find out flattened texture's width and height.
            for (int i = 0; i < fillResult.FilledPoints.Count; ++i) {

                // step.0: let map center point be zero!
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
                transformed[i] += right * moveRightLength;

                // step.3: Calc left/right angle boundary.
                float angle = Vector2.SignedAngle(right, transformed[i]); // in degrees
                if (angle > BorderSectorRightAngle) {
                    BorderSectorRightAngle = angle;
                } else if (angle < BorderSectorLeftAngle) {
                    BorderSectorLeftAngle = angle;
                }

                // write (radius, angleInDegree) to transformed
                transformed[i] = new Vector2(radius, angle);

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
            FlattenedHeight = BorderSectorFarRadius - BorderSectorNearRadius;
            float middleRadius = (BorderSectorFarRadius + BorderSectorNearRadius) / 2f;
            float occupiedAngle = BorderSectorRightAngle - BorderSectorLeftAngle;
            FlattenedWidth = 2f * Mathf.PI * middleRadius * occupiedAngle / 360f;

            // ------------------------
            // Pass 2: Transform all points in radial coord (done in pass 1) to flattened tex's coord.
            // currently elements in `transformed` are in radial coord (r, theta).
            for (int i = 0; i < transformed.Count; ++i) {
                float flattenedX = transformed[i].x - BorderSectorNearRadius;
                float flattenedY = (transformed[i].y - BorderSectorLeftAngle) / occupiedAngle * FlattenedWidth; // directly use degree value as tex height
                transformed[i] = new Vector2(flattenedX, flattenedY);
            }

            // Convert point list to bool map (can be optimized. seperate for-loop for easy understanding)
            flattenedTexWidth = Mathf.CeilToInt(FlattenedWidth);
            flattenedTexHeight = Mathf.CeilToInt(FlattenedHeight);
            FlattenedMap = Util.PlotPointsToBoolMap(transformed, flattenedTexHeight, flattenedTexWidth, true);

        }
        
        // depreciated.
        /*
        public void MarchingSquareRoadnetwork(int upscaleFactor, float wallHeight, int smoothCount, float smoothRatio, float widthRatio) {
            HashSet<int> trueMask = new HashSet<int>();
            trueMask.Add(1);
            trueMask.Add(2);
            trueMask.Add(3);
            bool[,] roadmap = Util.ByteMapToBoolMap(FlattenedMapWFC, trueMask); // mask road to bool map
            bool[,] upscaled = roadmap;
            if (upscaleFactor > 1) {
                debugTex1 = Util.BoolMap2Tex(roadmap, true);
                debugTex2 = Util.Upsample(debugTex1, 2);
                debugTex3 = Util.BoolMap2Tex(Util.Tex2BoolMap(debugTex2, true, widthRatio), true);
                upscaled = Util.Upscale(roadmap, upscaleFactor, widthRatio);
            }
            MarchingSquare mc = new MarchingSquare(); // feel the groove!
            mc.GenerateMesh(upscaled, 1f / upscaleFactor, wallHeight, smoothCount, smoothRatio, false);

            RoadNetworkCoverVertices = mc.CoverVertices;
            RoadNetworkWallVertices = mc.WallVertices;
            RoadNetworkCoverIndices = mc.CoverIndices;
            RoadNetworkWallIndices = mc.WallIndices;


            // roadNetworkCoverMesh = mc.CoverMesh;
            // roadNetworkWallMesh = mc.WallMesh;
        }
        */


        private void TransformBackInPlace(List<Vector3> originals) {
            for (int i = 0; i < originals.Count; ++i) {
                originals[i] = TransformBack(originals[i]);
            }
        }

        private List<Vector3> TransformBack(List<Vector3> originals) {
            List<Vector3> ret = new List<Vector3>(originals); // "deep copy"
            TransformBackInPlace(ret);
            return ret;
        }

        /// <summary>
        /// Inverse transform of Ring2FlattenTransform
        /// Original position should strictly follows FlattenMap coord.
        /// </summary>
        private Vector3 TransformBack(Vector3 original) {

            // Step.1: flatten 3D -> 2D
            Vector2 pos = new Vector2(original.x, original.z);

            // Step.2: 2D -> radial coord r/theta
            float r = pos.x + FlattenedWidth / 2f + BorderSectorNearRadius;
            float theta = pos.y;

            // step.3: radial coord -> to-the-right euler coord.
            pos.x = r * Mathf.Cos(theta * Mathf.Deg2Rad);
            pos.y = r * Mathf.Sin(theta * Mathf.Deg2Rad);

            // step.4: rotate back (move left, rotate back, move back)
            pos -= right * moveRightLength;
            pos.Rotate(-angleFromFilledCenterToRight);
            pos -= filledCenterToMapCenter;

            // step.5: reconstruct
            Vector3 ret = new Vector3(pos.x, 0, pos.y);
            return ret;
        }
    }

}
