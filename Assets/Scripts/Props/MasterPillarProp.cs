using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MasterPillarProp : PillarProp {
        
        public bool withCompanionBall;

        // BallController is in charge of spawning companion ball here (for historical reasons)
        // should only be set by ball controller...
        public bool CompanionSpawned { get; set; } = false;
        
    }

}