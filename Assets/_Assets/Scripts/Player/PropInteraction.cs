using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

public class PropInteraction : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunInstantiateMagicCallback { 
    [SerializeField]
    private int propID = -1;
    [Tooltip("Is this prop available for takeover?")]
    public bool isAvailable = true;
    [Tooltip("Is this prop masterclient only?")]
    public bool isHostOnly = false;
    [Tooltip("Is this a prop used by the masterclient to start the game?")]
    public bool isMapCartridge = false;
    private Rigidbody rb;



    private void Awake() {
        ResetRigidBodyAfterDetach();
        if (transform.parent != null) { // If we SPAWN this object with our parent over it already (aka, initial pre-game lobby spawn.)
            gameObject.layer = 0;
        }
    }

    public void ResetRigidBodyAfterDetach() {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    public int GetPropID() {
        return propID;
    }

    private void FixedUpdate() {
        if (gameObject.tag != "AttachedProp") {
            if (rb != null) {
                rb.AddForce(Physics.gravity * (rb.mass * rb.mass));
            } else {
                ResetRigidBodyAfterDetach();
            }
        }
    }


    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info) {

        /* THIS ENTIRE SECTION IS FOR WHEN YOU BECOME A PROP. WE SPAWN A NEW PROP BEFORE WE TAKE IT OVER */
        if (gameObject.GetComponent<PropInteraction>()) { // Is this a prop?

            object[] receivedInstData = info.photonView.InstantiationData;
            int spawnRoutine = (int)receivedInstData[0]; // First index only. 0 == Prop Spawner. 1 == Player Spawned Take-Over Prop. 2 == Player Spawning Seeker/Ghost Prefab. 3 == DeadProp Spawning Ghost.
            if (spawnRoutine == 0) { // Prop Spawner.


            } else if (spawnRoutine == 1) { // Prop spawned for sake of Takeover.

                //Grab our localPlayer "P" Object.
                GameObject plyTagObj = (GameObject)info.Sender.TagObject;
                PhotonView targetPlayerPV = plyTagObj.GetComponent<PhotonView>();

                Debug.Log(targetPlayerPV.Owner.NickName + " has instantiated: " + gameObject.name + ", with prop viewID: " + gameObject.GetComponent<PhotonView>().ViewID);

                //Build our vars.
                GameObject plyObject = targetPlayerPV.gameObject;
                Rigidbody plyRB = plyObject.GetComponent<Rigidbody>();
                PropRigidbodyTransformView prtv = gameObject.GetComponent<PropRigidbodyTransformView>();
                PropInteraction pInt = gameObject.GetComponent<PropInteraction>();
                PhotonView thisPropPV = gameObject.GetComponent<PhotonView>();

                //We need to destroy the rigidbody, disable proprigidbodytransformview, and clear observed components on photonview.
                Destroy(gameObject.GetComponent<Rigidbody>());
                gameObject.GetComponent<PhotonView>().ObservedComponents.Clear();
                if (prtv)
                    prtv.enabled = false;

                //Update PropInteraction on this newly spawned network object.
                if (pInt)
                    pInt.isAvailable = false;

                //We make sure the prop is on PropInteraction layer. Unless we're the owner, then we remove it so we don't highlight our own prop.
                if (info.Sender.IsLocal) {
                    gameObject.layer = 0;
                } else {
                    gameObject.layer = 11;
                }

                //We need to restrict ownership transfer so other players can't take over our PV on our prop.
                thisPropPV.OwnershipTransfer = OwnershipOption.Fixed;

                //Prop takeover, parent, then apply transforms to it.
                gameObject.transform.parent = plyObject.transform.Find("PropHolder");
                gameObject.transform.localPosition = Vector3.zero;

                //Let's give it a tag so we can better check against other objects in other scripts.
                gameObject.tag = "AttachedProp";

                //re-enable -PLAYER- rigidbody so we can move around again.
                plyRB.freezeRotation = false;
                plyRB.isKinematic = false;
            } else if (spawnRoutine == 2) { //Ghost/Seeker Prefab spawned for player use.
                GameObject plyTagObj = (GameObject)info.Sender.TagObject;
                PhotonView targetPlayerPV = plyTagObj.GetComponent<PhotonView>();

                //Build our vars.
                Rigidbody plyRB = plyTagObj.GetComponent<Rigidbody>();

                //Prop takeover, parent, then apply transforms to it.
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.SetParent(plyTagObj.transform.Find("PropHolder"), false);
                gameObject.transform.localPosition = Vector3.zero;

                //Let's give it a tag so we can better check against other objects in other scripts.
                gameObject.tag = "AttachedProp";

                //Update RB.
                plyRB.freezeRotation = true;
                plyRB.isKinematic = false;

            } else if (spawnRoutine == 3) { //Dead ghost.
                GameObject plyTagObj = (GameObject)info.Sender.TagObject;
                PhotonView targetPlayerPV = plyTagObj.GetComponent<PhotonView>();

                //Build our vars.
                Rigidbody plyRB = plyTagObj.GetComponent<Rigidbody>();

                //Prop takeover, parent, then apply transforms to it.
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.SetParent(plyTagObj.transform.Find("PropHolder"), false);
                gameObject.transform.localPosition = Vector3.zero;

                //Let's give it a tag so we can better check against other objects in other scripts.
                gameObject.tag = "AttachedProp";

                //Update RB.
                plyRB.freezeRotation = true;
                plyRB.isKinematic = false;
            }
        }
    }
}
