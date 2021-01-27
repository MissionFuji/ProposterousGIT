using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateMMCamera : MonoBehaviour
{
    public float rotateSpeed;
    public bool plyInRoom = false;
    public bool test = false;


    private void Update() {
        if (!plyInRoom) {
            transform.RotateAround(transform.position, transform.up, Time.deltaTime * rotateSpeed);
        } else {
            if (!Mathf.Approximately(transform.rotation.y, 0f)) {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, (2f * rotateSpeed) * Time.deltaTime);
            }
        }
        
    }
}
