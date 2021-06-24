using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterX : MonoBehaviour
{

    [Tooltip("THIS IS FOR NON-NETWORK GAMEOBJECTS ONLY. DO NOT USE ON NETOBJECTS.")]
    public float timeBeforeDestroy;

    private void Awake() {
        Invoke("Invoke_DestroyAfterX", timeBeforeDestroy);
    }

    private void Invoke_DestroyAfterX() {
        Destroy(gameObject);
    }

}
