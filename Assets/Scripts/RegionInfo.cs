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
        public Mesh roadNetworkCover;
        public Mesh roadNetworkWall;
        
        public Texture2D debugTex1;
        public Texture2D debugTex2;
        public Texture2D debugTex3;
        public Texture2D debugTex4;

        private int flattenedTexWidth, flattenedTexHeight; // float -> int

        FloodFill.FillResult fillResult;

        public RegionInfo(FloodFill.FillResult _fillResult) {
            if (!_fillResult.Finished) {
                throw new System.Exception("Fill result not finished");
            }
            fillResult = _fillResult;
            DoCalc();
        }

        void DoCalc() {
            GenerateMap();
            GenerateSubMap();
            Ring2FlattenTransform();
            MapTex = Util.BoolMap2Tex(Map, brighterEquals: true);
            SubMapTex = Util.BoolMap2Tex(SubMap, brighterEquals: true);
            FlattenedMapTex = Util.BoolMap2Tex(FlattenedMap, brighterEquals: true);
        }

        public void GenerateSubMap(bool fillValue = true) {
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

        public void GenerateMap(bool fillValue = true) {
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
            Vector2 filledCenterToMapCenter = new Vector2(
                mapCenter.x - fillResult.FilledAreaCenterPoint.x,
                mapCenter.y - fillResult.FilledAreaCenterPoint.y
            );
            Vector2 mapCenterToFilledCenter = -filledCenterToMapCenter;
            Vector2 mapCenterToFilledCenterDirection = filledCenterToMapCenter.normalized;
            float length = filledCenterToMapCenter.magnitude;
            Vector2 right = new Vector2(0, 1); // not Vector2.right!

            // should be kept.
            float angleFromFilledCenterToRight = Vector2.SignedAngle(mapCenterToFilledCenterDirection, right); // in degree, [-180, 180]

            List<Vector2> transformed = new List<Vector2>(fillResult.FilledPoints.Count);
            for (int i = 0; i < fillResult.FilledPoints.Count; ++i) {
                transformed.Add(fillResult.FilledPoints[i]);
            }


            // Pass 1: Iterate all points in 2D world space inside a ring, transform them to radial coord and find out flattened texture's width and height.
            for (int i = 0; i < fillResult.FilledPoints.Count; ++i) {

                transformed[i] -= mapCenter; // let map center point be zero!

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
                transformed[i] += right * length;

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

            // Pass 2: Transform all points in radial coord (done in pass 1) to flattened tex's coord.
            for (int i = 0; i < transformed.Count; ++i) {
                float flattenedX = transformed[i].x - BorderSectorNearRadius;
                float flattenedY = (transformed[i].y - BorderSectorLeftAngle) / occupiedAngle * FlattenedWidth;
                transformed[i] = new Vector2(flattenedX, flattenedY);
            }

            // Convert point list to bool map (can be optimized. seperate for-loop for easy understanding)
            flattenedTexWidth = Mathf.CeilToInt(FlattenedWidth);
            flattenedTexHeight = Mathf.CeilToInt(FlattenedHeight);
            FlattenedMap = Util.PlotPointsToBoolMap(transformed, flattenedTexHeight, flattenedTexWidth, true);

        }

        // used in MST
        public void RegisterConnected(IGraphNode other) {
            ConnectedRegion.Add(other as RegionInfo);
        }

        public void DoWFC(WFC wfc, int seed) {

            // ID rules:
            // Empty = 0
            // Road = 1
            // FountainRoad = 2
            // PillarRoad = 3
            // *Wall = 4
            FlattenedMapWFC = wfc.Run(flattenedTexHeight, flattenedTexWidth, seed); // here we follow (row, col) convension
            // expand road alongside border
            byte[,] mask = ExpandRoad(0, 1, 4);
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
        }

        byte[,] ExpandRoad(byte emptyId, byte roadId, byte wallId) {
            int rowSize = FlattenedMap.GetLength(0), colSize = FlattenedMap.GetLength(1);
            byte[,] ret = new byte[rowSize, colSize];
            for (int r = 0; r < FlattenedMap.GetLength(0); ++r) {
                for (int c = 0; c < FlattenedMap.GetLength(1); ++c) {
                    if (FlattenedMap[r, c]) { // ground
                        if (r == 0 || r == rowSize - 1 || c == 0 || c == colSize - 1) { // on the edge
                            ret[r, c] = roadId;
                        } else {
                            if (FlattenedMap[r - 1, c] && FlattenedMap[r + 1, c] && FlattenedMap[r, c-1] && FlattenedMap[r, c+1]) {
                                ret[r, c] = emptyId;
                            } else {
                                ret[r, c] = roadId;
                            }
                        }
                    } else { // wall
                        ret[r, c] = wallId;
                    }
                }
            }
            return ret;
        }

        public void MarchingSquareRoadnetwork(int upscaleFactor, float squareSize, float wallHeight, int smoothCount, float smoothRatio, float widthRatio) {
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
            MarchingSquare mc = new MarchingSquare();
            mc.GenerateMesh(upscaled, squareSize, wallHeight, smoothCount, smoothRatio, false);
            roadNetworkCover = mc.CoverMesh;
            roadNetworkWall = mc.WallMesh;
        }
    }

}
