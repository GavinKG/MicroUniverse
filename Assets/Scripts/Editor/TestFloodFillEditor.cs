using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MicroUniverse {

    [CustomEditor(typeof(TestFloodFill))]
    public class TestFloodFillEditor : Editor {

        string previewIndex = "";

        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            TestFloodFill testFloodFill = target as TestFloodFill;

            if (GUILayout.Button("Fill")) {
                testFloodFill.FloodFill();
            }
            
            previewIndex = GUILayout.TextField(previewIndex);
            
            if (GUILayout.Button("Preview")) {
                int index = int.Parse(previewIndex);
                testFloodFill.Preview(index);
            }

        }

    }

}

