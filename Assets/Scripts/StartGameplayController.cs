using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {
    public class StartGameplayController : GameplayControllerBase {

        public KaleidoPainter painter;

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
            GameManager.Instance.KaleidoTex = painter.GetTexture();
            GameManager.Instance.SwitchLevel(GameManager.Level.Main);
        }
    }

}