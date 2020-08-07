using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
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

            // blend kaleido:
            Texture original = canvasSprite.texture; // RW enabled tex.

            director.Play(outroTimeline);
            
        }

        public override void Finish() {

        }

        public void OnTimelineFinished() {
            
        }
    }

}