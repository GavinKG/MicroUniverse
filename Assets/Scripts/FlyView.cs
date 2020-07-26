using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlyView : MonoBehaviour {
    public float rotationSpeed = 5f;
    public float movementSpeed = 1f;

    Vector2 inputMovementAxis;
    Vector2 inputAimAxis;
    float upDownAxis;
    public bool speedup;

    public void OnPlayerMovementInput(InputAction.CallbackContext context) {
        inputMovementAxis = context.ReadValue<Vector2>();
    }

    public void OnPlayerAimInput(InputAction.CallbackContext context) {
        inputAimAxis = context.ReadValue<Vector2>();
    }

    public void OnPlayerSpeedupInput(InputAction.CallbackContext context) {
        if (context.started) {
            speedup = true;
        } else if (context.canceled) {
            speedup = false;
        }
    }

    public void OnPlayerUpDownInput(InputAction.CallbackContext context) {
        upDownAxis = context.ReadValue<float>();
    }

    private void Update() {

        Vector3 rot = transform.eulerAngles;
        rot.y += inputAimAxis.x * rotationSpeed * Time.deltaTime;
        rot.x += -inputAimAxis.y * rotationSpeed * Time.deltaTime;
        if (rot.x > 89.9f) {
            rot.x = 89.9f;
        } else if (rot.x < -70f) {
            rot.x = -70f;
        }

        transform.eulerAngles = rot;

        Vector3 currAiming = transform.rotation * Vector3.forward;
        Vector3 xzAimingN = new Vector3(currAiming.x, 0f, currAiming.z).normalized;
        Vector3 rightN = Vector3.Cross(xzAimingN, Vector3.up);
        Vector3 pos = transform.position;
        pos += (inputMovementAxis.y * xzAimingN + inputMovementAxis.x * -rightN + upDownAxis * Vector3.up) * movementSpeed * Time.deltaTime * (speedup ? 2f : 1f);
        transform.position = pos;
    }
}
