using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class LightRotator : MonoBehaviour {

        public Vector3 initialRotation;
        
        public float speed = 5f;

        Vector3 currRotation;

        void Start() {
            currRotation = initialRotation;
        }
        
        void Update() {
            currRotation.y += speed * Time.deltaTime;
            if (currRotation.y > 360f) {
                currRotation.y = -360;
            }
            transform.eulerAngles = currRotation;
        }
    }

}
