using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PropInteraction : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunInstantiateMagicCallback {

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


    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info) {
        if (gameObject.GetComponent<PropInteraction>()) { // Are we a prop? 

            //This is used for when a new prop is PhotonNetwork.Instantiated for the sake of being taken-over. 

            Debug.Log(info.Sender.NickName + " has instantiated: " + gameObject.name + ", with prop viewID: " + gameObject.GetComponent<PhotonView>().ViewID);
            GameObject plyObject = (GameObject)info.Sender.TagObject;
            Rigidbody plyRB = plyObject.GetComponent<Rigidbody>();
            //We need to destroy the rigidbody, disable rigidbodytransformview, and clear observed components on photonview.
            Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<PhotonView>().ObservedComponents.Clear();
            gameObject.GetComponent<RigidbodyTransformView>().enabled = false;
            //Update PropInteraction on this newly spawned network object.
            gameObject.GetComponent<PropInteraction>().isAvailable = false;
            //We make sure the prop is on PropInteraction layer. Unless we're the owner, then we remove it.
            if (info.Sender.IsLocal) {
                gameObject.layer = 0;
            } else {
                gameObject.layer = 11;
            }
            //Prop takeover, parent, then apply transforms to it.
            gameObject.transform.parent = plyObject.transform.Find("PropHolder");
            gameObject.transform.localPosition = Vector3.zero;
            //re-enable rigidbody so we can move around again.
            plyRB.freezeRotation = false;
            plyRB.isKinematic = false;
        }
    }
}
