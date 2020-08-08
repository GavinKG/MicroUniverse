using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

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
                colored = GaussianBlur.Blur(colored, 0, 0.25f, 1); // TODO: gaussian blur will whiten color due to lack of gamma correction, but whatever...
                int width = original.width, height = original.height;
                RenderTexture rt = RenderTexture.GetTemporary(width, height, 0);
                RenderTexture prevRT = RenderTexture.active;
                Graphics.Blit(colored, rt);
                Texture2D colored1024 = Util.RT2Tex(rt);
                RenderTexture.active = prevRT;
                RenderTexture.ReleaseTemporary(rt);
                
                // Color it on CPU:
                Color32[] originalColors = original.GetPixels32(); // should be in 1024x1024
                Color32[] coloredColors = colored1024.GetPixels32();

                
                for (int x = 0; x < width; ++x) {
                    for (int y = 0;  y < height; ++y) {
                        int index = x * width + y;
                        if (originalColors[index].a == 0) {
                            originalColors[index] = coloredColors[index];
                        }
                    }
                }

                original.SetPixels32(originalColors);
                original.Apply();
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