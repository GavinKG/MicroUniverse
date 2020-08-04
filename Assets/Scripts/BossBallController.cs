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
        public GameObject groundGO;


        public BuildingProp target = null;

        Rigidbody rb;
        float hp = 100;

        public enum State {
            Idle, Move, InAir
        }

        State currState;
        Vector3 toTargetDir;

        /// <summary>
        /// AI Enabler.
        /// </summary>
        public void InitState() {
            if (buildings == null || buildings.Count == 0) {
                throw new System.Exception("Boss: nothing to do!!");
            }

            // random position:
            BuildingProp landingProp = buildings[Random.Range(0, buildings.Count)];
            Vector3 propPos = landingProp.transform.position;
            Vector3 spawnPosition = new Vector3(propPos.x, spawnHeight, propPos.z);
            transform.position = spawnPosition;

            TransitionState(State.Move);
        }

        void TransitionState(State newState) {
            switch (currState) {
                case State.Idle:
                    if (newState == State.Move) {
                        OnMove();
                        currState = newState;
                    }
                    break;
                case State.InAir:
                    if (newState == State.Move) {
                        // TODO

                        currState = newState;
                    }
                    break;
                case State.Move:
                    if (newState == State.InAir) {
                        OnJump();
                        currState = newState;
                    } else if (newState == State.Idle) {
                        OnRest();
                        currState = newState;
                    }
                    break;
            }

        }

        void OnMove() {
            target = buildings[Random.Range(0, buildings.Count)];
            float distance = Vector3.Distance(target.transform.position, transform.position);
            if (distance > jumpMinDis) {
                WaitAndSwitchState(Random.Range(jumpLatency.x, jumpLatency.y), State.InAir);
            }
        }

        void OnJump() {
            Vector3 jumpDir = new Vector3(toTargetDir.x, jumpYMul, toTargetDir.z); // hack
            rb.AddForce(jumpDir * jumpForce);
        }

        void OnRest() {
            target = null;
            WaitAndSwitchState(Random.Range(restTime.x, restTime.y), State.Move);
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
            }
        }

        private IEnumerator WaitAndSwitchState(float time, State newState) {
            yield return new WaitForSeconds(time);
            TransitionState(newState);
            yield return null;
        }

        private void OnCollisionEnter(Collision collision) {

            GameObject other = collision.transform.gameObject;

            BuildingProp buildingProp = other.GetComponent<BuildingProp>();
            if (buildingProp != null) {
                buildingProp.Destroy();
                // shake
                return;
            }

            PillarProp pillarProp = other.GetComponent<PillarProp>();
            if (pillarProp != null) {
                pillarProp.Deactivate();
                return;
            }

            if (currState == State.InAir && other == groundGO) {
                TransitionState(State.Move);
            }

        }

    }

}