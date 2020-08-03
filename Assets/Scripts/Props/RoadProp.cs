using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class RoadProp : CityProp {

        [HideInInspector] public RoadProp left;
        [HideInInspector] public RoadProp right;
        [HideInInspector] public RoadProp top;
        [HideInInspector] public RoadProp bottom;


    }

}