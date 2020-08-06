using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialKeywordActivator : MonoBehaviour {

    public Material mat;
    public string keyword;
   

    public void On() {
        mat.EnableKeyword(keyword);
    }

    public void Off() {
        mat.DisableKeyword(keyword);
    }
}
