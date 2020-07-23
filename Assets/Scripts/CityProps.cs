using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace MicroUniverse {
    public class CityProps : MonoBehaviour {

        public List<MeshFilter> meshesToTransform;
        public List<Transform> positionsToTransform;


        bool placed = false;

        List<List<Vector3>> GetOriginalData() {

            if (placed) {
                Debug.LogError("Cannot get original data because props are already transformed.");
                return null;
            }

            int dataSize = meshesToTransform.Count;
            if (positionsToTransform.Count != 0) {
                ++dataSize;
            }

            List<List<Vector3>> ret = new List<List<Vector3>>(dataSize);
            foreach (MeshFilter mf in meshesToTransform) {
                ret.Add(mf.mesh.vertices.ToList());
            }

            // pack all single position in transformBackPositions together:
            List<Vector3> packed = new List<Vector3>(positionsToTransform.Count);
            foreach (Transform t in positionsToTransform) {
                packed.Add(t.position);
            }

            ret.Add(packed);
            return ret;
        }

        void SetTransformedData(List<List<Vector3>> data) {

            placed = true;
        }
    }
}
