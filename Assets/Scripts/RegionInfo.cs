using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class RegionInfo {

        // Ring border
        // Angle value should be in [0, 2*pi)
        public float BorderSectorLeftAngle { get; private set; } = float.MaxValue;
        public float BorderSectorRightAngle { get; private set; } = float.MinValue;
        public float BorderSectorNearRadius { get; private set; } = float.MaxValue;
        public float BorderSectorFarRadius { get; private set; } = float.MinValue;

        FloodFill.FillResult fillResult;

        public RegionInfo(FloodFill.FillResult _fillResult) {
            if (!_fillResult.Finished) {
                throw new System.Exception("Fill result not finished");
            }
            fillResult = _fillResult;
        }

        public bool[,] GenerateSubMap(bool fillValue = true) {
            int subWidth = fillResult.BorderColMax - fillResult.BorderColMin + 1;
            int subHeight = fillResult.BorderRowMax - fillResult.BorderRowMin + 1;
            bool[,] subMap = new bool[subHeight, subWidth];

            for (int r = 0; r < subHeight; ++r) {
                for (int c = 0; c < subWidth; ++c) {
                    subMap[r, c] = !fillValue;
                }
            }

            foreach (Vector2Int p in fillResult.FilledPoints) {
                int subRow = p.x - fillResult.BorderRowMin;
                int subCol = p.y - fillResult.BorderColMin;
                subMap[subRow, subCol] = fillValue;
            }

            return subMap;
        }

        void CalcSectorBorder(FloodFill.FillResult fillResult) {

            // see sketch for details.

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


            for (int i = 0; i < transformed.Count; ++i) {

                transformed[i] = new Vector2(fillResult.FilledPoints[i].x, fillResult.FilledPoints[i].y); // init list.
                transformed[i] -= mapCenter; // let center point be zero!

                // step.1: update sector near/far radius.
                float radius = transformed[i].sqrMagnitude; // faster, but remember sqrt(radius) after loop!!
                if (radius > BorderSectorFarRadius) {
                    BorderSectorFarRadius = radius;
                } else if (radius < BorderSectorNearRadius) {
                    BorderSectorNearRadius = radius;
                }

                // step.2: 2D rotation. TODO: consider using matrix.
                // move to center:
                transformed[i] += filledCenterToMapCenter;
                // 2D rotation to move the pattern to the right:
                transformed[i].Rotate(angleFromFilledCenterToRight);
                // move to the right:
                transformed[i] += right * length;

                // step.3: Calc left/right angle boundary.
                float angle = Vector2.Angle(right, transformed[i]);
                if (angle > BorderSectorRightAngle) {
                    BorderSectorRightAngle = angle;
                } else if (angle < BorderSectorLeftAngle) {
                    BorderSectorLeftAngle = angle;
                }

            }

            BorderSectorNearRadius = Mathf.Sqrt(BorderSectorNearRadius);
            BorderSectorFarRadius = Mathf.Sqrt(BorderSectorFarRadius);
        }

    }

}
