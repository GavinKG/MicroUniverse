using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {
    public class TestFloodFill : MonoBehaviour {

        public Texture2D fillTex;
        public RawImage previewImage;
        public int debugPass = -1;

        private int currPass = -1;

        private List<FloodFill.FillResult> floodInfos;

        public void FloodFill() {
            currPass = -1;
            FloodFill floodFiller = new FloodFill();
            floodFiller.OnPreviewFloodProcess += OnPreviewFloodProcess;
            bool[,] map = Util.Tex2BoolMap(fillTex, brighterEquals: true);
            floodInfos = floodFiller.FindAndFill(ref map, fillValue: true);
            floodFiller.OnPreviewFloodProcess -= OnPreviewFloodProcess;
        }

        public bool OnPreviewFloodProcess(in bool[,] map) {
            ++currPass;
            if (currPass == debugPass) {
                previewImage.texture = Util.BoolMap2Tex(map, true);
                print("OnPreviewFloodProcess called in pass " + currPass.ToString());
                return false;
            } else {
                return true;
            }
        }

        public void Preview(int index) {
            if (index < 0 || index >= floodInfos.Count) {
                print("index out of range.");
                return;
            }
            RegionInfo regionInfo = new RegionInfo(floodInfos[index]);
            previewImage.texture = Util.BoolMap2Tex(regionInfo.GenerateSubMap(), brighterEquals: true);
        }
    }
}

