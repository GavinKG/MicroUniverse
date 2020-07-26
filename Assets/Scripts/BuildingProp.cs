using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class BuildingProp : CityProp {

        public enum HeightType {
            Low,
            Mid,
            High
        }

        public enum BuildingType {
            Corner,
            AlongsideRoad,
            AlongsideTwoRoads,
            Stub,
            Alone,
            DontCare
        }



        public HeightType heightType;
        public BuildingType buildingType;


    }
}