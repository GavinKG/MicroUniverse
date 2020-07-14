using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {

    [RequireComponent(typeof(CaptureOverviewMask))]
    public class TestCityWallCapture : MonoBehaviour {

        public RawImage previewImage;

        public void Capture() {
            CaptureOverviewMask capturer = GetComponent<CaptureOverviewMask>();
            Texture2D captured = capturer.Capture();
            previewImage.texture = captured;
        }

    }
}


