using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable] public class GravityEvent : UnityEvent<Vector3> { }


[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour {

    public float gravity = 9.8f; // pointing -Y
    [Range(0.01f, 0.5f)] public float maxTiltDistance = 0.3f; // like tangent

    public GravityEvent onGravityUpdate;
    public bool preferGravitySensor = true;

    // Input related:
    Vector2 inputMovementAxis;
    Vector3 worldMovementDirection;

    Vector3 gravityDirection;
    Vector3 lastLegalGravityDirection = Vector3.down;

    Rigidbody rb;


    public void OnPlayerMovementInput(InputAction.CallbackContext context) {
        inputMovementAxis = context.ReadValue<Vector2>();
        float sqrDistance = inputMovementAxis.sqrMagnitude;
        if (sqrDistance > 1) {
            inputMovementAxis.Normalize();
        }
        print(inputMovementAxis);
    }

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody>();
        if (preferGravitySensor && GravitySensor.current != null) {
            InputSystem.EnableDevice(GravitySensor.current);
        }
        
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

        if (preferGravitySensor && GravitySensor.current != null) {
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
            gravityDirection = new Vector3(x, y, z).normalized;
        } else {
            gravityDirection = new Vector3(worldMovementDirection.x * maxTiltDistance, -1, worldMovementDirection.z * maxTiltDistance).normalized;
        }
        Vector3 force = gravityDirection * gravity;
        rb.AddForce(force);
    }
}
