using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    /// <summary>
    /// All Vector2 (point) used should be represented as (row, col), not (x, y)
    /// </summary>
    public class FloodFill {

        public delegate bool PreviewFloodProcessDelegate(in bool[,] map);

        public PreviewFloodProcessDelegate OnPreviewFloodProcess;

        public class FillResult {

            public List<Vector2Int> FilledPoints { get; private set; } = new List<Vector2Int>();

            public int BorderColMin { get; private set; } = int.MaxValue;
            public int BorderColMax { get; private set; } = int.MinValue;
            public int BorderRowMin { get; private set; } = int.MaxValue;
            public int BorderRowMax { get; private set; } = int.MinValue;


            public Vector2Int FilledAreaCenterPoint { get; private set; }
            public Vector2Int MapCenterPoint { get { return new Vector2Int(ColSize / 2, RowSize / 2); } }

            public bool Finished { get; private set; } = false;


            Vector2Int accPoint = Vector2Int.zero;
            public int ColSize { get; private set; }
            public int RowSize { get; private set; }

            public FillResult(in bool[,] map) {
                RowSize = map.GetLength(0);
                ColSize = map.GetLength(1);
            }

            public void FinishFill() {
                if (Finished) {
                    throw new System.Exception("Already finished.");
                }
                FilledAreaCenterPoint = new Vector2Int(accPoint.x / FilledPoints.Count, accPoint.y / FilledPoints.Count);
                Finished = true;
            }

            public void ExpandSquareBorder(in Vector2Int p) {
                if (BorderColMin > p.y) {
                    BorderColMin = p.y;
                } else if (BorderColMax < p.y) {
                    BorderColMax = p.y;
                }
                if (BorderRowMin > p.x) {
                    BorderRowMin = p.x;
                } else if (BorderRowMax < p.x) {
                    BorderRowMax = p.x;
                }
            }

            public void AddPoint(in Vector2Int p) {
                FilledPoints.Add(p);
                accPoint += p;
                ExpandSquareBorder(p);
            }
        }


        /// <summary>
        /// Flood fill position (x, y) in map.
        /// </summary>
        /// <param name="map">Map to be filled. Changes will write back to this map</param>
        /// <param name="fillValue">value to be filled, like an empty land region (with fillValue == true) surrounded by a border (with fillValue == false).</param>
        public FillResult Fill(ref bool[,] map, int row, int col, bool fillValue) {

            FillResult fillInfo = new FillResult(map);

            int rowCount = map.GetLength(0), colCount = map.GetLength(1);

            if (map[row, col] == !fillValue) {
                Debug.Log("Start point is a border, quitting...");
                return null;
            }

            Queue<Vector2Int> q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(row, col));

            while (q.Count != 0) {
                Vector2Int p = q.Dequeue();
                if (p.x < 0 || p.x >= rowCount || p.y < 0 || p.y >= colCount || map[p.x, p.y] == !fillValue) {
                    continue;
                }

                // actual fill w/ callback:
                map[p.x, p.y] = !fillValue;
                fillInfo.AddPoint(p);

                q.Enqueue(new Vector2Int(p.x, p.y + 1));
                q.Enqueue(new Vector2Int(p.x, p.y - 1));
                q.Enqueue(new Vector2Int(p.x + 1, p.y));
                q.Enqueue(new Vector2Int(p.x - 1, p.y));
            }

            fillInfo.FinishFill();
            return fillInfo;
        }

        /// <summary>
        /// Scans in scanline fashion, that is, top->bottom(left->right)
        /// </summary>
        public List<FillResult> FindAndFill(ref bool[,] map, bool fillValue) {
            List<FillResult> infos = new List<FillResult>();
            int rowCount = map.GetLength(0), colCount = map.GetLength(1);
            for (int r = 0; r < rowCount; ++r) {
                for (int c = 0; c < colCount; ++c) {
                    if (map[r, c] == fillValue) {
                        FillResult info = Fill(ref map, r, c, fillValue);
                        infos.Add(info);
                        if (OnPreviewFloodProcess != null) {
                            bool shouldContinue = OnPreviewFloodProcess.Invoke(in map);
                            if (!shouldContinue) {
                                return new List<FillResult>();
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


