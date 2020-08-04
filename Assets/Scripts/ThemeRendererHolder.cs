using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    /// <summary>
    /// Used for nesting varaibles in CityProp
    /// </summary>
    [System.Serializable]
    public class ThemeRendererHolder {
        public List<MeshRenderer> buildings;
        public List<MeshRenderer> bases;
        public List<MeshRenderer> empties;
        public List<MeshRenderer> plants;
    }

}


