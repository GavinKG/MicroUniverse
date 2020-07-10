using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ring2Stripe : MonoBehaviour {

    public Texture ringTex;
    public RenderTexture stripeTex;
    public Material blitMat;


    void Update() {
        Graphics.Blit(ringTex, stripeTex, blitMat);
    }
}
