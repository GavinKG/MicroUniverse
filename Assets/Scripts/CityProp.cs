using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace MicroUniverse {
    public class CityProp : MonoBehaviour {

        public List<MeshFilter> meshesToTransform;

        bool placed = false;

        /*
        public List<List<Vector3>> GetOriginalData() {

            if (placed) {
                Debug.LogError("Cannot get original data because props are already transformed.");
                return null;
            }

            int dataSize = meshesToTransform.Count;
            if (positionsToTransform.Count != 0) {
                ++dataSize;
            }

            List<List<Vector3>> ret = new List<List<Vector3>>(dataSize);

            if (dataSize == 0) {
                return ret;
            }

            // pack:

            foreach (MeshFilter mf in meshesToTransform) {
                ret.Add(mf.mesh.vertices.ToList());
            }

            // pack all single position in transformBackPositions together:
            if (positionsToTransform.Count != 0) {
                List<Vector3> packed = new List<Vector3>(positionsToTransform.Count);
                foreach (Transform t in positionsToTransform) {
                    packed.Add(t.position);
                }
                ret.Add(packed);
            }

            return ret;
        }

        public void SetTransformedData(List<List<Vector3>> data) {

            if (placed) {
                throw new System.Exception("?");
            }

            // unpack:
            
            for (int i = 0; i < meshesToTransform.Count; ++i) {
                meshesToTransform[i].mesh.SetVertices(data[i]);
            }

            if (positionsToTransform.Count != 0) {
                for (int i = 0; i < positionsToTransform.Count; ++i) {
                    positionsToTransform[i].position = data[data.Count - 1][i];
                }
            }


            placed = true;
        }
        */

        private void OnDrawGizmosSelected() {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
        }
    }
}
