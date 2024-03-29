﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace MicroUniverse {
    public class StartGameplayController : GameplayControllerBase {

        [Header("Ref")]
        public KaleidoPainter painter;
        public GameObject clearButtonGO;
        public GameObject colorButtonGO;

        public Texture2D maskTex;
        public Shader blitMaskShader;
        public Rotator bgRotator;

        [Header("Debug")]
        public GameObject debugRoot;
        public Text debugStateText;
        public Text debugPainterStateText;

        [Header("Animation")]
        // public TimelineAsset kaleidoHandTimeline;
        public TimelineAsset kaleidoNoGoTimeline;
        public TimelineAsset kaleidoFinishedTimeline;
        public TimelineAsset kaleidoClearTimeline;
        public TimelineAsset kaleidoStutterTimeline;

        public enum State {
            Start,
            FinishedDrawing,
            Stutter
        }

        public State currState = State.Start;

        public List<SpriteRenderer> spriteRenders;

        PlayableDirector director;


        public override void Begin() {
            colorButtonGO.SetActive(false);
            clearButtonGO.SetActive(false);
            debugRoot.SetActive(GameManager.Instance.showDebugInfo);
            director = GetComponent<PlayableDirector>();
            painter.ResetCanvas();
            // director.Play(kaleidoHandTimeline);
        }

        public override void Finish() {
            
        }

        private void Update() {
            if (GameManager.Instance.showDebugInfo) {
                debugStateText.text = "State: " + currState.ToString();
                debugPainterStateText.text = "Painter state: " + painter.currState.ToString();
            }
        }

        void TransitionState(State newState) {
            switch (currState) {
                case State.Start:
                    if (newState == State.FinishedDrawing) {
                        OnFinishDrawing();
                        currState = newState;
                    } else if (newState == State.Start) {
                        OnClearDrawing();
                    }
                    break;
                case State.FinishedDrawing:
                    if (newState == State.Stutter) {
                        OnStutter();
                        currState = newState;
                    } else if (newState == State.Start) {
                        OnClearDrawing();
                        currState = newState;
                    }
                    break;
            }
        }

        void OnFinishDrawing() {
            painter.enabled = false;
            colorButtonGO.SetActive(true);
            clearButtonGO.SetActive(true);
            director.Play(kaleidoFinishedTimeline);
        }

        void OnClearDrawing() {
            painter.enabled = true;
            painter.ResetCanvas();
            colorButtonGO.SetActive(false);
            clearButtonGO.SetActive(false);
            director.Play(kaleidoClearTimeline);
        }

        void OnStutter() {
            // timeline stuff
            colorButtonGO.SetActive(false);
            clearButtonGO.SetActive(false);
            painter.enabled = false;
            bgRotator.enabled = false;
            director.Play(kaleidoStutterTimeline);
        }

        // Callbacks:

        public void OnPaintClick() {
            GenerateKaleidoTex();

            // debugImage.texture = GameManager.Instance.KaleidoTex;
            // SceneManager.LoadScene("main");

            TransitionState(State.Stutter);
        }

        public void OnClearClick() {
            TransitionState(State.Start);
        }

        // called by painter
        public void OnFinishedDrawing() {
            TransitionState(State.FinishedDrawing);
        }
        public void OnDrawingFailed() {
            painter.ResetCanvas();
            director.Stop();
            director.Play(kaleidoNoGoTimeline);
        }


        public void OnTimelineFinished() {
            SceneManager.LoadScene("main");
        }






        public void GenerateKaleidoTex() {
            Material blitMat = new Material(blitMaskShader);
            blitMat.SetTexture("_MaskTex", maskTex);
            RenderTexture rt = RenderTexture.GetTemporary(maskTex.width, maskTex.height, 0);
            RenderTexture prevRT = RenderTexture.active;
            Graphics.Blit(painter.GetTexture(), rt, blitMat);
            GameManager.Instance.KaleidoTex = Util.RT2Tex(rt);
            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);
        }

        public IEnumerator WaitAndSwitchState(float time, State newState) {
            yield return new WaitForSeconds(time);
            TransitionState(newState);
        }
    }

}