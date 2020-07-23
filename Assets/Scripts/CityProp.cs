using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace MicroUniverse {
    public class CityProp : MonoBehaviour {

        public List<MeshFilter> meshesToTransform;

        private void OnDrawGizmosSelected() {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
        }
    }
}
