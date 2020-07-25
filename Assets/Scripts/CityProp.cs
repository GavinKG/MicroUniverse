﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace MicroUniverse {
    public class CityProp : MonoBehaviour {

        [Range(1, 100)] public int propWeight = 1;

        // Listed MeshFilters' meshes will be transformed from flatten space to world space in per-vertex basis.
        public List<MeshFilter> meshesToTransform;
    }
}
