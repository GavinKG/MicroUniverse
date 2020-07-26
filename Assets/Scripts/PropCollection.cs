using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MicroUniverse {

    [System.Serializable]
    public class PropCollection {

        public List<BuildingProp> buildings;
        public List<CityProp> fountains;
        public List<CityProp> emptys;
        public List<CityProp> pillars;

        [Range(0f, 1f)] public float buildingLMSep = 0.4f;
        [Range(0f, 1f)] public float buildingMHSep = 0.8f;

        public GameObject GetBuildingPrefab(float heat, BuildingProp.BuildingType buildingType) {
            // turn: lookat:
            // 0 -> forward
            // 1 -> left
            // 2 -> back
            // 3 -> right

            BuildingProp.HeightType heightType;
            if (heat < buildingLMSep) {
                heightType = BuildingProp.HeightType.Low;
            } else if (heat < buildingMHSep) {
                heightType = BuildingProp.HeightType.Mid;
            } else {
                heightType = BuildingProp.HeightType.High;
            }

            var filtered = buildings.Where(bp => bp.buildingType == buildingType && bp.heightType == heightType);
            int totalWeight = 0;
            foreach (BuildingProp prop in filtered) {
                totalWeight += prop.propWeight;
            }
            int dice = Random.Range(0, totalWeight);
            int currWeight = 0;
            foreach (BuildingProp prop in filtered) {
                currWeight += prop.propWeight;
                if (currWeight >= dice) {
                    return prop.gameObject;
                }
            }

            // cannot find proper building, fallback to DontCare:
            var filteredDontcare = buildings.Where(bp => bp.buildingType == BuildingProp.BuildingType.DontCare && bp.heightType == heightType).ToArray();
            if (filteredDontcare.Length == 0) {
                throw new System.Exception("Cannot find proper building with building type DontCare or " + buildingType.ToString() + " and height type " + heightType.ToString());
            }
            int dontcareIndex = Random.Range(0, filteredDontcare.Length);
            return filteredDontcare[dontcareIndex].gameObject;

        }

        GameObject GetNormalPropPrefab(List<CityProp> props) {

            // TODO: Cache
            int totalWeight = 0;
            foreach (CityProp prop in props) {
                totalWeight += prop.propWeight;
            }
            int dice = Random.Range(0, totalWeight);
            int currWeight = 0;
            foreach (CityProp prop in props) {
                currWeight += prop.propWeight;
                if (currWeight >= dice) {
                    return prop.gameObject;
                }
            }
            throw new System.Exception("Cannot find props...");

        }

        public GameObject GetFountainPrefab() { return GetNormalPropPrefab(fountains); }
        public GameObject GetEmptyPrefab() { return GetNormalPropPrefab(emptys); }
        public GameObject GetPillarPrefab() { return GetNormalPropPrefab(pillars); }
    }

}


