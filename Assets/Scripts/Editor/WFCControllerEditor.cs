using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MicroUniverse {
    [CustomEditor(typeof(WFCController))]
    public class WFCControllerEditor : Editor {
        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            WFCController wfcController = target as WFCController;

            if (GUILayout.Button("Run!")) {
                wfcController.Run();
            }
        }
    }

}