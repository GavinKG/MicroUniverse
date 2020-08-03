using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace MicroUniverse {
    

    [RequireComponent(typeof(Collider))]
    public class Sensor : MonoBehaviour {

        [HideInInspector]
        public List<GameObject> InRangeGOs { get; private set; } = new List<GameObject>();

        private void OnTriggerEnter(Collider other) {
            InRangeGOs.Add(other.gameObject);
            print(InRangeGOs.Count);
        }

        private void OnTriggerExit(Collider other) {
            InRangeGOs.Remove(other.gameObject);
            print(InRangeGOs.Count);

        }

        public void ClearInRangeList() {
            InRangeGOs.Clear();
        }

        public GameObject getNearestGO() {
            if (InRangeGOs.Count == 0) {
                return null;
            }
            float minSqrDis = float.MaxValue;
            int minIndex = 0;
            for (int i = 0; i  < InRangeGOs.Count; ++i) {
                float d = (transform.position - InRangeGOs[i].transform.position).sqrMagnitude;
                if (d < minSqrDis) {
                    minSqrDis = d;
                    minIndex = i;
                }
            }
            return InRangeGOs[minIndex];
        }
    }

}