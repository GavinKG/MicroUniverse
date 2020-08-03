using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MasterPillarProp : PillarProp {
        
        public int companionBallCount = 0;

        public bool CompanionSpawned { get; private set; } = false;

        public void SetCompanionBallSpawned() {
            CompanionSpawned = true;
        }

        protected override void OnTriggerEnter(Collider other) {
            base.OnTriggerEnter(other);
        }
    }

}