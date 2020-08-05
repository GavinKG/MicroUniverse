using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class MasterPillarProp : PillarProp {
        
        public bool withCompanionBall;

        // should only be set by ball controller...
        public bool CompanionSpawned { get; set; } = false;
        
    }

}