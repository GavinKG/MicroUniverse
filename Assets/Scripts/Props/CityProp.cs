using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace MicroUniverse {

    public class CityProp : MonoBehaviour {

        [Range(1, 100)] public int propWeight = 1;

        // Compensate scale when move back from flatten space to world space
        public bool compensateScale = true;

        // Listed MeshFilters' meshes will be transformed from flatten space to world space in per-vertex basis.
        public List<MeshFilter> meshesToTransform;

        public ThemeRendererHolder themeRendererHolder;

        public void SetTheme(Theme theme) {

            if (!theme.InstanceCreated) {
                throw new System.Exception("Theme material not instanced.");
            }

            foreach (MeshRenderer r in themeRendererHolder.buildings) {
                r.material = theme.BuildingMat;
            }
            foreach (MeshRenderer r in themeRendererHolder.bases) {
                r.material = theme.BaseMat;
            }
            foreach (MeshRenderer r in themeRendererHolder.empties) {
                r.material = theme.EmptyMat;
            }
            foreach (MeshRenderer r in themeRendererHolder.plants) {
                r.material = theme.PlantMat;
            }
        }
    }


}
