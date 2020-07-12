using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Texture2BoolMap
{
    public static bool[,] Convert(Texture2D texture) {
        bool[,] ret = new bool[texture.width, texture.height];
        Color[] pix = texture.GetPixels();
        for (int i = 0; i < pix.Length; ++i) {
            ret[i % texture.width, i / texture.width] = (pix[i] == Color.black ? true : false);
        }
        return ret;
    }
}
