using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TMP_CameraSpin : MonoBehaviour
{
    public float rotSpeed = 5f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, rotSpeed * Time.deltaTime, 0);
    }
}
