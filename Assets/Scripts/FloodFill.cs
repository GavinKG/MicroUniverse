using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class FloodFill {

        public delegate bool PreviewFloodProcessDelegate(in bool[,] map);

        public PreviewFloodProcessDelegate OnPreviewFloodProcess;

        // Keep this a struct!!
        // x and y should stick to row / col according to bool map, not x, y in texture coords.
        public struct Point {
            public int row;
            public int col;
            public Point(int _r, int _c) {
                row = _r;
                col = _c;
            }
            public void Accumulate(in Point other) {
                row += other.row;
                col += other.col;
            }
            public int X { get { return col; } }
            public int Y { get { return row; } }
        }

        public class FillInfo {

            public List<Point> filledPoints = new List<Point>();
            public int borderColMin = int.MaxValue, borderColMax = int.MinValue, borderRowMin = int.MaxValue, borderRowMax = int.MinValue;
            public Point accPoint = new Point(0, 0);


            private readonly int col, row;

            public FillInfo(in bool[,] map) {
                row = map.GetLength(0);
                col = map.GetLength(1);
            }

            public void ExpandSquareBorder(in Point p) {
                if (borderColMin > p.col) {
                    borderColMin = p.col;
                } else if (borderColMax < p.col) {
                    borderColMax = p.col;
                }
                if (borderRowMin > p.row) {
                    borderRowMin = p.row;
                } else if (borderRowMax < p.row) {
                    borderRowMax = p.row;
                }
            }

            public void ExpandSectorBorder(in Point p) {

            }

            public void AddPoint(in Point p) {
                filledPoints.Add(p);
                accPoint.Accumulate(p);
                ExpandSquareBorder(p);
                ExpandSectorBorder(p);
            }

            public bool[,] GenerateSubMap(bool fillValue = true) {
                int subWidth = borderColMax - borderColMin + 1;
                int subHeight = borderRowMax - borderRowMin + 1;
                bool[,] subMap = new bool[subHeight, subWidth];

                for (int r = 0; r < subHeight; ++r) {
                    for (int c = 0; c < subWidth; ++c) {
                        subMap[r, c] = !fillValue;
                    }
                }

                foreach (Point p in filledPoints) {
                    int subRow = p.row - borderRowMin;
                    int subCol = p.col - borderColMin;
                    subMap[subRow, subCol] = fillValue;
                }

                return subMap;
            }

            public Point CenterPoint {
                get {
                    return new Point(accPoint.row / filledPoints.Count, accPoint.col / filledPoints.Count);
                }
            }

        }


        /// <summary>
        /// Flood fill position (x, y) in map.
        /// </summary>
        /// <param name="map">Map to be filled. Changes will write back to this map</param>
        /// <param name="fillValue">value to be filled, like an empty land region (with fillValue == true) surrounded by a border (with fillValue == false).</param>
        public FillInfo Fill(ref bool[,] map, int row, int col, bool fillValue, bool recordInfo = true) {

            FillInfo fillInfo = new FillInfo(map);

            int rowCount = map.GetLength(0), colCount = map.GetLength(1);

            if (map[row, col] == !fillValue) {
                throw new System.Exception("Start point is a border!");
            }

            Queue<Point> q = new Queue<Point>();
            q.Enqueue(new Point(row, col));

            while (q.Count != 0) {
                Point p = q.Dequeue();
                if (p.row < 0 || p.row >= rowCount || p.col < 0 || p.col >= colCount || map[p.row, p.col] == !fillValue) {
                    continue;
                }

                // actual fill w/ callback:
                map[p.row, p.col] = !fillValue;
                if (recordInfo) {
                    fillInfo.AddPoint(p);
                }

                q.Enqueue(new Point(p.row, p.col + 1));
                q.Enqueue(new Point(p.row, p.col - 1));
                q.Enqueue(new Point(p.row + 1, p.col));
                q.Enqueue(new Point(p.row - 1, p.col));
            }

            return fillInfo;
        }

        /// <summary>
        /// Scans in scanline fashion, that is, top->bottom(left->right)
        /// </summary>
        public List<FillInfo> FindAndFill(ref bool[,] map, bool fillValue) {
            List<FillInfo> infos = new List<FillInfo>();
            int rowCount = map.GetLength(0), colCount = map.GetLength(1);
            for (int r = 0; r < rowCount; ++r) {
                for (int c = 0; c < colCount; ++c) {
                    if (map[r, c] == fillValue) {
                        FillInfo info = Fill(ref map, r, c, fillValue, recordInfo: true);
                        infos.Add(info);
                        if (OnPreviewFloodProcess != null) {
                            bool shouldContinue = OnPreviewFloodProcess.Invoke(in map);
                            if (!shouldContinue) {
                                return new List<FillInfo>();
                            }
                        }

                    }
                }
            }
            Debug.Log("Successfully flood filled with " + infos.Count.ToString() + " region(s).");
            return infos;
        }
    }

}


