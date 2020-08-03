using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace MicroUniverse {
    

    [RequireComponent(typeof(Collider))]
    public class Sensor : MonoBehaviour {

        List<GameObject> inRangeGOs = new List<GameObject>();

        private void OnTriggerEnter(Collider other) {
            inRangeGOs.Add(other.gameObject);
        }

        private void OnTriggerExit(Collider other) {
            inRangeGOs.Remove(other.gameObject);

        }

        public void ClearInRangeList() {
            inRangeGOs.Clear();
        }

        public GameObject GetNearestGO() {
            if (inRangeGOs.Count == 0) {
                return null;
            }
            float minSqrDis = float.MaxValue;
            int minIndex = 0;
            for (int i = 0; i  < inRangeGOs.Count; ++i) {
                float d = (transform.position - inRangeGOs[i].transform.position).sqrMagnitude;
                if (d < minSqrDis) {
                    minSqrDis = d;
                    minIndex = i;
                }
            }
            return inRangeGOs[minIndex];
        }

        public List<GameObject> GetGOInRange() {
            return new List<GameObject>(inRangeGOs); // shallow copy.
        }
    }

}