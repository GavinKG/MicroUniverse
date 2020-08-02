using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            GameManager.Instance.SwitchLevel(GameManager.Level.Start);
        }



        public void OnFTUEToggleClick(bool value) {
            GameManager.Instance.ftue = value;
        }

        public void OnPreferSensorClick(bool value) {
            GameManager.Instance.preferSensorControl = value;
        }

        public void OnExampleKaleidoPatternClick(bool value) {
            GameManager.Instance.exampleKaleido = value;
        }
    }

}