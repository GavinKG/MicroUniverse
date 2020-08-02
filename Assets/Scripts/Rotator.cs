using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class Rotator : MonoBehaviour {
        public enum RotateAround {
            X, Y, Z
        }

        public RotateAround rotateAround;
        public float speed = 1f;


        void Update() {
            Vector3 euler = transform.eulerAngles;
            switch (rotateAround) {
                case RotateAround.X:
                    euler.x += speed * Time.deltaTime;
                    if (euler.x > 360) {
                        euler.x = 0;
                    }
                    break;
                case RotateAround.Y:
                    euler.y += speed * Time.deltaTime;
                    if (euler.y > 360) {
                        euler.y = 0;
                    }
                    break;
                case RotateAround.Z:
                    euler.z += speed * Time.deltaTime;
                    if (euler.z > 360) {
                        euler.z = 0;
                    }
                    break;
            }
            transform.eulerAngles = euler;
        }
    }

}