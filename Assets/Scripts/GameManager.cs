using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MicroUniverse {
    public class GameManager : MonoBehaviour {

        public GameplayControllerBase CurrController {
            get {
                return currController;
            }
            set {
                if (currController != null) {
                    currController.Finish();
                }
                currController = value;
                currController.Begin();
            }
        }
        GameplayControllerBase currController;

        public static GameManager Instance { get; private set; }


        // Start -> Main.
        public Texture2D KaleidoTex { get; set; }
        public Color CityWallColor { get; set; }

        // ------- GLOBAL SETTINGS -------

        public bool preferSensorControl = false;
        public bool exampleKaleido = false;
        public bool ftue = true;

        // ------- GLOBAL SETTINGS END

        public enum Level { Intro, Start, Main }

        public void SwitchLevel(Level level) {
            switch (level) {
                case Level.Intro:
                    SceneManager.LoadScene("Intro");
                    break;
                case Level.Main:
                    SceneManager.LoadScene("Main");
                    break;
                case Level.Start:
                    SceneManager.LoadScene("Start");
                    break;
            }
        }


        void Awake() {
            if (Instance == null) {
                Instance = this;
            } else if (Instance != this) {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
        }

        void Start() {
            Application.targetFrameRate = 60;
        }

    }

}