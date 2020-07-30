using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    [RequireComponent(typeof(Collider))]
    public class PillarProp : CityProp {

        public GameObject maskParticle;

        private void OnTriggerEnter(Collider other) {
            GameObject go = other.gameObject;
            //TODO: 
            maskParticle.SetActive(true);
        }

    }

}