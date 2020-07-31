using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class VCamController : MonoBehaviour {

    public bool worldRotationEffect = true;

    CinemachineVirtualCamera vcam;


    void Start() {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }
}
