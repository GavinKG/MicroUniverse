using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MicroUniverse {
    public class RegionPortal : MonoBehaviour {

        [HideInInspector] public int currRegionId;
        [HideInInspector] public int toRegionId;
        public float portalSpawnOffsetDistance = 0.1f;
        
        public Vector3 PortalSpawnPosition {
            get {
                return transform.position + transform.TransformDirection(Vector3.forward) * portalSpawnOffsetDistance;
            }
        }


        public void SetPortalActive() {
            gameObject.SetActive(true);
            // play some active effect...
        }

        public void ToNewRegion() {
            MainGameplayController controller = GameManager.Instance.CurrController as MainGameplayController;
            controller.GotoRegion(toRegionId);
        }

        // TODO: set collision layer!
        public void OnTriggerEnter(Collider other) {
            ToNewRegion();
        }

    }

}