using Photon.Pun;
using UnityEngine;

public class RefillAmmoDevTool : MonoBehaviour
{

    private PlayerPropertiesController ppc;

    private void Awake() {
        ppc = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.GetComponent<PropInteraction>()) {
            PlayerMovement pm = other.gameObject.transform.parent.transform.parent.GetComponent<PlayerMovement>();
            PhotonView pv = pm.gameObject.GetPhotonView();
            if (pv.IsMine) {
                    if (ppc.moveState == 3) {
                        Debug.Log("Seeker trying to reload their shots.");
                        pm.ResetMistakeCount();
                    }
            }
        }
    }

}
