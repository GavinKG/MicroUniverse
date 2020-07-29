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

            if (GUILayout.Button("Bake all building prop prefab")) {
                foreach (BuildingProp bp in loadingJob.propCollection.buildings) {
                    EditorUtility.SetDirty(bp);
                    foreach (Transform t in bp.transform) {
                        if (t.name.Contains("Base")) {
                            GameObject baseGO = t.gameObject;

                            bp.baseGO = baseGO;

                            bool meshesToTransformContainsThis = false;
                            foreach (MeshFilter mf in bp.meshesToTransform) {
                                if (mf.gameObject == baseGO) {
                                    meshesToTransformContainsThis = true;
                                    break;
                                }
                            }
                            if (!meshesToTransformContainsThis) {
                                MeshFilter mf = baseGO.GetComponent<MeshFilter>();
                                if (mf == null) {
                                    throw new System.Exception("mesh filter should be in base prefab's root transform.");
                                }
                                bp.meshesToTransform.Add(mf); 
                            }
                            break;
                        }
                    }
                }
                Debug.Log("Finished baking. Save to apply changes to prefab files.");
            }
        }
    }
}