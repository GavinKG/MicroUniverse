using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MicroUniverse {
    [CustomEditor(typeof(TestMST))]
    public class TestMSTEditor : Editor {
        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            TestMST testMST = target as TestMST;

            if (GUILayout.Button("Kruskal!")) {
                testMST.DoMST();
            }
        }
    }

}