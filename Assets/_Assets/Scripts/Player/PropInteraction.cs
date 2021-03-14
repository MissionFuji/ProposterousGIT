using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

public class PropInteraction : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunInstantiateMagicCallback {

    public bool isAvailable = true;
    public bool isHostOnly = false;
    public bool isMapCartridge = false;
    private Rigidbody rb;
    [SerializeField]
    private List<GameObject> propsSpawnedDuringGame = new List<GameObject>();



    private void Awake() {
        ResetRigidBodyAfterDetach();
    }

    public void ResetRigidBodyAfterDetach() {
        rb = gameObject.GetComponent<Rigidbody>();
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

            if (PhotonNetwork.IsMasterClient) { 
                if (info.photonView.gameObject.tag == "Map") {
                    // If a new map has been spawned in, let's delete all of the props that were spawned in last game.
                    foreach(GameObject obj in propsSpawnedDuringGame) {
                        if (obj != null)
                        PhotonNetwork.Destroy(obj);
                    }
                    Debug.Log("Destoyed " + propsSpawnedDuringGame.Count + " props that were left over after last game.");
                    // Then let's clear our list when we're done.
                    propsSpawnedDuringGame.Clear();
                }
            }

            object[] receivedInstData = info.photonView.InstantiationData;
            int spawnRoutine = (int)receivedInstData[0]; // First index only. 0 == Prop Spawner. 1 == Player Spawned Take-Over Prop. 2 == Player Spawning Seeker/Ghost Prefab. 3 == DeadProp Spawning Ghost.
            if (spawnRoutine == 0) { // Prop Spawner.

                //We want to add these objects to a list when they're spawned to PHotonNetwork.Remove them when we switch levels.
                propsSpawnedDuringGame.Add(info.photonView.gameObject);

            } else if (spawnRoutine == 1) { // Prop spawned for sake of Takeover.

                //We want to add these objects to a list when they're spawned to PHotonNetwork.Remove them when we switch levels.
                propsSpawnedDuringGame.Add(info.photonView.gameObject);

                //Grab our localPlayer "P" Object.
                GameObject plyTagObj = (GameObject)info.Sender.TagObject;
                PhotonView targetPlayerPV = plyTagObj.GetComponent<PhotonView>();

                Debug.Log(targetPlayerPV.Owner.NickName + " has instantiated: " + gameObject.name + ", with prop viewID: " + gameObject.GetComponent<PhotonView>().ViewID);

                //Build our vars.
                GameObject plyObject = targetPlayerPV.gameObject;
                Rigidbody plyRB = plyObject.GetComponent<Rigidbody>();
                PropRigidbodyTransformView prtv = gameObject.GetComponent<PropRigidbodyTransformView>();
                PropInteraction pInt = gameObject.GetComponent<PropInteraction>();

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
