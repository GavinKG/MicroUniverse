using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cinemachine;

namespace MicroUniverse {
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour {

        public enum State {
            Normal, Hooking, Hooked, Freeze
        }

        [Range(0.01f, 0.5f)] public float maxTiltDistance = 0.3f; // like tangent
        public float hookingSpeedIncrease = 5f;
        public float hookDistance = 0.5f;
        public float hookedSpeedBoost = 1f;
        public float maxSpeed = 15f;
        public Sensor sensor;
        public GameObject companionPrefab;
        public float companionDelay = 0.1f;
        public float companionGenInterval = 0.1f;
        public float damageSpeed = 10f;

        // for boss:
        public float damageMaxSpeed = 15f;
        public float damageMaxSpeedHPLoss = 10f;


        public GameObject indicator;

        public Vector3 GravityDirection { get; private set; }
        public Vector3 GravityForce { get; private set; }

        // Input related:
        Vector2 inputMovementAxis;
        Vector3 worldMovementDirection;
        bool hooking = false;
        
        Vector3 lastLegalGravityDirection = Vector3.down;

        Rigidbody rb;
        Collider collider;
        [HideInInspector] public State currState = State.Normal; // public for debug

        GameObject hookedPillarGO;
        float hookingSpeed;
        Vector3 toHookingPillarDir;
        [HideInInspector] public float currSpeed; // public for debug

        CinemachineImpulseSource shaker;


        public void OnPlayerMovementInput(InputAction.CallbackContext context) {
            inputMovementAxis = context.ReadValue<Vector2>();
            float sqrDistance = inputMovementAxis.sqrMagnitude;
            if (sqrDistance > 1) {
                inputMovementAxis.Normalize();
            }
        }

        public void OnHookActionPressed(InputAction.CallbackContext context) {
            if (context.started) {
                TransitionState(State.Hooking);
            } else if (context.canceled) {
                TransitionState(State.Normal);
            }
            hooking = context.performed;
        }

        public void KillVelocity() {
            if (rb != null) {
                rb.velocity = Vector3.zero;
            }
        }

        public void Freeze() {
            TransitionState(State.Freeze);
        }

        private void TransitionState(State newState) {
            switch (newState) {
                case State.Freeze:
                    KillVelocity();
                    currState = newState;
                    
                    break;
                case State.Hooked:
                    if (currState == State.Hooking) {
                        OnHooked();
                        currState = newState;
                    }
                    break;
                case State.Hooking:
                    if (currState == State.Normal) {
                        bool result = OnHooking();
                        if (result) {
                            currState = newState;
                        }
                    }
                    break;
                case State.Normal:
                    if (currState == State.Hooked) {
                        OnRelease();
                        currState = newState;
                    } else if (currState == State.Hooking) {
                        OnAbandonHook();
                        currState = newState;
                    } else if (currState == State.Freeze) {
                        // TODO
                        currState = newState;
                    }
                    break;
            }
        }

        private void OnHooked() {
            GetComponent<MeshRenderer>().enabled = false;
            float ballY = transform.position.y;
            Vector3 pillarPos = hookedPillarGO.transform.position;
            Vector3 newPos = new Vector3(pillarPos.x, ballY + 0.1f, pillarPos.z);
            transform.position = newPos;
            hookingSpeed += hookedSpeedBoost;
            // print("Ball hooked.");
        }

        private void OnFreeze() {

        }

        private bool OnHooking() {
            hookedPillarGO = sensor.GetNearestGO();
            if (hookedPillarGO == null) {
                return false;
            }
            hookingSpeed = rb.velocity.magnitude;
            rb.isKinematic = true;
            collider.enabled = false;
            // print("Ball hooking -> " + hookedPillarGO.name);
            return true;
        }

        private void OnRelease() {
            rb.isKinematic = false;

            Vector3 outVelocityDir;
            if (inputMovementAxis.x == 0 && inputMovementAxis.y == 0) {
                // along toHookingPillarDir
                outVelocityDir = toHookingPillarDir;
            } else {
                outVelocityDir = worldMovementDirection;
            }
            Vector3 outVelocity = outVelocityDir * Mathf.Min(hookingSpeed, maxSpeed);
            rb.velocity = outVelocity;

            // Companion ball:
            MasterPillarProp masterPillar = hookedPillarGO.GetComponent<MasterPillarProp>();
            if (!masterPillar.CompanionSpawned) {
                bool shouldGenCompanion = hookedPillarGO.GetComponent<MasterPillarProp>().withCompanionBall;
                if (shouldGenCompanion) {
                    GameObject companion = Instantiate(companionPrefab, hookedPillarGO.transform.position, Quaternion.identity, (GameManager.Instance.CurrController as MainGameplayController).CurrRegion.AutoBallRoot);
                    AutoBallController autoBallController = companion.GetComponent<AutoBallController>();
                    autoBallController.currRoadProp = masterPillar;
                }
                masterPillar.CompanionSpawned = true;
            }

            collider.enabled = true;
            GetComponent<MeshRenderer>().enabled = true;
            hookedPillarGO = null;
            hookingSpeed = 0;
            toHookingPillarDir = Vector3.zero;


            
            // print("Ball released");
        }

        private void OnAbandonHook() {
            rb.isKinematic = false;
            collider.enabled = true;

            Vector3 pos = transform.position;
            Vector3 targetPos = hookedPillarGO.transform.position;
            Vector3 diff = targetPos - pos;
            diff.Normalize();
            rb.velocity = diff * hookingSpeed;

            hookedPillarGO = null;
            hookingSpeed = 0;
            toHookingPillarDir = Vector3.zero;

        }

        void UpdateIndicator() {
            if (currState == State.Hooked) {
                if (inputMovementAxis.x == 0 && inputMovementAxis.y == 0) {
                    indicator.SetActive(false);
                } else {
                    indicator.SetActive(true);
                    indicator.transform.position = transform.position;
                    Quaternion rot = Quaternion.LookRotation(worldMovementDirection);
                    indicator.transform.rotation = rot;
                }
            } else {
                indicator.SetActive(false);
            }
        }



        // Start is called before the first frame update
        void Start() {
            rb = GetComponent<Rigidbody>();
            collider = GetComponent<Collider>();
            if (GameManager.Instance.preferSensorControl && GravitySensor.current != null) {
                InputSystem.EnableDevice(GravitySensor.current);
            }
            shaker = GetComponent<CinemachineImpulseSource>();
        }

        void Update() {

            if (currState == State.Freeze) {
                return;
            }

            sensor.transform.position = transform.position; // drive sensor around..

            UpdateWorldInputDirection();
            if (currState == State.Hooking) {
                UpdateHooking();
            }

            UpdateIndicator();
        }

        void FixedUpdate() {

            if (currState == State.Freeze) {
                return;
            }

            if (currState == State.Normal) {
                UpdateGravity();
            }

            currSpeed = rb.velocity.magnitude;

            if (currSpeed > maxSpeed) {
                Vector3 velocityDir = rb.velocity.normalized;
                rb.velocity = velocityDir * maxSpeed;
            }
        }

        void OnTriggerEnter(Collider other) {
            PillarProp pillarProp = other.gameObject.GetComponent<PillarProp>();
            if (pillarProp != null) {
                pillarProp.Activate(notifyController: true);
            }
        }

        private void OnCollisionEnter(Collision collision) {
            GameObject otherGO = collision.transform.gameObject;

            AutoBallController autoBallController = otherGO.GetComponent<AutoBallController>();
            if (autoBallController != null && !autoBallController.setPillarActive) { // bad ball
                if (currSpeed > damageSpeed) {
                    shaker.GenerateImpulse();
                    autoBallController.Die();
                }
                return;
            }

            BossBallController bossBallController = otherGO.GetComponent<BossBallController>();
            if (bossBallController != null) {
                float speedRange = (currSpeed - damageSpeed) / (damageMaxSpeed - damageSpeed);
                if (speedRange > 1) {
                    speedRange = 1;
                }
                if (speedRange > 0) {
                    bossBallController.Damage(damageMaxSpeedHPLoss * speedRange);
                    shaker.GenerateImpulse();
                }
                return;
            }

        }

        void UpdateHooking() {

            toHookingPillarDir = hookedPillarGO.transform.position - transform.position;
            toHookingPillarDir.y = 0;
            float distance = toHookingPillarDir.magnitude;
            if (float.IsNaN(distance)) {
                distance = 0.001f;
            }
            toHookingPillarDir /= distance;

            
            transform.position += toHookingPillarDir * hookingSpeed * Time.deltaTime;
            hookingSpeed += hookingSpeedIncrease * Time.deltaTime;
            if (distance < hookDistance) {
                TransitionState(State.Hooked);
            }
        }

        void UpdateWorldInputDirection() {

            Vector3 worldSpaceUp = Camera.main.cameraToWorldMatrix * Vector3.up;
            worldSpaceUp.y = 0;
            worldSpaceUp.Normalize();
            Vector3 worldSpaceRight = Vector3.Cross(worldSpaceUp, Vector3.up).normalized;

            if (inputMovementAxis.x != 0f || inputMovementAxis.y != 0f) {
                worldMovementDirection = worldSpaceUp * inputMovementAxis.y + worldSpaceRight * (-inputMovementAxis.x);
            } else {
                worldMovementDirection = Vector3.zero;
            }

        }

        void UpdateGravity() {

            if (GameManager.Instance.preferSensorControl && GravitySensor.current != null) {
                // use sensor data
                var gravitySensorData = GravitySensor.current.gravity;
                float x = gravitySensorData.y.ReadValue();
                float y = Mathf.Abs(gravitySensorData.z.ReadValue());
                float z = -gravitySensorData.x.ReadValue();

                x /= y;
                z /= y;
                y = -1;
                if (x > maxTiltDistance) {
                    x = maxTiltDistance;
                } else if (x < -maxTiltDistance) {
                    x = -maxTiltDistance;
                }
                if (z > maxTiltDistance) {
                    z = maxTiltDistance;
                } else if (z < -maxTiltDistance) {
                    z = -maxTiltDistance;
                }
                GravityDirection = new Vector3(x, y, z).normalized;
            } else {
                GravityDirection = new Vector3(worldMovementDirection.x * maxTiltDistance, -1, worldMovementDirection.z * maxTiltDistance).normalized;
            }
            GravityForce = GravityDirection * 9.8f;
            rb.AddForce(GravityForce);
        }
    }
}
