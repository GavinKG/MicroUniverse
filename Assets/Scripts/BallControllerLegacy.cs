using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallControllerLegacy : MonoBehaviour {

    public enum BallState {
        Idle, Hooked, Sprint
    }


    // --------------------------

    [Header("References")]
    public Transform stubRoot;
    public CinemachineVirtualCamera cineCam;

    [Header("Movement Force")]
    public float moveForce = 3f;
    public float hookedMoveForceMul = 3f;
    public float sprintSpeedupMul = 1.5f; // sprint force = hooked mul * sprint mul;

    [Header("Sprint Limiter")]
    public float limiterStartTime = 0.5f;
    public float limiterSpeed = 0.1f;

    // --------------------------
    // Region private vars:

    // ref:
    private Rigidbody rb;
    private List<GameObject> stubs = new List<GameObject>();

    // ball state:
    private BallState currState = BallState.Idle;

    // drive force:
    private Vector3 worldDriveForce;

    // hook:
    private float ropeLength;
    private GameObject hookedStub = null;
    private float hookTime = 0f;
    private float sprintMinSpeed = 0.5f;

    // sprint:
    private float sprintTime = 0f;
    private Vector3 sprintDirection;

    // input:
    private Vector2 inputMovementAxis;
    private Vector2 inputAimAxis;
    private bool hookActionDown = false;

    // world space input
    private Vector3 worldMovementDirection;
    private Vector3 worldAimDirection;

    // --------------------------
    // Region Input callback

    public void OnPlayerMovementInput(InputAction.CallbackContext context) {
        inputMovementAxis = context.ReadValue<Vector2>();
    }

    public void OnHookActionPress(InputAction.CallbackContext context) {
        // handle both press down and release
        if (context.started) {
            hookActionDown = true;
            TransitionBallState(BallState.Hooked);
        } else if (context.canceled) {
            hookActionDown = false;
            if (inputAimAxis.x == 0f && inputAimAxis.y == 0f) {
                // 0 should always be the case, no need to use float.epsilon
                TransitionBallState(BallState.Idle);
            } else {
                // TODO: judge speed limit.
                TransitionBallState(BallState.Sprint);
            }

        }
    }

    public void OnPlayerAimInput(InputAction.CallbackContext context) {
        inputAimAxis = context.ReadValue<Vector2>();
    }

    // --------------------------
    // Region Utility

    GameObject GetNearestStub() {
        float minDis = float.MaxValue;
        GameObject ret = null;
        Vector3 selfPos = transform.position;
        foreach (GameObject stub in stubs) { // kinda heavy!
            float curDis = Vector3.SqrMagnitude(stub.transform.position - selfPos);
            if (curDis < minDis) {
                ret = stub;
                minDis = curDis;
            }
        }
        return ret;
    }

    bool Hook() {
        hookedStub = GetNearestStub();
        if (hookedStub == null) {
            // failed to get a stub: posibilly no available stub around...
            return false;
        }
        ropeLength = Vector3.Distance(hookedStub.transform.position, transform.position);
        hookTime = 0f;
        return true;
    }

    void Unhook() {
        hookedStub = null;
    }

    void Sprint() {
        sprintTime = 0f;
        sprintDirection = worldAimDirection;
    }

    /// <summary>
    /// Ball State Machine
    /// </summary>
    void TransitionBallState(BallState newState) {
        switch (currState) {
            case BallState.Idle:
                switch (newState) {
                    case BallState.Hooked:
                        // state Idle -> Hooked
                        Hook();
                        currState = newState;
                        break;
                    default:
                        break;
                }
                break;
            case BallState.Hooked:
                switch (newState) {
                    case BallState.Idle:
                        // state: Hooked -> Idle
                        Unhook();
                        currState = newState;
                        break;
                    case BallState.Sprint:
                        // state: Hooked -> Sprint
                        Sprint();
                        currState = newState;
                        break;
                    default:
                        break;
                }
                break;
            case BallState.Sprint:
                switch (newState) {
                    case BallState.Idle:
                        // state: Sprint -> Idle
                        currState = newState;
                        break;
                    default:
                        break;
                }
                break;
        }

    }


    void UpdateSprint() {
        if (currState != BallState.Sprint) {
            return;
        }

        sprintTime += Time.deltaTime;

        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;
        
        if (sprintTime > limiterStartTime && speed < limiterSpeed) {
            // may be stuck to obstacle, free the ball to its idle state.
            TransitionBallState(BallState.Idle);
            return;
        }

        Vector3 ballDirection = velocity.normalized;
        speed += sprintSpeedupMul * Time.deltaTime;

        float cosineAngle = Vector3.Dot(ballDirection, worldAimDirection);
        if (cosineAngle > 0.95f) {
            rb.velocity = sprintDirection * speed;
            TransitionBallState(BallState.Idle);
        } else {
            rb.velocity = ballDirection * speed;
        }

    }


    void UpdateWorldDirection() {

        Vector3 worldSpaceUp = Camera.main.cameraToWorldMatrix * Vector3.up;
        worldSpaceUp.y = 0;
        worldSpaceUp.Normalize();
        Vector3 worldSpaceRight = Vector3.Cross(worldSpaceUp, Vector3.up).normalized;

        if (inputMovementAxis.x != 0f || inputMovementAxis.y != 0f) {
            worldMovementDirection = worldSpaceUp * inputMovementAxis.y + worldSpaceRight * (-inputMovementAxis.x);
        } else {
            worldMovementDirection = Vector3.zero;
        }

        if (inputAimAxis.x != 0f || inputAimAxis.y != 0f) {
            worldAimDirection = worldSpaceUp * inputAimAxis.y + worldSpaceRight * (-inputAimAxis.x);
        } else {
            worldAimDirection = Vector3.zero;
        }


    }


    /// <summary>
    /// Process player's drive force and apply to rigidbody in draw loop.
    /// </summary>
    void UpdatePlayerDriveForce() {

        if (currState == BallState.Sprint) {
            // when sprint, all movement are done automatically until shoot.
            return;
        }

        worldDriveForce = worldMovementDirection * moveForce;
        if (currState == BallState.Hooked) {
            worldDriveForce.x *= hookedMoveForceMul;
            worldDriveForce.z *= hookedMoveForceMul;
        }
        worldDriveForce.y = 0;
        rb.AddForce(worldDriveForce);
    }

    /// <summary>
    /// Process rope's force to the player ball (and apply it).
    /// </summary>
    void UpdateRopeForce() {

        if (currState == BallState.Idle) {
            return;
        }

        hookTime += Time.deltaTime;

        float distance = Vector3.Distance(hookedStub.transform.position, transform.position);

        if (distance > ropeLength) {

            // step.1: kill all energy (velosity) parallel to rope's direction
            Vector3 velocity = rb.velocity;
            Vector3 ropeDirToStub = Vector3.Normalize(hookedStub.transform.position - transform.position);
            velocity -= Vector3.Project(velocity, ropeDirToStub);
            rb.velocity = velocity;

            // step.2: correct physic deviation
            float diff = distance - ropeLength;
            Vector3 toStub = ropeDirToStub * diff;
            Vector3 pos = transform.position;
            pos += toStub;
            transform.position = pos;
        }
    }

    // --------------------------
    // region Unity Lifecycle

    void Start() {
        rb = GetComponent<Rigidbody>();
        foreach (Transform t in stubRoot) {
            stubs.Add(t.gameObject);
        }
    }

    void Update() {

        UpdateWorldDirection();

        // better be in FSM, but well...
        UpdatePlayerDriveForce();
        UpdateSprint();
        UpdateRopeForce();
    }

    private void OnDrawGizmos() {
        Color color = Gizmos.color;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, worldDriveForce);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, worldAimDirection);

        Gizmos.color = color;
    }

    void OnGUI() {
        GUILayout.Label("Ball Controller:");
        GUILayout.Label("   Speed: " + GetComponent<Rigidbody>().velocity.magnitude.ToString());
        GUILayout.Label("   State: " + currState.ToString());

    }
}
