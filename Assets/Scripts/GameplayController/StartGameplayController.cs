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

        public enum State {
            Start, Draw, Ready, Stutter 
        }

        public State currState = State.Start;

        public List<SpriteRenderer> spriteRenders;

        public override void Begin() {
            
        }

        public override void Finish() {
            
        }

        void TransitionState(State newState) {
            switch (currState) {
                case State.Start:
                    if (newState == State.Draw) {
                        OnBeginDraw();
                        currState = newState;
                    }
                    break;
                case State.Draw:
                    if (newState == State.Ready) {
                        OnEndDraw();
                        currState = newState;
                    }
                    break;
                case State.Ready:

                    break;
            }
        }

        void OnBeginDraw() {

        }

        void OnEndDraw() {

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
            RenderTexture rt = new RenderTexture(maskTex.width, maskTex.height, 0);
            Graphics.Blit(painter.GetTexture(), rt, blitMat);
            GameManager.Instance.KaleidoTex = Util.RT2Tex(rt);
            rt.Release();
        }
    }

}