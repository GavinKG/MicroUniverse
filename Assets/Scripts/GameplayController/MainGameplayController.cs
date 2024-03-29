﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
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
        public CanvasGroup onscreenControlCanvasGroup;
        public Camera maskCam;
        [Header("Gameplay")]
        [Range(0.1f, 1f)] public float pillarUnlockToSuccessRate = 0.8f;
        public GameObject unlockRateIndicatorUIRoot; //  SetActive should only be called inside region FSM and timeline ending signal.
        public Image unlockRateIndicator;
        public float unlockRateInitialWidth;
        public float unlockRateIndicatorAlpha = 0.5f;
        [Header("Boss Fight")]
        public GameObject bossFightUIRoot;
        public Image bossHP;
        public Image cityHP;
        public float hpInitialWidth = 500;
        [Header("Animations")]
        public TimelineAsset introTimeline;
        public TimelineAsset unlockingTimeline;
        public TimelineAsset outroTimeline;
        public float lightupSpeed = 0.4f;
        [Header("Sequence Debugging")]
        public bool skipIntro = false;
        public bool skipUnlocking = false;
        [Header("Debug")]
        public bool useAlreadyAssignedSourceTex = false;
        public GameObject debugTextRoot;
        public Text debugCoreSMStateText;
        public Text debugRegionSMStateText;
        public Text debugBallStateText;
        public Text debugBallSpeedText;
        public Text debugBossBallStateText;
        public Text debugRegionUnlockRateText;
        public Text debugUnlockedPillarCountText;
        public Text debugBadBallCountText;
        public Text debugCompanionBallCountText;
        public Text debugRegionText;

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

        // anim:
        PlayableDirector director;
        float lightupValue = 0f;

        public override void Begin() {

            if (!useAlreadyAssignedSourceTex && GameManager.Instance.KaleidoTex != null) {
                loadingJob.source = GameManager.Instance.KaleidoTex;
            }

            if (GameManager.Instance.realtimeShadow) {
                mainLight.shadows = LightShadows.Soft;
            }

            debugTextRoot.SetActive(GameManager.Instance.showDebugInfo);

            director = GetComponent<PlayableDirector>();

            loadingJob.Load();

            // loading job will fill these variables:
            if (RegionInfos == null || StartRegion == null) {
                throw new System.Exception("Init incomplete. WTF are you doing LoadingJob?");
            }

            Running = true;

            if (skipIntro) {
                TransitionState(GameplayState.Playing);
            } else {
                TransitionState(GameplayState.Intro);
            }
            
            
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
                        OnFirstEnterWorld();
                        SetInteractive(true);
                        currState = newState;
                    }
                    break;
                case GameplayState.Intro:
                    if (newState == GameplayState.Playing) {
                        SetInteractive(true);
                        currState = newState;
                    }
                    break;
                case GameplayState.Playing:
                    if (newState == GameplayState.Playing) {
                        OnEnterRegion(); // playing -> playing = every time player enters a region.
                        // currState = newState;
                    } else if (newState == GameplayState.Outro) {
                        OnEnterOutroState(); // timeline stuff
                        currState = newState;
                    }
                    break;
                default:
                    break;
            }
            smTransitioning = false;
        }

        void SetInteractive(bool value) {
            // ballGO.GetComponent<BallController>().enabled = value;
            // inGameUIRoot.SetActive(value); // can't do that because it will cause the new input system to spit out tons of errors. Fuck it!

            if (value) {
                ballGO.GetComponent<BallController>().UnFreeze();
            } else {
                ballGO.GetComponent<BallController>().Freeze();
            }

            onscreenControlCanvasGroup.alpha = value ? 1 : 0;
            onscreenControlCanvasGroup.blocksRaycasts = value;
        }

        void OnEnterIntroState() {
            SetInteractive(false);
            print("Play intro timeline...");
            OnFirstEnterWorld();
            director.Play(introTimeline);
            unlockRateIndicatorUIRoot.SetActive(false);
        }

        bool firstEnter = true; // for when developer (me) turning on "skip intro".
        void OnFirstEnterWorld() {
            if (!firstEnter) return;
            // player first enters the whole new world...
            CurrRegion = StartRegion;
            regionEnterPosition = StartRegion.portals[0].PortalSpawnPosition; // note that .y is set to 0. offset a little to prevent fall through floor.
            regionLeftoverForBossFight = GameManager.Instance.bossAfterArea;
            OnEnterRegion();
            firstEnter = false;
        }

        void OnEnterRegion() {
            // player enters a region...
            ballGO.GetComponent<BallController>().KillVelocity();
            Vector3 spawnPoint = new Vector3(regionEnterPosition.x, 1f, regionEnterPosition.z); // give it a little offset in +Y
            ballGO.transform.position = spawnPoint; // a new regionEnterPosition is already being updated by GotoRegion()
            TransitionRegionState(RegionInfo.RegionState.Dark); // try triggering uninit -> dark to init.
        }


        void OnEnterOutroState() {
            SetInteractive(false);
            unlockRateIndicatorUIRoot.SetActive(false);
            bossFightUIRoot.SetActive(false); // double check...
            director.Play(outroTimeline);
            // dont care about region FSM anymore (should be in Unlocking state actually but timeline will not trigger needed signal for it to transfer to unlocked!!)

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
                    if (newState == RegionInfo.RegionState.Unlocked) {
                        OnRegionUnlocked();
                        CurrRegion.currState = newState;
                    }
                    break;
                case RegionInfo.RegionState.Unlocked:
                    // no state to switch to...
                    break;
            }

        }

        void OnInitRegion() {

            unlockRateIndicatorUIRoot.SetActive(true);

            CurrRegion.SetAutoBallRootActive(true);


            UpdateUnlockRateUI();
            unlockRateIndicator.color = new Color(CurrRegion.MainColor.r, CurrRegion.MainColor.g, CurrRegion.MainColor.b, unlockRateIndicatorAlpha);

            // boss fight logic.
            if (CurrRegion.BuildingCount > GameManager.Instance.bossMinAreaBuildingCount) {
                print("Entering region #" + CurrRegion.RegionID.ToString() + " with building count " + CurrRegion.BuildingCount);
                print("Boss will appear after " + regionLeftoverForBossFight.ToString() + " big regions...");
                if (regionLeftoverForBossFight == 0) {
                    InitBoss();
                    unlockRateIndicatorUIRoot.SetActive(false); // manually override.
                } else {
                    bossFightUIRoot.SetActive(false);
                }
                --regionLeftoverForBossFight;
            }
        }


        void OnRegionUnlocking() {
            print("Region unlocking: #" + CurrRegion.RegionID.ToString());
            SetInteractive(false);
            unlockRateIndicatorUIRoot.SetActive(false);
            bool shouldFinishGame = (!bossfight && (UnlockedRegionCount + 1 == Mathf.Min(RegionCount, GameManager.Instance.maxUnlockAreaBeforeEnd))) || // why plus one: region not unlocked right now!
                                    (bossfight && bossBallController == null && GameManager.Instance.gameOverAfterBossFight);
            if (shouldFinishGame) {
                TransitionState(GameplayState.Outro); // game finished.
            } else {
                director.Play(unlockingTimeline);
            }
            
            // play some timeline here...
        }


        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        void OnRegionUnlocked() {
            print("Region unlocked: #" + CurrRegion.RegionID.ToString());
            // CurrRegion.RegionMaskGO.SetActive(true); // set active again (not used anymore due to ddl being so close)
            CurrRegion.SetPortalActive(true);
            CurrRegion.DestroyAutoBalls();
            // CurrRegion.SetAllPillarsActive(false); // if region mask cannot be done on time, switch this to true.
            CurrRegion.SetAllPillarsActiveWithoutNotifyingController(true); // make sure.
            CurrRegion.unlockedPillarCount = CurrRegion.AllPillarCount; // all unlocked!
            ++UnlockedRegionCount;

            bossfight = false; // of course.

            unlockRateIndicatorUIRoot.SetActive(false);
        }

        #endregion

        void Update() {

            // debug:
            if (GameManager.Instance.showDebugInfo && CurrRegion != null) {
                debugCoreSMStateText.text = "Core FSM State: " + currState.ToString();
                debugRegionSMStateText.text = "Region FSM State: " + CurrRegion.currState.ToString();
                debugBallStateText.text = "Player ball State: " + ballGO.GetComponent<BallController>().currState.ToString();
                debugBallSpeedText.text = "Player ball speed: " + ballGO.GetComponent<BallController>().currSpeed.ToString();
                if (bossBallController != null) {
                    debugBossBallStateText.text = "Boss ball State: " + bossBallController.currState.ToString();
                } else {
                    debugBossBallStateText.text = "Boss ball State: No boss present.";
                }
                debugRegionUnlockRateText.text = "Region Unlock Rate: " + (CurrRegion.unlockedPillarCount / (CurrRegion.AllPillarCount * pillarUnlockToSuccessRate)).ToString();
                debugUnlockedPillarCountText.text = "Pillar unlocked: " + CurrRegion.unlockedPillarCount.ToString() + " (" + CurrRegion.NormalPillarCount.ToString() + " Normal + " + CurrRegion.MasterPillarCount.ToString() + " Master total)";
                debugBadBallCountText.text = "Bad pillar (ball) count: " + CurrRegion.badPillars.Count.ToString(); // left? dont care.
                debugCompanionBallCountText.text = "Companion pillar (ball) count: " + CurrRegion.CompanionBallCount.ToString();
                debugRegionText.text = "Curr region: #" + CurrRegion.RegionID.ToString() + ". Overall stats: " + UnlockedRegionCount.ToString() + "/" + RegionCount.ToString() + " unlocked.";
            }


        }
        

        public void PillarEnabled(PillarProp pillarProp) {
            if (CurrRegion.currState != RegionInfo.RegionState.Dark) {
                return;
            }
            ++CurrRegion.unlockedPillarCount;
            UpdateUnlockRateUI();
            CheckPillarStatus();
        }

        public void PillarDisabled(PillarProp pillarProp) {
            if (CurrRegion.currState != RegionInfo.RegionState.Dark) {
                return;
            }
            --CurrRegion.unlockedPillarCount;
            UpdateUnlockRateUI();
        }

        public void OnBossHPLoss() {
            UpdateBossHPUI();
        }

        public void OnBossDestroyBuilding() {
            UpdateCityHPUI();
            // if (buildingsLeft == 0)
        }

        public void OnBossDie() {
            bossBallController = null;
            bossFightUIRoot.SetActive(false);
            UnlockCurrRegion();
            // bossfight = false;
        }

        public void OnUnlockingTimelineTriggers() {
            // timeline will trigger this when playing around the middle.
            TransitionRegionState(RegionInfo.RegionState.Unlocked);
        }

        public void OnUnlockingTimelineEnds() {
            // already transitioned to unlocked.
            SetInteractive(true);
        }

        // Triggered by OutroUnlockingWorldSignal in outro timeline.
        public void LightUpCity() {

            foreach (RegionInfo regionInfo in RegionInfos) {
                regionInfo.SetAutoBallRootActive(false);
            }

            lightupValue = 0f;
            StartCoroutine(LightUpCityUpdate());
        }
        IEnumerator LightUpCityUpdate() {
            // linear light up
            while (lightupValue <= 1f) {
                lightupValue += lightupSpeed * Time.deltaTime;
                float smoothed = Mathf.SmoothStep(0, 1, lightupValue);
                maskCam.backgroundColor = new Color(smoothed, smoothed, smoothed);
                yield return null; // jump a frame
            }

            // turn off all mask particle to save performance
            foreach (RegionInfo regionInfo in RegionInfos) {
                regionInfo.SetAllPillarsActiveWithoutNotifyingController(false);
            }
        }

        public void OnIntroTimelineFinished() {
            TransitionState(GameplayState.Playing);
            if (!bossfight) {
                unlockRateIndicatorUIRoot.SetActive(true); // "In this case, root active will be set by timeline signal callback."
            }
        }

        public void OnOutroTimelineFinished() {
            SceneManager.LoadScene("Outro");
        }

        public void KillBossNow() {
            bossBallController?.Damage(1000f, Vector3.up);
        }

        public void EndGameNow() {
            TransitionState(GameplayState.Outro);
        }

        public void GotoRegion(int regionId) {
            if (currState != GameplayState.Playing) {
                return;
            }
            print("Region switch " + CurrRegion.RegionID.ToString() + "->" + regionId.ToString());
            RegionInfo lastRegion = CurrRegion;
            CurrRegion = RegionInfos[regionId];
            RegionPortal backPortal = CurrRegion.FindPortalFromHereTo(lastRegion.RegionID);
            regionEnterPosition = backPortal.PortalSpawnPosition;

            TransitionState(GameplayState.Playing);
        }





        void UpdateBossHPUI() {
            float width = hpInitialWidth * bossBallController.HP / 100f;
            bossHP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        void UpdateCityHPUI() {
            float width = hpInitialWidth * bossBallController.LeftBuildings / bossBallController.TotalBuildings;
            cityHP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        void UpdateUnlockRateUI() {
            float unlockRate;
            if (CurrRegion.AllPillarCount == 0) {
                unlockRate = 1; // rare but you should not ignore.
            } else {
                unlockRate = CurrRegion.unlockedPillarCount / (CurrRegion.AllPillarCount * pillarUnlockToSuccessRate);
            }
            float width = unlockRateInitialWidth * Mathf.Clamp01(unlockRate);
            unlockRateIndicator.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        void InitBoss() {
            print("..And it's now for the boss fight!!!!!");
            bossfight = true;
            bossFightUIRoot.SetActive(true);
            
            GameObject bossGO = Instantiate(bossBallPrefab);
            bossBallController = bossGO.GetComponent<BossBallController>();
            bossBallController.Buildings = new List<BuildingProp>(CurrRegion.buildingProps); // shallow copy.
            bossBallController.OnDestroyBuildingEvent += OnBossDestroyBuilding;
            bossBallController.OnDieEvent += OnBossDie;
            bossBallController.OnHPLossEvent += OnBossHPLoss;

            bossBallController.InitState();

            UpdateBossHPUI();
            UpdateCityHPUI();

        }


        void CheckPillarStatus() {
            if (bossfight) return;
            if (CurrRegion.AllPillarCount * pillarUnlockToSuccessRate <= CurrRegion.unlockedPillarCount) {
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