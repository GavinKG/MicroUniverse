using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MainGameplayController : GameplayControllerBase {

        // Inspector:
        public LoadingJob loadingJob;
        public GameObject ballGO;
        [Header("Gameplay")]
        [Range(0.1f, 1f)] public float pillarUnlockToSuccessRate = 0.8f;
        [Header("Debugging")]
        public bool skipIntro = false;
        public bool skipUnlocking = false;
        public bool skipRegionSwitching = false;
        // Inspector END

        public List<RegionInfo> RegionInfos { get; set; }
        public RegionInfo StartRegion { get; set; }

        RegionInfo currRegion = null;
        Vector3 regionEnterPosition;

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

        #region Controller StateMachine

        private enum GameplayState {
            None, Intro, FTUE, Playing, Outro
        }

        GameplayState currState = GameplayState.None;

        bool smTransitioning = false;

        /// <summary>
        /// Core FSM.
        /// </summary>
        void TransitionState(GameplayState newState) {
            print("Core FSM: trying to transition from " + currState.ToString() + " to " + newState.ToString());
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
                    if (newState == GameplayState.Playing) {
                        OnReEnterPlayingState();
                        // currState = newState;
                    }
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
            // player first enters the whole new world...
            currRegion = StartRegion;
            regionEnterPosition = StartRegion.portals[0].PortalSpawnPosition;
            ballGO.transform.position = regionEnterPosition;
        }

        void OnReEnterPlayingState() {
            // player enters another region...
            ballGO.GetComponent<BallController>().KillVelocity();
            ballGO.transform.position = regionEnterPosition; // a new regionEnterPosition is already being updated by GotoRegion()
        }

        #endregion

        #region Region StateMachine

        void TransitionRegionState(RegionInfo.RegionState newState) {
            print("Region FSM: trying to transition from " + currRegion.currState.ToString() + " to " + newState.ToString());
            switch (currRegion.currState) {
                case RegionInfo.RegionState.Locked:
                    if (newState == RegionInfo.RegionState.Unlocking) {
                        OnRegionUnlocking();
                        currRegion.currState = newState;
                    } else if (newState == RegionInfo.RegionState.Unlocked) {
                        // debugging...
                        OnRegionUnlocked();
                        currRegion.currState = newState;
                    }
                    break;
                case RegionInfo.RegionState.Unlocking:
                    break;
                case RegionInfo.RegionState.Unlocked:

                    break;
            }

        }

        void OnRegionUnlocking() {
            print("Region unlocking: " + currRegion.RegionID.ToString());
            // play some timeline here...
        }

        void OnRegionUnlocked() {
            print("Region unlocked: " + currRegion.RegionID.ToString());
            foreach (RegionPortal regionPortal in currRegion.portals) {
                regionPortal.SetPortalActive();
            }
        }

        #endregion


        #region Logic Callback

        public void PillarEnabled(PillarProp pillarProp) {
            if (currRegion.currState != RegionInfo.RegionState.Locked) {
                return;
            }
            ++currRegion.unlockedPillar;
            CheckPillarStatus();
        }

        public void PillarDisabled(PillarProp pillarProp) {
            if (currRegion.currState != RegionInfo.RegionState.Locked) {
                return;
            }
            --currRegion.unlockedPillar;
        }

        public void GotoRegion(int regionId) {
            print("Region switch " + currRegion.RegionID.ToString() + "->" + regionId.ToString());
            RegionInfo lastRegion = currRegion;
            currRegion = RegionInfos[regionId];
            RegionPortal backPortal = currRegion.FindPortalFromHereTo(lastRegion.RegionID);
            regionEnterPosition = backPortal.PortalSpawnPosition;

            TransitionState(GameplayState.Playing);
        }

        #endregion

        void CheckPillarStatus() {
            print("Curr region unlock rate: " + (currRegion.unlockedPillar / (currRegion.PillarCount * pillarUnlockToSuccessRate)).ToString());
            if (currRegion.PillarCount * pillarUnlockToSuccessRate <= currRegion.unlockedPillar) {

                UnlockCurrRegion();
            }
        }

        // make it public to debug.
        public void UnlockCurrRegion() {

            if (skipUnlocking) {
                // debugging...
                TransitionRegionState(RegionInfo.RegionState.Unlocked);
            } else {
                TransitionRegionState(RegionInfo.RegionState.Unlocking);
            }
        }
    }


}