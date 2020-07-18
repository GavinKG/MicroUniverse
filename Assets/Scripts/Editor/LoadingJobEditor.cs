using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace MicroUniverse {

    [CustomEditor(typeof(LoadingJob))]
    public class LoadingJobEditor : Editor {
        public override void OnInspectorGUI() {

            DrawDefaultInspector();
            LoadingJob loadingJob = target as LoadingJob;

            if (GUILayout.Button("Generate Level Now!")) {
                loadingJob.Load();
            }

        }
    }

}