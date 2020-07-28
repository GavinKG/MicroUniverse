using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallControllerSimple : MonoBehaviour
{

    public float moveForce = 3f;

    Vector2 inputMovementAxis;
    Vector3 worldMovementDirection;
    Vector3 worldDriveForce;
    Rigidbody rb;


    public void OnPlayerMovementInput(InputAction.CallbackContext context) {
        inputMovementAxis = context.ReadValue<Vector2>();
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateWorldDirection();
        UpdatePlayerDriveForce();
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

    void UpdatePlayerDriveForce() {
        
        worldDriveForce = worldMovementDirection * moveForce;
        worldDriveForce.y = 0;
        rb.AddForce(worldDriveForce);
    }
}
