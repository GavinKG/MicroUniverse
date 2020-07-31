using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace MicroUniverse {

    public class CityProp : MonoBehaviour {

        [Range(1, 100)] public int propWeight = 1;

        public bool debug = false;

        // Compensate scale when move back from flatten space to world space
        public bool compensateScale = true;

        // Listed MeshFilters' meshes will be transformed from flatten space to world space in per-vertex basis.
        public List<MeshFilter> meshesToTransform;

        public void OnDrawGizmosSelected() {
            if (debug) {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
            }
        }
    }


}
