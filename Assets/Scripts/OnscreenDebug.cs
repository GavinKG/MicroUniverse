using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uses GUILayout.Label() to display console info onscreen.
/// </summary>
public sealed class OnscreenDebug : MonoBehaviour {

    private struct LogContent {
        public string condition;
        public string stackTrace;
        public LogType type;
        public int frame;
        public LogContent(string condition, string stackTrace, LogType type, int frame) {
            this.condition = condition;
            this.stackTrace = stackTrace;
            this.type = type;
            this.frame = frame;
        }
    }

    public Font font;

    [Header("Log")]
    public bool outputLog = true;
    public float logDeleteInterval = 0.5f;
    public float logDeleteBeforeTime = 2f;
    public int maxLogCount = 10;
    public bool errorQuit = true;

    [Header("Normal Color")] public Color normalColor = Color.black;
    [Header("Error Color")] public Color errorColor = Color.red;

    private LinkedList<LogContent> logs = new LinkedList<LogContent>(); // doubly linked

    private List<LogContent> pauseLogs;

    private GUIStyle normalStyle;
    private GUIStyle errorStyle;

    private void Awake() {

        Application.logMessageReceived += Application_logMessageReceived;

        normalStyle = new GUIStyle();
        normalStyle.fontSize = 12;
        normalStyle.normal.textColor = normalColor;
        normalStyle.font = font;

        errorStyle = new GUIStyle(normalStyle);
        errorStyle.normal.textColor = errorColor;

    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type) {

        if (errorQuit && (type == LogType.Error || type == LogType.Exception)) {
            print("An error has occurred. See error log for more details. Quitting...");
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying) {
                UnityEditor.EditorApplication.isPaused = true;
            }
#else
         Application.Quit();
#endif
        }

        if (!outputLog) {
            return;
        }

        StopAllCoroutines();
        logs.AddFirst(new LogContent(condition, stackTrace, type, Time.frameCount));
        if (logs.Count > maxLogCount) {
            logs.RemoveLast();
        }
        StartCoroutine(WaitAndDeleteOldestLog());
    }


    private void OutputContents(IEnumerable enumerable) {
        GUILayout.TextField("--- Log (recent on top) ---", normalStyle);
        foreach (LogContent lc in enumerable) {
            if (lc.type == LogType.Exception || lc.type == LogType.Error) {
                GUILayout.TextField("[" + lc.frame.ToString() + "] " + lc.condition, errorStyle);
            } else {
                GUILayout.TextField("[" + lc.frame.ToString() + "] " + lc.condition, normalStyle);
            }
        }
    }

    private void OnGUI() {

        OutputContents(logs);


    }

    private IEnumerator WaitAndDeleteOldestLog() {
        yield return new WaitForSeconds(logDeleteBeforeTime);
        while (logs.Count != 0) {
            logs.RemoveLast();
            yield return new WaitForSeconds(logDeleteInterval);
        }
    }

}
