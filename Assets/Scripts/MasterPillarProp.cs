using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MasterPillarProp : PillarProp {

        public int auxBallCount = 0;

        protected override void OnTriggerEnter(Collider other) {
            base.OnTriggerEnter(other);
        }
    }

}