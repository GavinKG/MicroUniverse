using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MicroUniverse {

    [CustomEditor(typeof(TestFloodFill))]
    public class TestFloodFillEditor : Editor {

        string previewIndex = "1";

        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            TestFloodFill testFloodFill = target as TestFloodFill;

            if (GUILayout.Button("Fill")) {
                testFloodFill.FloodFill();
            }
            
            previewIndex = GUILayout.TextField(previewIndex);

            if (GUILayout.Button("Generate RegionInfo")) {
                int index = int.Parse(previewIndex);
                testFloodFill.Generate(index);
            }

            if (GUILayout.Button("Preview Map")) {
                testFloodFill.PreviewMap();
            }

            if (GUILayout.Button("Preview SubMap")) {
                testFloodFill.PreviewSubMap();
            }

            if (GUILayout.Button("Preview FlattenedMap")) {
                testFloodFill.PreviewFlattenedMap();
            }

            /*
            if (GUILayout.Button("Preview debugTex1")) {
                testFloodFill.PreviewDebug1();
            }
            */

        }

    }

}

