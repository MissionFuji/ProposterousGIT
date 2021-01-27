using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{

    private Transform mainCamT;

    private void Start() {
        mainCamT = Camera.main.transform;
    }

    private void LateUpdate() {
        transform.LookAt(transform.position + mainCamT.rotation * Vector3.forward,
            mainCamT.rotation * Vector3.up);
    }

}
