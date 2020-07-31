using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable] public class GravityEvent : UnityEvent<Vector3> { }


[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour {

    public float gravity = 9.8f; // pointing -Y
    [Range(0f, 1f)] public float maxGravityTilt = 0.5f;

    public GravityEvent onGravityUpdate;

    // Input related:
    Vector2 inputMovementAxis;
    Vector3 worldMovementDirection;

    Vector3 gravityDirection;

    Rigidbody rb;


    public void OnPlayerMovementInput(InputAction.CallbackContext context) {
        inputMovementAxis = context.ReadValue<Vector2>();
    }

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody>();
    }

    void Update() {
        UpdateWorldDirection();
        DispatchEvent();
    }

    void FixedUpdate() {
        UpdateGravity();
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

    }

    void DispatchEvent() {
        onGravityUpdate.Invoke(gravityDirection);
    }

    void UpdateGravity() {
        gravityDirection = new Vector3(worldMovementDirection.x * maxGravityTilt, -1, worldMovementDirection.z * maxGravityTilt).normalized;
        Vector3 force = gravityDirection * gravity;
        rb.AddForce(force);
    }
}
