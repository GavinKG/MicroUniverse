using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    [System.Serializable]
    public class PropCollection {

        public List<GameObject> buildingPrefabsLowHeight;
        public List<GameObject> buildingPrefabsMediumHeight;
        public List<GameObject> buildingPrefabsHighHeight;
        public List<GameObject> pillarPrefabs;
        public List<GameObject> emptyPrefabs;
        public List<GameObject> fountainPrefabs;

        [Range(0f, 1f)] public float buildingLowMediumSeperator = 0.2f;
        [Range(0f, 1f)] public float buildingMediumHighSeperator = 0.8f;


        int BuildingLowHeightTotalWeight {
            get {
                if (bLowHeightTotalWeight < 0) {
                    bLowHeightTotalWeight = 0;
                    foreach (GameObject go in buildingPrefabsLowHeight) {
                        CityProp prop = go.GetComponent<CityProp>();
                        bLowHeightTotalWeight += prop.propWeight;
                    }
                }
                return bLowHeightTotalWeight;
            }
        }
        int bLowHeightTotalWeight = -1;


        int BuildingMediumHeightTotalWeight {
            get {
                if (bMediumHeightTotalWeight < 0) {
                    bMediumHeightTotalWeight = 0;
                    foreach (GameObject go in buildingPrefabsMediumHeight) {
                        CityProp prop = go.GetComponent<CityProp>();
                        bMediumHeightTotalWeight += prop.propWeight;
                    }
                }
                return bMediumHeightTotalWeight;
            }
        }
        int bMediumHeightTotalWeight = -1;


        int BuildingHighHeightTotalWeight {
            get {
                if (bHighHeightTotalWeight < 0) {
                    bHighHeightTotalWeight = 0;
                    foreach (GameObject go in buildingPrefabsHighHeight) {
                        CityProp prop = go.GetComponent<CityProp>();
                        bHighHeightTotalWeight += prop.propWeight;
                    }
                }
                return bHighHeightTotalWeight;
            }
        }
        int bHighHeightTotalWeight = -1;

        int PillarTotalWeight {
            get {
                if (pillarTotalWeight < 0) {
                    pillarTotalWeight = 0;
                    foreach (GameObject go in pillarPrefabs) {
                        CityProp prop = go.GetComponent<CityProp>();
                        pillarTotalWeight += prop.propWeight;
                    }
                }
                return pillarTotalWeight;
            }
        }
        int pillarTotalWeight = -1;

        int EmptyTotalWeight {
            get {
                if (emptyTotalWeight < 0) {
                    emptyTotalWeight = 0;
                    foreach (GameObject go in emptyPrefabs) {
                        CityProp prop = go.GetComponent<CityProp>();
                        emptyTotalWeight += prop.propWeight;
                    }
                }
                return emptyTotalWeight;
            }
        }
        int emptyTotalWeight = -1;

        int FountainTotalWeight {
            get {
                if (fountainTotalWeight < 0) {
                    fountainTotalWeight = 0;
                    foreach (GameObject go in fountainPrefabs) {
                        CityProp prop = go.GetComponent<CityProp>();
                        fountainTotalWeight += prop.propWeight;
                    }
                }
                return fountainTotalWeight;
            }
        }
        int fountainTotalWeight = -1;


        public GameObject RandomBuildingLowHeight() {
            int dice = Random.Range(0, BuildingLowHeightTotalWeight);
            int counter = 0;
            foreach (GameObject go in buildingPrefabsLowHeight) {
                CityProp prop = go.GetComponent<CityProp>();
                counter += prop.propWeight;
                if (counter > dice) {
                    return go;
                }
            }
            return null;
        }

        public GameObject RandomBuildingMediumHeight() {
            int dice = Random.Range(0, BuildingMediumHeightTotalWeight);
            int counter = 0;
            foreach (GameObject go in buildingPrefabsMediumHeight) {
                CityProp prop = go.GetComponent<CityProp>();
                counter += prop.propWeight;
                if (counter > dice) {
                    return go;
                }
            }
            return null;
        }

        public GameObject RandomBuildingHighHeight() {
            int dice = Random.Range(0, BuildingHighHeightTotalWeight);
            int counter = 0;
            foreach (GameObject go in buildingPrefabsHighHeight) {
                CityProp prop = go.GetComponent<CityProp>();
                counter += prop.propWeight;
                if (counter > dice) {
                    return go;
                }
            }
            return null;
        }

        public GameObject RandomPillar() {
            int dice = Random.Range(0, PillarTotalWeight);
            int counter = 0;
            foreach (GameObject go in pillarPrefabs) {
                CityProp prop = go.GetComponent<CityProp>();
                counter += prop.propWeight;
                if (counter > dice) {
                    return go;
                }
            }
            return null;
        }

        public GameObject RandomFountain() {
            int dice = Random.Range(0, FountainTotalWeight);
            int counter = 0;
            foreach (GameObject go in fountainPrefabs) {
                CityProp prop = go.GetComponent<CityProp>();
                counter += prop.propWeight;
                if (counter > dice) {
                    return go;
                }
            }
            return null;
        }

        public GameObject RandomEmpty() {
            int dice = Random.Range(0, EmptyTotalWeight);
            int counter = 0;
            foreach (GameObject go in emptyPrefabs) {
                CityProp prop = go.GetComponent<CityProp>();
                counter += prop.propWeight;
                if (counter > dice) {
                    return go;
                }
            }
            return null;
        }

    }

}


