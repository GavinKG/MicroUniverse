using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    [RequireComponent(typeof(Rigidbody))]
    public class CompanionBallController : MonoBehaviour {

        Rigidbody rb;
        BallController ballController;

        private void Start() {
            rb = GetComponent<Rigidbody>();
            ballController = (GameManager.Instance.CurrController as MainGameplayController).ballGO.GetComponent<BallController>();
        }

        private void Update() {
            rb.AddForce(ballController.GravityForce);
        }

    }

}