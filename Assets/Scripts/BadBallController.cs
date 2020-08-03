using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MicroUniverse {

    /// <summary>
    /// Logic:
    /// Find nearest activated pillar -> deactivate it -> find next
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BadBallController : MonoBehaviour {

        public enum State {
            Idle, Tracing
        }
        public State currState = State.Tracing;
        public Sensor sensor;
        public float tracingForce = 2;
        public float tracingTiredTime = 3f;
        public float slowdownDistance = 1f;
        public Vector2 idleTime = new Vector2(3f, 7f);
        public LayerMask raycastIgnoreLayer;


        Vector3 tracingPos;
        Rigidbody rb;
        float tracingTime;

        private void Start() {
            rb = GetComponent<Rigidbody>();
            // hack:
            if (currState == State.Idle) {
                OnIdle();
            } else if (currState == State.Tracing) {
                OnFindingTarget();
            }
        }

        void Update() {
            if (currState == State.Tracing) {

                Vector3 toTracing = tracingPos - transform.position;
                float dis = toTracing.magnitude;
                Vector3 toTracingDir = toTracing / dis;
                rb.AddForce(toTracingDir * tracingForce);
                tracingTime += Time.deltaTime;
                if (dis < slowdownDistance || tracingTime > tracingTiredTime) {
                    TransitionState(State.Idle);
                }
            }
        }

        void OnTriggerEnter(Collider other) {
            PillarProp pillarProp = other.gameObject.GetComponent<PillarProp>();
            if (pillarProp != null && pillarProp.Activated) {
                print("Dead pillar!");
                pillarProp.Deactivate();
            }
        }

        private void TransitionState(State newState) {
            switch (newState) {
                case State.Idle:
                    if (currState == State.Tracing) {
                        OnIdle();
                        currState = newState;
                    }
                    break;
                case State.Tracing:
                    if (currState == State.Idle) {
                        OnFindingTarget();
                        currState = newState;
                    }
                    break;
            }
        }

        private void OnFindingTarget() {
            tracingTime = 0f;

            List<GameObject> sensorGOs = sensor.GetGOInRange();
            sensorGOs.Sort((a, b) => {
                float disToA = Vector3.Distance(a.transform.position, transform.position);
                float disToB = Vector3.Distance(b.transform.position, transform.position);
                if (disToA < disToB) {
                    return -1;
                } else if (disToA > disToB) {
                    return 1;
                } else { // float!!
                    return 0;
                }
            });
            List<GameObject> activePillars = sensorGOs.Where(a => a.gameObject.GetComponent<PillarProp>().Activated).ToList();

            List<GameObject> visiblePillars = new List<GameObject>();
            foreach (GameObject go in activePillars) {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, go.transform.position, out hit, raycastIgnoreLayer)) {
                    if (hit.transform.gameObject == go) {
                        visiblePillars.Add(go);
                    }
                }
            }


            GameObject tracingGO = null;

            if (visiblePillars.Count != 0) {
                tracingGO = visiblePillars[Random.Range(0, visiblePillars.Count)];
                print("attacking visible pillar");
            } else if (activePillars.Count != 0) {
                tracingGO = activePillars[0];
                print("attacking nearest active pillar");
            } else {
                Vector2 randomCircle = Random.insideUnitCircle;
                tracingPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                tracingTime = tracingTiredTime / 2;
                print("Sad story: active: " + activePillars.Count.ToString() + ", visible = 0");
            }

        }

        private void OnIdle() {
            float waitTime = Random.Range(idleTime.x, idleTime.y);
            StartCoroutine(WaitAndSwitchState(waitTime, State.Tracing));
        }

        private IEnumerator WaitAndSwitchState(float waitTime, State newState) {
            yield return new WaitForSeconds(waitTime);
            TransitionState(newState);
        }



    }

}