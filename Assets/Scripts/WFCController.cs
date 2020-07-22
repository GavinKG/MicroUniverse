using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class WFCController : MonoBehaviour {

        public string sampleFilePath = "WFCSample.txt";

        [Range(1, 3)]
        public int N = 3;

        [Range(1, 7)]
        public int symmetryVariantCount = 7;

        public List<GameObject> prefabs;

        public int outputWidth = 50;
        public int outputHeight = 50;

        [Range(0, 200)]
        public int randomSeed = 0;

        WFC wfc;

        void WFCWarmup() {
            string sampleString = Util.ReadStringFromResource(sampleFilePath);
            byte[,] sample = Util.StringToByteMapWithSingleDigit(sampleString);
            wfc = new WFC(sample, N, false, false, symmetryVariantCount);
        }

        public void Run() {
            if (wfc == null) {
                WFCWarmup();
            }
            byte[,] output = wfc.Run(outputWidth, outputHeight, randomSeed);
            string outputString = Util.ByteMapWithSingleDigitToString(output);
            print(outputString);
        }
    }

}
