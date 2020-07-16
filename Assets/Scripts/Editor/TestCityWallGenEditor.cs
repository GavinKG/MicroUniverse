using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MicroUniverse {

    [CustomEditor(typeof(TestCityWallGen))]
    public class TestCityWallGenEditor : Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            TestCityWallGen testCityWallGen = target as TestCityWallGen;

            if (GUILayout.Button("Generate!")) {
                testCityWallGen.Generate();
                // EditorUtility.DisplayDialog("TestCityWallGen", "Generate finished.", "Dismiss");

            }

            if (testCityWallGen.cityTex != null) {
                GUILayout.Label("Texture resolution: " + testCityWallGen.cityTex.width.ToString() + "x" + testCityWallGen.cityTex.height.ToString());
            }

            if (GUILayout.Button("Clear!")) {
                testCityWallGen.coverGO.GetComponent<MeshFilter>().mesh = null;
                testCityWallGen.wallGO.GetComponent<MeshFilter>().mesh = null;
                EditorUtility.SetDirty(testCityWallGen.coverGO);
                EditorUtility.SetDirty(testCityWallGen.wallGO);
            }
        }

    }

}


