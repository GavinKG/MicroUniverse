using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MicroUniverse {

    [CustomEditor(typeof(MainGameplayController))]
    public class MainGameplayControllerEditor : Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();
            MainGameplayController controller = target as MainGameplayController;

            if (GUILayout.Button("Unlock Current Region")) {
                controller.UnlockCurrRegion();
            }
            
        }

    }

}
