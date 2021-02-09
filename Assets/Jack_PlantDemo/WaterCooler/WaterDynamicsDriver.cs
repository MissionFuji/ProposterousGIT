using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterDynamicsDriver : MonoBehaviour
{
    public Transform surfaceIndicator;
    public MeshRenderer meshRenderer;

    Material[] mats;

    int posHash = Shader.PropertyToID("_planePos");
    int norHash = Shader.PropertyToID("_planeNormal");

    Vector3 targetUp;
    Vector3 velocity;
    Vector3 offset;
    Vector3 lastPos;

    // Start is called before the first frame update
    void Start()
    {
        mats = meshRenderer.materials;
        targetUp = Vector3.up;
        velocity = Vector3.zero;
        offset = Vector3.zero;
        lastPos = surfaceIndicator.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        velocity = (surfaceIndicator.transform.position - lastPos) / Time.deltaTime;
        offset += velocity * Time.deltaTime * .1f;

        Debug.Log(Mathf.PingPong(Time.time, 8));
        /*
        if(velocity == Vector3.zero)
        {
            offset *= Mathf.PingPong(Time.time, 8);
        }*/

        Debug.DrawLine(surfaceIndicator.transform.position, surfaceIndicator.transform.position + offset, Color.red);

        surfaceIndicator.transform.up = targetUp + offset;

        foreach (Material mat in mats)
        {
            mat.SetVector(posHash, surfaceIndicator.transform.position);
            mat.SetVector(norHash, -surfaceIndicator.transform.up);
        }

        lastPos = surfaceIndicator.transform.position;
    }
}
