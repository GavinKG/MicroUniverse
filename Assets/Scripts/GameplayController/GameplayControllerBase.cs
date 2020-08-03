using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MicroUniverse {
    public abstract class GameplayControllerBase : MonoBehaviour {

        protected virtual void Start() {
            GameManager.Instance.CurrController = this;
        }

        public bool Running { get; protected set; } = false;

        public abstract void Begin();
        public abstract void Finish();

    }
}