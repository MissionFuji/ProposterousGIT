using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTarController : MonoBehaviour
{

    private GameObject ourPlayer;
    [SerializeField]
    private float lerpSpeed;
    [SerializeField]
    private float offset;


    public void SetFollowCamTarget(GameObject ourPly) {
        ourPlayer = ourPly;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (ourPlayer != null) {
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, ourPlayer.transform.position + new Vector3(0, offset, 0), Time.deltaTime * lerpSpeed);
        }
    }
}
