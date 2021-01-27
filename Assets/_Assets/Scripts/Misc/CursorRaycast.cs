using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CursorRaycast : MonoBehaviourPunCallbacks, IInRoomCallbacks {

    private RaycastHit objectHit;
    [SerializeField]
    private LayerMask PropInteraction;
    private PlayerPropertiesController PPC;

    private void Awake() {
        PPC = GameObject.FindGameObjectWithTag("PPC").GetComponent<PlayerPropertiesController>();
    }

    private void Update() {
        Vector3 fwd = gameObject.transform.TransformDirection(Vector3.forward);
        Debug.DrawRay(gameObject.transform.position, fwd * 65, Color.green);
        //if () { if pv is mine
        if (Input.GetKeyDown(KeyCode.E)) {
            if (PPC.moveState == 1) {
                if (Physics.Raycast(gameObject.transform.position, fwd, out objectHit, 65, PropInteraction)) {
                    Debug.Log("Raycast detected. : " + objectHit.collider.gameObject.name);
                    if (objectHit.collider.gameObject.tag == "Available") {
                        Debug.Log("As a ghost, you tried to possess: " + objectHit.collider.gameObject.name + ", successful takeover.");
                    } else if (objectHit.collider.gameObject.tag == "Unavailable") {
                        Debug.Log("As a ghost, you tried to possess: " + objectHit.collider.gameObject.name + ", failed takeover. Prop already posessed by another player.");
                    } else if (objectHit.collider.gameObject.tag == "Non-Posessable") {
                        Debug.Log("As a ghost, you tried to possess: " + objectHit.collider.gameObject.name + ", failed takeover. Prop is not Posessable.");
                    }
                }

            }
        }
        //}
    }
}
