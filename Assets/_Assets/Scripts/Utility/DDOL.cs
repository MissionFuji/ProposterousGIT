using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDOL : MonoBehaviour
{

    public bool isSingleton;
    private static DDOL s;

    private void Awake() {
        if (isSingleton) {
            if (DDOL.s == null) {
                DDOL.s = this;
            } else {
                if (DDOL.s != this) {
                    Destroy(DDOL.s.gameObject);
                    Debug.Log("destroyed object.");
                    DDOL.s = this;
                }
            }
        }
        DontDestroyOnLoad(this.gameObject);
    }
}
