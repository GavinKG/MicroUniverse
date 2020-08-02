using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class StartGameplayController : GameplayControllerBase {

        public KaleidoPainter painter;

        public override void Begin() {
            
        }

        public override void Finish() {
            
        }

        public void OnPaintClick() {
            print("Paint!");
            GameManager.Instance.KaleidoTex = painter.GetTexture();
            GameManager.Instance.SwitchLevel(GameManager.Level.Main);
        }
    }

}