using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {

    public class ThemeMaterialHolder {

        public ThemeMaterialHolder(Theme theme) {
            this.theme = theme;
        }

        // Material instances used for this theme.
        public Material BuildingMat { get; private set; }
        public Material BaseMat { get; private set; }
        public Material EmptyMat { get; private set; }
        public Material PlantMat { get; private set; }

        public bool InstanceCreated { get; private set; } = false;

        public Theme theme;

        public void CreateMaterialInstance(Material buildingT, Material baseT, Material emptyT, Material plantT) {

            // T stands for template.

            if (InstanceCreated) {
                return;
            }

            BuildingMat = new Material(buildingT);
            BaseMat = new Material(baseT);
            EmptyMat = new Material(emptyT);
            PlantMat = new Material(plantT);

            BuildingMat.SetColor("_NW", theme.buildingRoof);
            BuildingMat.SetColor("_NE", theme.buildingBody);
            BuildingMat.SetColor("_SE", theme.buildingFrame);
            BuildingMat.SetColor("_SW", theme.buildingWindows);

            BaseMat.SetColor("_Diffuse", theme.propBase);

            EmptyMat.SetColor("_Diffuse", theme.empty);

            PlantMat.SetColor("_Diffuse", theme.plant);

            InstanceCreated = true;
        }
    }

}
