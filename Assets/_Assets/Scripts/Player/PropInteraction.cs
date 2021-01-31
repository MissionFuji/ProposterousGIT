using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PropInteraction : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    public bool isAvailable = true;
    public bool isHostOnly = false;
    public bool isMapCartridge = false;
    public bool isAlreadyClaimedOverNetwork = false;
    private Rigidbody rb;



    private void Awake() {
        ResetRigidBodyAfterDetach();
    }

    public void ResetRigidBodyAfterDetach() {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    private void FixedUpdate() {
        if (transform.parent == null) {
            if (rb != null) {
                rb.AddForce(Physics.gravity * (rb.mass * rb.mass));
            } else {
                ResetRigidBodyAfterDetach();
            }
        }
    }

}
