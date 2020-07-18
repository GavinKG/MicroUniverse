using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class LoadingJob : MonoBehaviour {

        #region Inspector

        [Header("Source")]
        public Texture2D source;

        [Header("Step.2")]
        public int smallRegionThreshold = 20;

        #endregion

        #region Private Member

        // Step.1:
        private Texture2D texAfterStep1;

        // Step.2:
        private bool[,] fillMap;
        private List<RegionInfo> regionInfos;


        #endregion



        public void Load() {

            
            print("Loading Level...");
            print("Starting at " + Time.time.ToString());


            // ----------
            // Step.1
            print("Step.1: binarize, downsample.");

            Texture afterBinarize = Util.Binarize(source, 0.5f);
            texAfterStep1 = Util.Downsample(afterBinarize, 16); // should be from 2048x2048 to 128x128
            // stroke should be in white, background black.


            // ----------
            // Step.2
            print("Step.2: flood fill.");
            
            fillMap = Util.Tex2BoolMap(texAfterStep1, brighterEquals: true);
            int fillMapRowCount = fillMap.GetLength(0), fillMapColCount = fillMap.GetLength(1);
            FloodFill floodFiller = new FloodFill();

            // first fill four corners / center region to white since they are useless in further process.
            floodFiller.Fill(ref fillMap, 0, 0, fillValue: false);
            floodFiller.Fill(ref fillMap, 0, fillMapColCount - 1, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapRowCount - 1, 0, fillValue: false);
            floodFiller.Fill(ref fillMap, fillMapRowCount - 1, fillMapColCount - 1, fillValue: false);

            List<FloodFill.FillResult> fillResults = floodFiller.FindAndFill(ref fillMap, false);
            regionInfos = new List<RegionInfo>();
            foreach (FloodFill.FillResult fillResult in fillResults) {
                if (fillResult.FilledPoints.Count >= smallRegionThreshold) {
                    regionInfos.Add(new RegionInfo(fillResult));
                }
            }
            print("RegionInfo list contains " + regionInfos.Count.ToString() + " regions");

            // Step.3
            print("Step.3: MST.");
            List<IGraphNode> graphNodes = new List<IGraphNode>(regionInfos.Count);
            foreach (RegionInfo regionInfo in regionInfos) {
                graphNodes.Add(regionInfo as IGraphNode);
            }
            RegionInfo rootRegion = MST.Run(graphNodes, registerBidirectional: false) as RegionInfo;

            print("Loading finished at " + Time.time.ToString());

        }

    }

}