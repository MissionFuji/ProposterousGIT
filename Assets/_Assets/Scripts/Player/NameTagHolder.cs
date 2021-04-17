using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NameTagHolder : MonoBehaviourPunCallbacks, IInRoomCallbacks {
    public int ownerID = -1;
    private int actorID = -1;
    public GameObject tarPlayer = null;
    [SerializeField]
    private LayerMask layerToHit;
    [SerializeField]
    private Vector3 nameTagOffset;
    [SerializeField]
    private float ntLerpSpeed;

    private Transform propHolderTrans;
    private Transform updatedTransform;
    private Renderer propRenderer;

    private Vector3 propMeshCenterPosition;

    // Update is called once per frame
    void Update() {

        // Is our player null?
        if (tarPlayer != null) {

            // ownerID will be -1 the first time that it runs. These all get ran once.
            if (ownerID == -1) {
                ownerID = tarPlayer.GetComponent<PhotonView>().ViewID;
                actorID = tarPlayer.GetComponent<PhotonView>().Owner.ActorNumber;
                propHolderTrans = tarPlayer.transform.Find("PropHolder");
            }

            // Required raycast vars.
            RaycastHit hit;
            Vector3 originPoint = new Vector3(tarPlayer.transform.position.x, tarPlayer.transform.position.y + 100f, tarPlayer.transform.position.z);

            // Are we hitting our prop? If we are, just update the information required to move the prop.
            if (Physics.Raycast(originPoint, -gameObject.transform.up, out hit, 200f, layerToHit) && (hit.collider.gameObject.transform.root == tarPlayer.transform)) {

                // Do we have access to our propHolder?
                if (propHolderTrans != null) {
                    propHolderTrans = tarPlayer.transform.Find("PropHolder");
                }

                // Is our renderer null/out of date?
                if (propRenderer == null) {
                    propRenderer = propHolderTrans.GetChild(0).gameObject.GetComponent<Renderer>();
                } else if (updatedTransform != propHolderTrans.GetChild(0)) {
                    updatedTransform = propHolderTrans.GetChild(0);
                    propRenderer = updatedTransform.GetComponent<Renderer>();
                }

                // Update our next nametag position.
                propMeshCenterPosition = propRenderer.bounds.center;

            }

            // Let's move the nametag.
            if (propRenderer != null) {
                gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, (propMeshCenterPosition + nameTagOffset), Time.deltaTime * ntLerpSpeed);
            } else {
                Debug.Log("Couldn't find propRenderer to use in order to calculate next nameTag position.");
            }

        }
    }
}
