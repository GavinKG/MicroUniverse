using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MainGameplayController : GameplayControllerBase {

        private enum GameplayState {
            None, Intro, FTUE, Playing, Outro
        }

        // Inspector:
        public LoadingJob loadingJob;
        public GameObject ballGO;
        [Header("Debugging")]
        public bool skipIntro = false;
    

        public List<RegionInfo> RegionInfos { get; set; }
        public RegionInfo StartRegion { get; set; }

        GameplayState currState = GameplayState.None;

        bool smTransitioning = false;

        public override void Begin() {

            loadingJob.Load();

            // loading job will fill these variables:
            if (RegionInfos == null || StartRegion == null) {
                throw new System.Exception("Init incomplete. WTF are you doing LoadingJob?");
            }

            BallController bc = ballGO.GetComponent<BallController>();
            bc.preferGravitySensor = GameManager.Instance.preferSensorControl;

            if (skipIntro) {
                TransitionState(GameplayState.Playing);
            } else {
                TransitionState(GameplayState.Intro);
            }
            

            Running = true;
        }

        public override void Finish() {
            
        }

        /// <summary>
        /// Core FSM.
        /// </summary>
        void TransitionState(GameplayState newState) {
            if (smTransitioning) {
                throw new System.Exception("transition recursion detected.");
            }
            smTransitioning = true;
            switch (currState) {
                case GameplayState.None:
                    if (newState == GameplayState.Intro) {
                        OnEnterIntroState();
                        currState = newState;
                    } else if (newState == GameplayState.Playing) {
                        // skip to play
                        OnEnterPlayingState();
                        currState = newState;
                    }
                    break;
                case GameplayState.Intro:
                    break;
                case GameplayState.FTUE:
                    break;
                case GameplayState.Playing:
                    break;
                case GameplayState.Outro:
                    break;
            }
            smTransitioning = false;
        }

        void OnEnterIntroState() {
        }

        void OnEnterFTUEState() {
        }

        void OnEnterPlayingState() {
            ballGO.transform.position = StartRegion.portals[0].transform.position;
        }

    }


}