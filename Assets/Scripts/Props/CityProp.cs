﻿using System.Collections;
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

        public void SetThemeMaterial(ThemeMaterialHolder holder) {

            foreach (MeshRenderer r in themeRendererHolder.buildings) {
                r.material = holder.BuildingMat;
            }
            foreach (MeshRenderer r in themeRendererHolder.bases) {
                r.material = holder.BaseMat;
            }
            foreach (MeshRenderer r in themeRendererHolder.empties) {
                r.material = holder.EmptyMat;
            }
            foreach (MeshRenderer r in themeRendererHolder.plants) {
                r.material = holder.PlantMat;
            }
        }
    }


}
