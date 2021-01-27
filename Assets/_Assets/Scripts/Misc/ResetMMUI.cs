using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetMMUI : MonoBehaviour
{

    private void OnEnable() {
        gameObject.transform.Find("CancelSearch").gameObject.SetActive(false);
    }

}
