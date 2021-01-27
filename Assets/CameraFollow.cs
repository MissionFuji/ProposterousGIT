using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    private GameObject cam;
    [SerializeField]
    private Vector3 offset;
    [SerializeField]
    private float camFollowSpeed;


    private void Awake() {
        cam = Camera.main.gameObject;
    }


    void Update()
    {
        if (cam.transform.position != gameObject.transform.position + offset) {
            cam.transform.position = Vector3.Lerp(cam.transform.position, gameObject.transform.position + offset, Time.deltaTime * camFollowSpeed);
        }
    }
}
