using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {
    public class TestGaussianBlur : MonoBehaviour {

        [Header("Input")]
        public Texture2D src;

        [Range(0, 6)]
        public int downSampleNum;

        [Range(0.1f, 20f)]
        public float blurSpreadSize;

        [Range(1, 8)]
        public int blurIterations;

        [Header("Output")]
        public RawImage outputImage;

        public void Blur() {
            Texture2D result = GaussianBlur.Blur(src, downSampleNum, blurSpreadSize, blurIterations);
            outputImage.texture = result;
        }
    }

}