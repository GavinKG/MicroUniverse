using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MainGameplayController : GameplayControllerBase {

        // Inspector:
        [Header("Ref")]
        public LoadingJob loadingJob;
        public GameObject ballGO;
        public GameObject bossBallPrefab;
        public GameObject groundGO;
        [Header("Gameplay")]
        [Range(0.1f, 1f)] public float pillarUnlockToSuccessRate = 0.8f;
        [Header("Debugging")]
        public bool skipIntro = false;
        public bool skipUnlocking = false;
        public bool skipRegionSwitching = false;
        [Header("Debug")]
        public bool useAlreadyAssignedSourceTex = false;
        // Inspector END

        public List<RegionInfo> RegionInfos { get; set; }
        public RegionInfo StartRegion { get; set; }

        public int RegionCount { get { return RegionInfos.Count; } }
        public int UnlockedRegionCount { get; private set; } = 0;

        RegionInfo currRegion = null;
        Vector3 regionEnterPosition;
        int regionLeftoverForBossFight;
        bool bossfight = false;

        public override void Begin() {

            if (!useAlreadyAssignedSourceTex && GameManager.Instance.KaleidoTex != null) {
                loadingJob.source = GameManager.Instance.KaleidoTex;
            }
            loadingJob.Load();

            // loading job will fill these variables:
            if (RegionInfos == null || StartRegion == null) {
                throw new System.Exception("Init incomplete. WTF are you doing LoadingJob?");
            }

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
            None, Intro, Playing, Outro
        }

        GameplayState currState = GameplayState.None;

        bool smTransitioning = false;

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
                        OnFirstEnterPlayingState();
                        currState = newState;
                    }
                    break;
                case GameplayState.Intro:
                    break;
                case GameplayState.Playing:
                    if (newState == GameplayState.Playing) {
                        OnEnterPlayingState();
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

        void OnFirstEnterPlayingState() {
            // player first enters the whole new world...
            currRegion = StartRegion;
            regionEnterPosition = StartRegion.portals[0].PortalSpawnPosition;
            regionLeftoverForBossFight = GameManager.Instance.bossAfterArea;
            OnEnterPlayingState();
        }

        void OnEnterPlayingState() {
            // player enters a region...
            ballGO.GetComponent<BallController>().KillVelocity();
            ballGO.transform.position = regionEnterPosition; // a new regionEnterPosition is already being updated by GotoRegion()
            TransitionRegionState(RegionInfo.RegionState.Dark); // try triggering uninit -> dark to init.
        }

        #endregion

        #region Region StateMachine

        void TransitionRegionState(RegionInfo.RegionState newState) {
            //print("Region FSM: trying to transition from " + currRegion.currState.ToString() + " to " + newState.ToString());
            switch (currRegion.currState) {
                case RegionInfo.RegionState.Uninitialized:
                    if (newState == RegionInfo.RegionState.Dark) {
                        OnInitRegion();
                        currRegion.currState = newState;
                    }
                    break;
                case RegionInfo.RegionState.Dark:
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
                    // no state to switch to...
                    break;
            }

        }

        void OnInitRegion() {
            currRegion.SetBadBallsActive(true);

            // boss fight logic.
            if (currRegion.BuildingCount > GameManager.Instance.bossMinAreaBuildingCount) {
                print("Entering region #" + currRegion.RegionID.ToString() + " with building count " + currRegion.BuildingCount);
                print("Boss will appear after " + regionLeftoverForBossFight.ToString() + " big regions...");
                if (regionLeftoverForBossFight == 0) {
                    InitBoss();
                } else {
                    --regionLeftoverForBossFight;
                }
            }
        }


        void OnRegionUnlocking() {
            print("Region unlocking: #" + currRegion.RegionID.ToString());
            // play some timeline here...
        }

        void OnRegionUnlocked() {
            print("Region unlocked: #" + currRegion.RegionID.ToString());
            foreach (RegionPortal regionPortal in currRegion.portals) {
                regionPortal.SetPortalActive();
            }
            currRegion.SetBadBallsActive(false);
            ++UnlockedRegionCount;

            if (UnlockedRegionCount == RegionCount || bossfight) {
                TransitionState(GameplayState.Outro);
            }
        }

        #endregion


        #region Logic Callback

        public void PillarEnabled(PillarProp pillarProp) {
            if (currRegion.currState != RegionInfo.RegionState.Dark) {
                return;
            }
            ++currRegion.unlockedPillar;
            CheckPillarStatus();
        }

        public void PillarDisabled(PillarProp pillarProp) {
            if (currRegion.currState != RegionInfo.RegionState.Dark) {
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

        void InitBoss() {
            print("..And it's now for the boss fight!!!!!");
            bossfight = true;
            GameObject bossGO = Instantiate(bossBallPrefab);
            BossBallController bossBallController = bossGO.GetComponent<BossBallController>();
            bossBallController.buildings = currRegion.buildingProps;
            bossBallController.InitState();
        }


        void CheckPillarStatus() {
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