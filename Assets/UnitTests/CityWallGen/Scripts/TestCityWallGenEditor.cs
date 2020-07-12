using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestCityWallGen))]
public class TestCityWallGenEditor : Editor {

    public override void OnInspectorGUI() {

        DrawDefaultInspector();

        TestCityWallGen testCityWallGen = target as TestCityWallGen;

        if (GUILayout.Button("Generate!")) {
            testCityWallGen.Generate();
            EditorUtility.DisplayDialog("TestCityWallGen", "Generate finished.", "Dismiss");

        }
    }

}
