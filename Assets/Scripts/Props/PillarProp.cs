using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    [RequireComponent(typeof(Collider))]
    public class PillarProp : RoadProp {

        public GameObject maskParticle;

        public bool Activated { get; private set; } = false;

        public void Activate(bool notifyController) {
            if (Activated) return;
            maskParticle.SetActive(true);
            ParticleSystem ps = maskParticle.GetComponent<ParticleSystem>();
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0, 2);
            ps.emission.SetBursts(new ParticleSystem.Burst[] { burst });
            if (notifyController) {
                MainGameplayController controller = GameManager.Instance.CurrController as MainGameplayController;
                controller.PillarEnabled(this);
            }

            Activated = true;
        }

        public void Deactivate(bool notifyController) {
            if (!Activated) return;
            maskParticle.SetActive(false);
            if (notifyController) {
                MainGameplayController controller = GameManager.Instance.CurrController as MainGameplayController;
                controller.PillarDisabled(this);
            }

            Activated = false;
        }

    }

}