using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace MicroUniverse {

    // Just play some timeline...
    public class OutroGameplayController : GameplayControllerBase {

        public Sprite canvasSprite;

        PlayableDirector director;

        [Header("Animations")]
        public TimelineAsset outroTimeline;

        public override void Begin() {

            // prep ref:
            director = GetComponent<PlayableDirector>();


            if (GameManager.Instance.ColoredTransparentTex != null) {
                // blend kaleido:
                Texture2D original = canvasSprite.texture; // RW enabled tex.
                Texture2D colored = GameManager.Instance.ColoredTransparentTex;

                // Color it on CPU:
                Color32[] colors = original.GetPixels32();
                int width = original.width, height = original.height;
                



            } else {
                throw new System.Exception("?");
            }


            director.Play(outroTimeline);
            
        }

        public override void Finish() {

        }

        public void OnTimelineFinished() {
            SceneManager.LoadScene("Intro");
        }
    }

}