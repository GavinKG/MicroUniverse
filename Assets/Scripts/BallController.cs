using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        public Sensor sensor;
        public GameObject companionPrefab;
        public Transform companionRoot;
        public float companionDelay = 0.1f;
        public float companionGenInterval = 0.1f;



        public Vector3 GravityDirection { get; private set; }
        public Vector3 GravityForce { get; private set; }

        // Input related:
        Vector2 inputMovementAxis;
        Vector3 worldMovementDirection;
        bool hooking = false;
        
        Vector3 lastLegalGravityDirection = Vector3.down;

        Rigidbody rb;
        Collider collider;
        State currState = State.Normal;

        GameObject hookedPillarGO;
        float hookingSpeed;
        Vector3 toHookingPillarDir;


        public void OnPlayerMovementInput(InputAction.CallbackContext context) {
            inputMovementAxis = context.ReadValue<Vector2>();
            float sqrDistance = inputMovementAxis.sqrMagnitude;
            if (sqrDistance > 1) {
                inputMovementAxis.Normalize();
            }
        }

        public void OnHookActionPressed(InputAction.CallbackContext context) {
            if (context.started) {
                print("START");
                TransitionState(State.Hooking);
            } else if (context.canceled) {
                print("CANCEL");
                TransitionState(State.Normal);
            }
            hooking = context.performed;
        }

        public void KillVelocity() {
            rb.velocity = Vector3.zero;
        }



        private void TransitionState(State newState) {
            switch (newState) {
                case State.Freeze:

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
            print("Ball hooked.");
        }

        private void OnFreeze() {

        }

        private bool OnHooking() {
            hookedPillarGO = sensor.getNearestGO();
            if (hookedPillarGO == null) {
                return false;
            }
            hookingSpeed = rb.velocity.magnitude;
            toHookingPillarDir = (hookedPillarGO.transform.position - transform.position).normalized;
            rb.isKinematic = true;
            collider.enabled = false;
            print("Ball hooking -> " + hookedPillarGO.name);
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
            Vector3 outVelocity = outVelocityDir * hookingSpeed;
            rb.velocity = outVelocity;

            // Companion ball:
            MasterPillarProp masterPillar = hookedPillarGO.GetComponent<MasterPillarProp>();
            if (!masterPillar.CompanionSpawned) {
                int companionCount = hookedPillarGO.GetComponent<MasterPillarProp>().companionBallCount;
                if (companionCount != 0) {
                    StartCoroutine(WaitAndGenCompanion(companionCount, hookedPillarGO.transform.position, outVelocity));
                }
                masterPillar.SetCompanionBallSpawned();
            }

            collider.enabled = true;
            GetComponent<MeshRenderer>().enabled = true;
            hookedPillarGO = null;
            hookingSpeed = 0;
            toHookingPillarDir = Vector3.zero;


            
            print("Ball released");
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

        private IEnumerator WaitAndGenCompanion(int count, Vector3 pos, Vector3 velocity) {
            yield return new WaitForSeconds(companionDelay);
            for (int i = 0; i < count; ++i) {
                GameObject companion = Instantiate(companionPrefab, pos, Quaternion.identity, companionRoot);
                Rigidbody rb = companion.GetComponent<Rigidbody>();
                rb.velocity = velocity;
                yield return new WaitForSeconds(companionGenInterval);
            }
            yield return null;
        }



        // Start is called before the first frame update
        void Start() {
            rb = GetComponent<Rigidbody>();
            collider = GetComponent<Collider>();
            if (GameManager.Instance.preferSensorControl && GravitySensor.current != null) {
                InputSystem.EnableDevice(GravitySensor.current);
            }

        }

        void Update() {
            UpdateWorldInputDirection();
            if (currState == State.Hooking) {
                UpdateHooking();
            }
            
        }

        void FixedUpdate() {
            if (currState == State.Normal) {
                UpdateGravity();
            }
            
        }

        void UpdateHooking() {
            float d = Vector3.Distance(hookedPillarGO.transform.position, transform.position);
            transform.position += toHookingPillarDir * hookingSpeed * Time.deltaTime;
            hookingSpeed += hookingSpeedIncrease * Time.deltaTime;
            if (d < hookDistance) {
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
