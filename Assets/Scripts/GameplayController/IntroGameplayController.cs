using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MicroUniverse {

    public class IntroGameplayController : GameplayControllerBase {

        public GameObject mainUI;
        public GameObject storyUI;
        public GameObject settingsUI;

        public override void Begin() {
            storyUI.SetActive(false);
            mainUI.SetActive(true);
            settingsUI.SetActive(false);
        }

        public override void Finish() {
            
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