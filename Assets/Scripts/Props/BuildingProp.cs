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

        public GameObject baseGO;
        public GameObject destroyedPrefab;
        public bool Destroyed { get; private set; } = false;

        public void Destroy() {
            //TODO:
            gameObject.SetActive(false);
            Destroyed = true;
        }

        /// <summary>
        /// Replace existing props (not base) to the new props from given prop prefab.
        /// Old props will be destroyed; new props will be instantiated.
        /// Used for replacing destroyed version of this building on the go.
        /// </summary>
        /// <param name="newProp"></param>
        public void ReplaceBuildingProp(BuildingProp newProp) {

            // Destroy
            foreach (Transform t in transform) {
                if (t.gameObject != baseGO) {
                    Destroy(t.gameObject);
                }
            }

            // Instantiate new
            foreach (Transform t in newProp.transform) {
                if (t.gameObject != newProp.baseGO) {
                    Instantiate(t.gameObject, transform); // apply sqt or not?
                }
            }

            // doesn't care whether baseGO is null or not!
        }


    }
}