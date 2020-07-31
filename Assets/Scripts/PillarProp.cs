using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    [RequireComponent(typeof(Collider))]
    public class PillarProp : CityProp {

        public GameObject maskParticle;

        bool activated = false;

        protected virtual void OnTriggerEnter(Collider other) {
            if (!activated) {

                GameObject go = other.gameObject;
                //TODO: 
                maskParticle.SetActive(true);
                ParticleSystem ps = maskParticle.GetComponent<ParticleSystem>();
                ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 2);
                ps.emission.SetBursts(new ParticleSystem.Burst[] { burst });

                activated = true;
            }

        }

    }

}