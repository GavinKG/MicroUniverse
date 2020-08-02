using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {
    public class StartGameplayController : GameplayControllerBase {

        public KaleidoPainter painter;
        public Texture2D maskTex;
        public Shader blitMaskShader;
        public Shader reverseShader;

        public RawImage debugImage;

        public List<SpriteRenderer> spriteRenders;

        public override void Begin() {
            
        }

        public override void Finish() {
            
        }

        public void OnColorBallClick() {
            print("!");
            GameObject buttonGO = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            Image buttonImage = buttonGO.GetComponent<Image>();
            Color color = buttonImage.color;
            GameManager.Instance.CityWallColor = color;
            foreach (SpriteRenderer spriteRenderer in spriteRenders) {
                spriteRenderer.color = color;
            }
        }

        public void OnPaintClick() {
            print("Paint!");
            GenerateKaleidoTex();
            // debugImage.texture = GameManager.Instance.KaleidoTex;
            GameManager.Instance.SwitchLevel(GameManager.Level.Main);
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