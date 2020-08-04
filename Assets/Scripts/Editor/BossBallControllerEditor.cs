using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MicroUniverse {

    [CustomEditor(typeof(BossBallController))]
    public class BossBallControllerEditor : Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();
            BossBallController controller = target as BossBallController;

            if (GUILayout.Button("TriggerJump")) {
                controller.TransitionState(BossBallController.State.InAir);
            }

        }

    }

}
