using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    [RequireComponent(typeof(Rigidbody))]
    public class BossBallController : MonoBehaviour {

        [HideInInspector] public List<BuildingProp> buildings;

        public float spawnHeight = 5f;
        public float jumpMinDis = 20f; // > mindis: jump, otherwise: walk!
        public Vector2 jumpLatency = new Vector2(1f, 5f);
        public Vector2 restTime = new Vector2(1f, 5f);
        public float targetArriveDis = 1f;
        public float moveForce = 1f;
        public float jumpForce = 10f;
        public float jumpYMul = 1f;
        public float readyJumpFreezeRatio = 0.95f;

        [Header("Debug")]
        public BuildingProp target = null;
        public State currState;
        public float HP { get; private set; }

        GameObject groundGO;
        CinemachineImpulseSource impulseSource;
        Rigidbody rb;
        Vector3 toTargetDir;

        public enum State {
            Idle, Move, ReadyJump, InAir, Die
        }


        /// <summary>
        /// AI Enabler.
        /// </summary>
        public void InitState() {
            if (buildings == null || buildings.Count == 0) {
                throw new System.Exception("Boss: nothing to do!!");
            }

            groundGO = (GameManager.Instance.CurrController as MainGameplayController).groundGO;
            impulseSource = GetComponent<CinemachineImpulseSource>();

            // random position:
            BuildingProp landingProp = buildings[Random.Range(0, buildings.Count)];
            Vector3 propPos = landingProp.transform.position;
            Vector3 spawnPosition = new Vector3(propPos.x, spawnHeight, propPos.z);
            transform.position = spawnPosition;

            target = buildings[Random.Range(0, buildings.Count)];
            HP = 100;

            TransitionState(State.Move);
        }

        // make it public to debug.
        public void TransitionState(State newState) {

            // any state ->
            if (newState == State.Die) {
                OnDie();
            }

            switch (currState) {
                case State.Idle:
                    if (newState == State.Move) {
                        OnMove();
                        currState = newState;
                    } else if (newState == State.ReadyJump) {
                        OnReadyJump();
                        currState = newState;
                    }
                    break;
                case State.ReadyJump:
                    if (newState  == State.InAir) {
                        OnJump();
                        currState = newState;
                    }
                    break;
                case State.InAir:
                    if (newState == State.Move) {
                        // nothing to do, keeps moving
                        currState = newState;
                    }
                    break;
                case State.Move:
                    if (newState == State.Idle) {
                        OnRest();
                        currState = newState;
                    }
                    break;
            }

        }

        void OnDie() {
            // play some animation...
            print("Aaaaaaaaa, dead.");
            gameObject.SetActive(false);
        }

        void OnMove() {
            print("Out of my way!!");
        }

        void OnReadyJump() {
            print("Ready...");
            StartCoroutine(WaitAndSwitchState(Random.Range(jumpLatency.x, jumpLatency.y), State.InAir));

        }

        void OnJump() {
            Vector3 jumpDir = new Vector3(toTargetDir.x, jumpYMul, toTargetDir.z); // hack
            rb.AddForce(jumpDir * jumpForce);
            print("Juuuuuuuuuummmmmmmmmp!");
        }

        void OnRest() {
            print("Tired, I'm gonna rest...");
            target = buildings[Random.Range(0, buildings.Count)];
            float distance = Vector3.Distance(target.transform.position, transform.position);
            if (distance > jumpMinDis) {
                StartCoroutine(WaitAndSwitchState(Random.Range(restTime.x, restTime.y), State.ReadyJump));
            } else {
                StartCoroutine(WaitAndSwitchState(Random.Range(restTime.x, restTime.y), State.Move));
            }
        }

        void Start() {
            rb = GetComponent<Rigidbody>();
        }

        private void Update() {
            if (currState == State.Move) {
                float disToTarget = Vector3.Distance(transform.position, target.transform.position);
                if (disToTarget < targetArriveDis) {
                    TransitionState(State.Idle);
                }
            }
        }

        private void FixedUpdate() {
            if (currState == State.Move) {
                toTargetDir = target.transform.position - transform.position;
                toTargetDir.y = 0;
                toTargetDir.Normalize();
                rb.AddForce(toTargetDir * moveForce);
            } else if (currState == State.ReadyJump) {
                rb.velocity *= readyJumpFreezeRatio;
            }
        }

        private IEnumerator WaitAndSwitchState(float time, State newState) {
            yield return new WaitForSeconds(time);
            TransitionState(newState);
            yield return null;
        }

        private void OnTriggerEnter(Collider other) {
            PillarProp pillarProp = other.GetComponent<PillarProp>();
            if (pillarProp != null) {
                print("Pillar Boom!");
                pillarProp.Deactivate();
                return;
            }
        }

        private void OnCollisionEnter(Collision collision) {

            GameObject other = collision.transform.gameObject;

            if (currState == State.InAir && other == groundGO) {
                impulseSource.GenerateImpulse();
                TransitionState(State.Move);
                return;
            }

            AutoBallController autoBallController = other.transform.GetComponent<AutoBallController>();
            if (autoBallController != null) {
                print("Fuck off, little scum!");
                autoBallController.Die();
                return;
            }

            BuildingProp buildingProp = other.transform.GetComponent<BuildingProp>();
            if (buildingProp == null) {
                buildingProp = other.transform.GetComponentInParent<BuildingProp>();
            }
            if (buildingProp != null) {
                print("Building Boom!");
                buildingProp.Destroy();
                impulseSource.GenerateImpulse();
                return;
            }
        }

        public void Damage(float value) {
            print(value.ToString() + " HP! It hurts!");
            HP -= value;
            print("Now I only have " + HP.ToString() + " HP...");
            if (HP < 0) {
                TransitionState(State.Die);
            }
        }

    }

}