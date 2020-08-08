using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MicroUniverse {

    public class IntroGameplayController : GameplayControllerBase {

        public GameObject mainUI;
        public GameObject storyUI;
        public GameObject settingsUI;

        [Header("Settings Ref")]
        public Toggle preferGravitySensorToggle;
        public Toggle enableKaleidoscopeToggle;
        public Toggle realtimeShadowToggle;
        public Toggle gameOverAfterBossFightToggle;
        public Toggle showDebugInfoToggle;
        public TMP_Text maximumPlayableRegionsText;
        public TMP_Text bossFightAfterLargeRegionText;

        public override void Begin() {
            storyUI.SetActive(false);
            mainUI.SetActive(true);
            settingsUI.SetActive(false);
        }

        public override void Finish() {
            
        }


        // Refresh whole settings based on GameManager's value:
        public void RefreshSettingsUI() {
            preferGravitySensorToggle.isOn = GameManager.Instance.preferSensorControl;
            enableKaleidoscopeToggle.isOn = GameManager.Instance.exampleKaleido;
            realtimeShadowToggle.isOn = GameManager.Instance.realtimeShadow;
            gameOverAfterBossFightToggle.isOn = GameManager.Instance.gameOverAfterBossFight;
            showDebugInfoToggle.isOn = GameManager.Instance.showDebugInfo;

            maximumPlayableRegionsText.text = GameManager.Instance.maxUnlockAreaBeforeEnd.ToString();
            bossFightAfterLargeRegionText.text = GameManager.Instance.bossAfterArea.ToString();

        }



        public void OnMaximumPlayableRegionsMinusClick() {
            if (GameManager.Instance.maxUnlockAreaBeforeEnd > 1) {
                --GameManager.Instance.maxUnlockAreaBeforeEnd;
            }
            RefreshSettingsUI();
        }

        public void OnMaximumPlayableRegionsPlusClick() {
            ++GameManager.Instance.maxUnlockAreaBeforeEnd;
            RefreshSettingsUI();
        }

        public void OnBossFightAfterLargeRegionMinusClick() {
            if (GameManager.Instance.bossAfterArea > 1) {
                --GameManager.Instance.bossAfterArea;
            }
            RefreshSettingsUI();
        }

        public void OnBossFightAfterLargeRegionPlusClick() {
            ++GameManager.Instance.bossAfterArea;
            RefreshSettingsUI();
        }

        public void OnReturnClick() {
            storyUI.SetActive(false);
            mainUI.SetActive(true);
            settingsUI.SetActive(false);
        }

        public void OnSettingsClick() {
            storyUI.SetActive(false);
            mainUI.SetActive(false);
            settingsUI.SetActive(true);
            RefreshSettingsUI();
        }

        public void OnStoryClick() {
            storyUI.SetActive(true);
            mainUI.SetActive(false);
            settingsUI.SetActive(false);
        }
        
        public void OnPlayClick() {
            SceneManager.LoadScene("Start");
        }
        

        public void OnPreferSensorClick(bool value) {
            GameManager.Instance.preferSensorControl = value;
        }

        public void OnExampleKaleidoPatternClick(bool value) {
            GameManager.Instance.exampleKaleido = value;
        }

        public void OnRealtimeShadowClick(bool value) {
            GameManager.Instance.realtimeShadow = value;
        }

        public void OnGameOverAfterBossFightClick(bool value) {
            GameManager.Instance.gameOverAfterBossFight = value;
        }

        public void OnShowDebugInfoClick(bool value) {
            GameManager.Instance.showDebugInfo = value;
        }
    }

}