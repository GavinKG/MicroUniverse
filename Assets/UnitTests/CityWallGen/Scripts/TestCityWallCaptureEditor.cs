using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace MicroUniverse {
    [CustomEditor(typeof(TestCityWallCapture))]
    public class TestCityWallCaptureEditor : Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();
            TestCityWallCapture testCityWallCapture = target as TestCityWallCapture;

            if (GUILayout.Button("Capture Now")) {
                testCityWallCapture.Capture();
            }

        }
    }
}


