using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MicroUniverse {

    [RequireComponent(typeof(Rigidbody))]
    public class BossBallController : MonoBehaviour {

         

        public float spawnHeight = 5f;
        public float jumpMinDis = 20f; // > mindis: jump, otherwise: walk!
        public Vector2 jumpLatency = new Vector2(1f, 5f);
        public Vector2 restTime = new Vector2(1f, 5f);
        public Vector2 moveTiredTime = new Vector2(10f, 15f);
        public float targetArriveDis = 1f;
        public float moveForce = 1f;
        public float jumpForce = 10f;
        public float jumpYMul = 1f;
        public float restFreezeRatio = 0.99f;
        public float readyJumpFreezeRatio = 0.95f;

        public ParticleSystem splashParticle;
        public ParticleSystem boomParticle;
        public Light glowLight;

        public float slowMotionTimescale = 0.5f;
        public float slowMotionTime = 1f;
        public float slowMotionSpeedup = 2f;

        [Header("Animations")]
        public TimelineAsset hurtTimeline;
        public TimelineAsset dyingTimeline;
        

        [Header("Debug")]
        public BuildingProp target = null;
        public State currState;
        public float HP { get; private set; }

        public delegate void HPLossDelegate();
        public delegate void DieDelegate();
        public delegate void DestroyBuildingDelegate();

        public event HPLossDelegate OnHPLossEvent;
        public event DieDelegate OnDieEvent;
        public event DestroyBuildingDelegate OnDestroyBuildingEvent;

        GameObject groundGO;
        CinemachineImpulseSource impulseSource;
        Rigidbody rb;
        Vector3 toTargetDir;
        public List<BuildingProp> Buildings { private get; set; }
        List<BuildingProp> ogBuildings;
        PlayableDirector director;

        public int TotalBuildings { get; private set; }
        public int LeftBuildings { get { return Buildings.Count; } }

        public enum State {
            Idle, Move, ReadyJump, InAir, Dying, Die
        }


        /// <summary>
        /// AI Enabler.
        /// </summary>
        public void InitState() {
            if (Buildings == null || Buildings.Count == 0) {
                throw new System.Exception("Boss: nothing to do!!");
            }

            ogBuildings = Buildings;
            Buildings = Buildings.Where(a => !a.tree).Shuffle();
            TotalBuildings = Buildings.Count;

            groundGO = (GameManager.Instance.CurrController as MainGameplayController).groundGO;
            impulseSource = GetComponent<CinemachineImpulseSource>();

            // random position:
            BuildingProp landingProp = Buildings[Random.Range(0, Buildings.Count)];
            Vector3 propPos = landingProp.transform.position;
            Vector3 spawnPosition = new Vector3(propPos.x, spawnHeight, propPos.z);
            transform.position = spawnPosition;

            FindNextTarget();
            HP = 100;

            TransitionState(State.Move);
        }

        // make it public to debug.
        public void TransitionState(State newState) {

            // any state ->
            if (newState == State.Dying) {
                OnDying();
                currState = newState;
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
                case State.Dying:
                    if (newState == State.Die) {
                        OnDie();
                        currState = newState;
                    }
                    break;
            }

        }

        void OnDying() {
            rb.isKinematic = true; // disable physics.
            GetComponent<Collider>().enabled = false;
            StartCoroutine(SlowMotion());
            director.Play(dyingTimeline);
        }

        void OnDie() {
            // play some animation...
            print("Aaaaaaaaa, dead.");
            OnDieEvent?.Invoke();
            // gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void OnMove() {
            print("Out of my way!!");
            StartCoroutine(WaitAndSwitchState(Random.Range(moveTiredTime.x, moveTiredTime.y), State.Idle));
        }

        void OnReadyJump() {
            print("Ready...");
            StartCoroutine(WaitAndSwitchState(Random.Range(jumpLatency.x, jumpLatency.y), State.InAir));

        }

        void OnJump() {
            toTargetDir = target.transform.position - transform.position;
            toTargetDir.y = 0;
            toTargetDir.Normalize();
            Vector3 jumpDir = new Vector3(toTargetDir.x, jumpYMul, toTargetDir.z); // hack
            rb.AddForce(jumpDir * jumpForce);
            print("Juuuuuuuuuummmmmmmmmp!");
        }

        void FindNextTarget() {
            if (Buildings.Count > 0) {
                target = Buildings[Random.Range(0, Buildings.Count)];
            } else {
                target = ogBuildings[Random.Range(0, ogBuildings.Count)];
            }
            

        }

        void OnRest() {
            print("Tired, I'm gonna rest...");

            FindNextTarget();

            float distance = Vector3.Distance(target.transform.position, transform.position);
            if (distance > jumpMinDis) {
                StartCoroutine(WaitAndSwitchState(Random.Range(restTime.x, restTime.y), State.ReadyJump));
            } else {
                StartCoroutine(WaitAndSwitchState(Random.Range(restTime.x, restTime.y), State.Move));
            }
        }

        void Start() {
            rb = GetComponent<Rigidbody>();
            director = GetComponent<PlayableDirector>();
        }

        private void Update() {
            if (currState == State.Move) {
                float disToTarget = Vector3.Distance(transform.position, target.transform.position);
                if (disToTarget < targetArriveDis) {
                    print("ON TARGET!");
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
            } else if (currState == State.Idle) {
                rb.velocity *= restFreezeRatio;
            }
        }

        private IEnumerator WaitAndSwitchState(float time, State newState) {
            yield return new WaitForSeconds(time);
            TransitionState(newState);
        }

        private void OnTriggerEnter(Collider other) {
            PillarProp pillarProp = other.GetComponent<PillarProp>();
            if (pillarProp != null) {
                // print("Pillar Boom!");
                pillarProp.Deactivate(notifyController: true);
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
                autoBallController.Die(rb.velocity.normalized);
                return;
            }

            BuildingProp buildingProp = other.transform.GetComponent<BuildingProp>();
            if (buildingProp == null) {
                buildingProp = other.transform.GetComponentInParent<BuildingProp>();
            }
            if (buildingProp != null) {
                // print("Building Boom!");
                buildingProp.Destroy();
                Buildings.Remove(buildingProp);
                OnDestroyBuildingEvent?.Invoke();
                impulseSource.GenerateImpulse();
                return;
            }
        }

        public void Damage(float value, Vector3 splashDirection) {
            print(value.ToString() + " HP! It hurts!");
            HP -= value;
            director.Play(hurtTimeline);

            Quaternion rot = Quaternion.LookRotation(splashDirection);
            splashParticle.transform.rotation = rot;
            splashParticle.Play();


            print("Now I only have " + HP.ToString() + " HP...");
            OnHPLossEvent?.Invoke();
            if (HP <= 0) {
                HP = 0;
                TransitionState(State.Dying);
            }
        }

        public void OnDyingTimelineTriggerBoom() {
            GetComponent<MeshRenderer>().enabled = false;
            glowLight.enabled = false;
            boomParticle.transform.rotation = Quaternion.LookRotation(Vector3.up);
            boomParticle.Play();
            print("I boom...");
        }

        public void OnDyingTimelineEnds() {
            TransitionState(State.Die);
        }
        
        IEnumerator SlowMotion() {
            Time.timeScale = slowMotionTimescale;
            yield return new WaitForSecondsRealtime(slowMotionTime);
            while (Time.timeScale <= 1f) {
                Time.timeScale += Time.unscaledDeltaTime * slowMotionSpeedup;
                yield return null;
            }
            Time.timeScale = 1f;
        }


        void OnDrawGizmos() {
            if (target != null) {
                Gizmos.DrawWireSphere(target.transform.position, targetArriveDis);
            }
        }

    }

}