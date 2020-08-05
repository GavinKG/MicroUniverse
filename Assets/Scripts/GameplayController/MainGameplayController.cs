using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MicroUniverse {

    public class MainGameplayController : GameplayControllerBase {

        // Inspector:
        [Header("Ref")]
        public LoadingJob loadingJob;
        public GameObject ballGO;
        public GameObject bossBallPrefab;
        public GameObject groundGO;
        public Light mainLight;
        [Header("Gameplay")]
        [Range(0.1f, 1f)] public float pillarUnlockToSuccessRate = 0.8f;
        public Image unlockRateIndicator;
        [Header("Boss Fight")]
        public GameObject bossFightUIRoot;
        public Image bossHP;
        public Image cityHP;
        public float hpInitialWidth = 500;
        [Header("Debugging")]
        public bool skipIntro = false;
        public bool skipUnlocking = false;
        public bool skipRegionSwitching = false;
        [Header("Debug")]
        public bool useAlreadyAssignedSourceTex = false;
        public GameObject debugTextRoot;
        public Text debugCoreSMStateText;
        public Text debugRegionSMStateText;
        public Text debugBallStateText;
        public Text debugBallSpeedText;
        public Text debugBossBallStateText;

        // Inspector END

        public List<RegionInfo> RegionInfos { get; set; }
        public RegionInfo StartRegion { get; set; }

        public int RegionCount { get { return RegionInfos.Count; } }
        public int UnlockedRegionCount { get; private set; } = 0;

        public RegionInfo CurrRegion { get; private set; } = null;
        Vector3 regionEnterPosition;

        // boss fight:
        int regionLeftoverForBossFight;
        bool bossfight = false;
        BossBallController bossBallController;

        public override void Begin() {

            if (!useAlreadyAssignedSourceTex && GameManager.Instance.KaleidoTex != null) {
                loadingJob.source = GameManager.Instance.KaleidoTex;
            }

            if (GameManager.Instance.realtimeShadow) {
                mainLight.shadows = LightShadows.Soft;
            }

            debugTextRoot.SetActive(GameManager.Instance.showDebugInfo);

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
            CurrRegion = StartRegion;
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
            switch (CurrRegion.currState) {
                case RegionInfo.RegionState.Uninitialized:
                    if (newState == RegionInfo.RegionState.Dark) {
                        OnInitRegion();
                        CurrRegion.currState = newState;
                    }
                    break;
                case RegionInfo.RegionState.Dark:
                    if (newState == RegionInfo.RegionState.Unlocking) {
                        OnRegionUnlocking();
                        CurrRegion.currState = newState;
                    } else if (newState == RegionInfo.RegionState.Unlocked) {
                        // debugging...
                        OnRegionUnlocked();
                        CurrRegion.currState = newState;
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
            CurrRegion.SetAutoBallRootActive(true);

            // boss fight logic.
            if (CurrRegion.BuildingCount > GameManager.Instance.bossMinAreaBuildingCount) {
                print("Entering region #" + CurrRegion.RegionID.ToString() + " with building count " + CurrRegion.BuildingCount);
                print("Boss will appear after " + regionLeftoverForBossFight.ToString() + " big regions...");
                if (regionLeftoverForBossFight == 0) {
                    InitBoss();
                } else {
                    --regionLeftoverForBossFight;
                    bossFightUIRoot.SetActive(false);
                }
            }
        }


        void OnRegionUnlocking() {
            print("Region unlocking: #" + CurrRegion.RegionID.ToString());
            // play some timeline here...
        }

        void OnRegionUnlocked() {
            print("Region unlocked: #" + CurrRegion.RegionID.ToString());
            foreach (RegionPortal regionPortal in CurrRegion.portals) {
                regionPortal.SetPortalActive();
            }
            CurrRegion.DestroyAutoBalls();
            ++UnlockedRegionCount;
            if (UnlockedRegionCount == RegionCount || (bossfight && GameManager.Instance.gameOverAfterBossFight)) {
                TransitionState(GameplayState.Outro); // game over.
            }
        }

        #endregion

        void Update() {

            // debug:
            if (GameManager.Instance.showDebugInfo) {
                if (debugCoreSMStateText != null) {
                    debugCoreSMStateText.text = "Core FSM State: " + currState.ToString();
                }
                if (debugRegionSMStateText != null) {
                    debugRegionSMStateText.text = "Region FSM State: " + CurrRegion.currState.ToString();

                }
                if (debugBallStateText != null) {
                    debugBallStateText.text = "Player ball State: " + ballGO.GetComponent<BallController>().currState.ToString();
                }
                if (debugBallSpeedText != null) {
                    debugBallSpeedText.text = "Player ball speed: " + ballGO.GetComponent<BallController>().currSpeed.ToString();
                }
                if (debugBossBallStateText != null) {
                    if (bossBallController != null) {
                        debugBossBallStateText.text = "Boss ball State: " + bossBallController.currState.ToString();
                    } else {
                        debugBossBallStateText.text = "Boss ball State: No boss present.";
                    }
                }
            }


        }


        #region Logic Callback

        public void PillarEnabled(PillarProp pillarProp) {
            if (CurrRegion.currState != RegionInfo.RegionState.Dark) {
                return;
            }
            ++CurrRegion.unlockedPillar;
            CheckPillarStatus();
        }

        public void PillarDisabled(PillarProp pillarProp) {
            if (CurrRegion.currState != RegionInfo.RegionState.Dark) {
                return;
            }
            --CurrRegion.unlockedPillar;
        }

        public void BossHPLoss() {
            UpdateBossHPUI();
        }

        public void BossDestroyBuilding() {
            UpdateCityHPUI();
            // if (buildingsLeft == 0)
        }

        public void BossDie() {
            // TODO: switch state.
            bossBallController = null;
            bossFightUIRoot.SetActive(false);
        }

        public void GotoRegion(int regionId) {
            print("Region switch " + CurrRegion.RegionID.ToString() + "->" + regionId.ToString());
            RegionInfo lastRegion = CurrRegion;
            CurrRegion = RegionInfos[regionId];
            RegionPortal backPortal = CurrRegion.FindPortalFromHereTo(lastRegion.RegionID);
            regionEnterPosition = backPortal.PortalSpawnPosition;

            TransitionState(GameplayState.Playing);
        }

        #endregion

        void UpdateBossHPUI() {
            float width = hpInitialWidth * bossBallController.HP / 100f;
            bossHP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        void UpdateCityHPUI() {
            float width = hpInitialWidth * bossBallController.LeftBuildings / bossBallController.TotalBuildings;
            cityHP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        void InitBoss() {
            print("..And it's now for the boss fight!!!!!");
            bossfight = true;
            bossFightUIRoot.SetActive(true);
            
            GameObject bossGO = Instantiate(bossBallPrefab);
            bossBallController = bossGO.GetComponent<BossBallController>();
            bossBallController.Buildings = new List<BuildingProp>(CurrRegion.buildingProps); // shallow copy.
            bossBallController.OnDestroyBuildingEvent += BossDestroyBuilding;
            bossBallController.OnDieEvent += BossDie;
            bossBallController.OnHPLossEvent += BossHPLoss;

            bossBallController.InitState();

            UpdateBossHPUI();
            UpdateCityHPUI();

        }


        void CheckPillarStatus() {
            if (CurrRegion.PillarCount * pillarUnlockToSuccessRate <= CurrRegion.unlockedPillar) {

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