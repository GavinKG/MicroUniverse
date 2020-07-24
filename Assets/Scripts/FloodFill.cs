using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class FloodFill {

        public delegate bool PreviewFloodProcessDelegate(in bool[,] map);

        public PreviewFloodProcessDelegate OnPreviewFloodProcess;

        public class FillResult {

            public List<Vector2Int> FilledPoints { get; private set; } = new List<Vector2Int>();

            public int BorderYMin { get; private set; } = int.MaxValue;
            public int BorderYMax { get; private set; } = int.MinValue;
            public int BorderXMin { get; private set; } = int.MaxValue;
            public int BorderXMax { get; private set; } = int.MinValue;


            public Vector2Int FilledAreaCenterPoint { get; private set; }
            public Vector2Int MapCenterPoint { get { return new Vector2Int(Width / 2, Height / 2); } }

            public bool Finished { get; private set; } = false;


            Vector2Int accumulatePoint = Vector2Int.zero;
            public int Height { get; private set; }
            public int Width { get; private set; }

            public FillResult(in bool[,] map) {
                Width = map.GetLength(0);
                Height = map.GetLength(1);
            }

            public void FinishFill() {
                if (Finished) {
                    throw new System.Exception("Already finished.");
                }
                FilledAreaCenterPoint = new Vector2Int(accumulatePoint.x / FilledPoints.Count, accumulatePoint.y / FilledPoints.Count);
                Finished = true;
            }

            public void ExpandSquareBorder(in Vector2Int p) {
                if (BorderYMin > p.y) {
                    BorderYMin = p.y;
                } else if (BorderYMax < p.y) {
                    BorderYMax = p.y;
                }
                if (BorderXMin > p.x) {
                    BorderXMin = p.x;
                } else if (BorderXMax < p.x) {
                    BorderXMax = p.x;
                }
            }

            public void AddPoint(in Vector2Int p) {
                FilledPoints.Add(p);
                accumulatePoint += p;
                ExpandSquareBorder(p);
            }
        }


        /// <summary>
        /// Flood fill position (x, y) in map.
        /// </summary>
        /// <param name="map">Map to be filled. Changes will write back to this map</param>
        /// <param name="fillValue">value to be filled, like an empty land region (with fillValue == true) surrounded by a border (with fillValue == false).</param>
        public FillResult Fill(ref bool[,] map, int x, int y, bool fillValue) {

            FillResult fillInfo = new FillResult(map);

            int width = map.GetLength(0), height = map.GetLength(1);

            if (map[x, y] == !fillValue) {
                Debug.Log("Start point is a border, quitting...");
                return null;
            }

            Queue<Vector2Int> q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(x, y));

            while (q.Count != 0) {
                Vector2Int p = q.Dequeue();
                if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height || map[p.x, p.y] == !fillValue) {
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
        /// Scans in scanline fashion
        /// </summary>
        public List<FillResult> FindAndFill(ref bool[,] map, bool fillValue) {
            List<FillResult> infos = new List<FillResult>();
            int width = map.GetLength(0), height = map.GetLength(1);
            for (int x = 0; x < width; ++x) {
                for (int y = 0; y < height; ++y) {
                    if (map[x, y] == fillValue) {
                        FillResult info = Fill(ref map, x, y, fillValue);
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


