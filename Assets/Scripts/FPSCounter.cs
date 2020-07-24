using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour {

    public Text fpsText;
    public float refreshRate = 1f;

    private float timer;

    private void Update() {
        if (Time.unscaledTime > timer) {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            fpsText.text = "FPS: " + fps;
            timer = Time.unscaledTime + refreshRate;
        }
    }
}