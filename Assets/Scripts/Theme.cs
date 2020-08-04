using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class Theme : MonoBehaviour {

        public Color main;

        public Color floor;
        public Color buildingRoof;
        public Color buildingBody;
        public Color buildingFrame;
        public Color buildingWindows;
        public Color propBase;
        public Color empty;
        public Color plant;

        // Material instances used for this theme.
        public Material BuildingMat { get; private set; }
        public Material BaseMat { get; private set; }
        public Material EmptyMat { get; private set; }
        public Material PlantMat { get; private set; }

        bool instanceCreated = false;

        public void CreateMaterialInstance(Material buildingT, Material baseT, Material emptyT, Material plantT) {
            
            // T stands for template.

            if (instanceCreated) {
                return;
            }

            BuildingMat = new Material(buildingT);
            BaseMat = new Material(baseT);
            EmptyMat = new Material(emptyT);
            PlantMat = new Material(plantT);

            BuildingMat.SetColor("_NW", buildingRoof);
            BuildingMat.SetColor("_NE", buildingBody);
            BuildingMat.SetColor("_SE", buildingFrame);
            BuildingMat.SetColor("_SW", buildingWindows);

            BaseMat.SetColor("_Diffuse", propBase);

            EmptyMat.SetColor("_Diffuse", empty);

            PlantMat.SetColor("_Diffuse", plant);

            instanceCreated = true;
        }
    }

}