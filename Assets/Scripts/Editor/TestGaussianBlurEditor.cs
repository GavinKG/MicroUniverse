using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MicroUniverse {
    [CustomEditor(typeof(TestGaussianBlur))]
    public class TestGaussianBlurEditor : Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            TestGaussianBlur testGaussianBlur = target as TestGaussianBlur;

            if (GUILayout.Button("Blur")) {
                testGaussianBlur.Blur();
            }

        }
    }

}