using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MicroUniverse {
    public class StartGameplayController : GameplayControllerBase {

        public KaleidoPainter painter;
        public Texture2D maskTex;
        public Shader blitMaskShader;
        public Shader reverseShader;
        public Shader thresholdShader;

        public RawImage debugImage;

        public List<SpriteRenderer> spriteRenders;

        public override void Begin() {
            
        }

        public override void Finish() {
            
        }
        
        public void OnPaintClick() {
            print("Paint!");
            GenerateKaleidoTex();
            
            // debugImage.texture = GameManager.Instance.KaleidoTex;
            SceneManager.LoadScene("main");
        }

        public void GenerateKaleidoTex() {
            Material blitMat = new Material(blitMaskShader);
            blitMat.SetTexture("_MaskTex", maskTex);
            RenderTexture rt1 = new RenderTexture(maskTex.width, maskTex.height, 0);
            RenderTexture rt2 = new RenderTexture(maskTex.width, maskTex.height, 0);
            Graphics.Blit(painter.GetTexture(), rt1, blitMat);
            Graphics.Blit(rt1, rt2, new Material(reverseShader));
            GameManager.Instance.KaleidoTex = Util.RT2Tex(rt2);
            rt1.Release();
            rt2.Release();
        }
    }

}