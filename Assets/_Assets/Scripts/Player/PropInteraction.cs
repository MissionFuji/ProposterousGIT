using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using JacobGames.SuperInvoke;
using UnityEngine;

public class PropInteraction : MonoBehaviourPunCallbacks, IInRoomCallbacks, IPunInstantiateMagicCallback {

    [Tooltip("Is this prop available for takeover?")]
    public bool isAvailable = true;

    [Tooltip("Is this prop masterclient only?")]
    public bool isHostOnly = false;

    [Tooltip("Is this a prop used by the masterclient to start the game?")]
    public bool isMapCartridge = false;

    [Tooltip("Is this prop in stasis?")]
    [SerializeField]
    private bool isStasisActive = false;

    [Tooltip("How long do we want stasis to last?")]
    [SerializeField]
    private int stasisDuration;

    [Tooltip("What material do you want to use for stasis?")]
    [SerializeField]
    private Material stasisMat;

    [Tooltip("What particles do you want to emit on stasis start?")]
    [SerializeField]
    private ParticleSystem stasisParticles;

    private int propID = -1;
    private Rigidbody rb;
    Dictionary<Renderer, Material> defaultMatDict;



    private void Awake() {
        ResetRigidBodyAfterDetach();
        defaultMatDict = new Dictionary<Renderer, Material>();
        GetOurDefaultMaterials(gameObject.transform); // This searches our children for the material if there isn't one on this parent object.
        if (transform.parent != null) { // If we SPAWN this object with our parent over it already (first player spawn of the session is the only time this happens.)
            if (gameObject.transform.root.gameObject.tag == "LocalPlayer") { // Make sure it's our localPlayer object.
                gameObject.layer = 0;
            }
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
    
    // This is ran from PlayerMovement when a seeker tries to stasis-ify a possessed prop.
    public void TrySetStasis(int senderID, int propID) {
        PhotonView sender = PhotonView.Find(senderID);
        // We make sure that the "sender" is us. (We're the seeker that initiated the original call to stasis the prop..)
        if (sender.IsMine) {
            photonView.RPC("RPC_StasisOnProp", RpcTarget.All, propID);
        }
    }

    public bool GetStasisStatus() {
        return isStasisActive;
    }

    private void GetOurDefaultMaterials(Transform objToSearch) {

        // Does THIS object have a renderer and mat?
        if (objToSearch.gameObject.GetComponent<Renderer>()) {
            // We got our renderer.
            Renderer r = objToSearch.gameObject.GetComponent<Renderer>();
            // Does is have a mat on it?
            if (r.material != null) {
                // Get that mat.
                Material m = r.material;
                // Store the renderer and mat as kv pairs.
                if (m != null) {
                    defaultMatDict.Add(r, m);
                }
            }
        }

        // Dig deeper for other mats.
        foreach (Transform t in objToSearch) {
            GetOurDefaultMaterials(t);
        }

    }

    // Interate through our dict by renderer, and setting those materials as stasis mat.
    private void AddStasisMaterialAndParticles() {
        foreach(Renderer r in defaultMatDict.Keys) {
            r.material = stasisMat;
        }
        Instantiate(stasisParticles, gameObject.transform.position, gameObject.transform.rotation);
    }

    // Runs on all clients.
    [PunRPC]
    private void RPC_StasisOnProp( int propID) {
        if (!isStasisActive) {
            // Enter stasis.
            isStasisActive = true;

            // Get our default materials if we need them.
            if (defaultMatDict.Count <= 0) {
                GetOurDefaultMaterials(gameObject.transform);
                Debug.LogWarning("We found that this PropInteraction object doesn't have any materials?.. Looking again.");
            }

            // Add stasis material.
            AddStasisMaterialAndParticles();

            // If we're the masterclient, start a time to revert this stasis.
            if (PhotonNetwork.IsMasterClient) {
                SuperInvoke.Run(() => Invoke_EndStasisCountdown(propID), stasisDuration);
            }

        } else {
            Debug.LogWarning("Tried to add stasis to a prop that already has stasis enabled?");
        }
    }

    [PunRPC]
    private void RPC_EndStasis(int propID) {
        foreach (KeyValuePair<Renderer, Material> kv in defaultMatDict) {
            kv.Key.material = kv.Value;
        }
        defaultMatDict.Clear();
        isStasisActive = false;
    }

    private void Invoke_EndStasisCountdown(int propID) {
        if (PhotonNetwork.IsMasterClient) {
            photonView.RPC("RPC_EndStasis", RpcTarget.All, propID);
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

                //Build our vars.
                Rigidbody plyRB = plyTagObj.GetComponent<Rigidbody>();

                //If it's ours, we need to make sure it's not on the PropInteraction layer. Let's set it to default locally.
                if (gameObject.GetPhotonView().IsMine) {
                    Debug.Log("Looks like we spawned a seeker/ghost prop that belongs to us: " + gameObject.name);
                    gameObject.layer = 0;
                }

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
