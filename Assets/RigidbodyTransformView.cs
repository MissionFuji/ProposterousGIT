using Photon.Pun;
using UnityEngine;

public class RigidbodyTransformView : MonoBehaviour, IPunObservable {

    PhotonView pv;
    Rigidbody rb;
    Vector3 latestPos;
    Quaternion latestRot;
    Vector3 velocity;
    Vector3 angularVelocity;
    Quaternion latestPropRot;
    GameObject propHolder;
    [SerializeField]
    float lerpSpeed;

    public bool isRotLocked = false;
    bool netRotLocked = false;

    bool valuesReceived = false;

    void Awake() {
        pv = GetComponent<PhotonView>();
        ResetRB();
    }


    void ResetRB() {
        rb = GetComponent<Rigidbody>();
    }

    void ResetPropHolder() {
        propHolder = transform.Find("PropHolder").gameObject;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            //We own this player: send the others our data
            if (rb == null) {
                ResetRB();
            }
            if (propHolder == null) {
                ResetPropHolder();
            }
            //Player
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.angularVelocity);
            //Child Prop
            stream.SendNext(isRotLocked);
            stream.SendNext(propHolder.transform.GetChild(0).transform.rotation);


        } else {
            //Network player, receive data
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();
            velocity = (Vector3)stream.ReceiveNext();
            angularVelocity = (Vector3)stream.ReceiveNext();
            //Child Prop
            netRotLocked = (bool)stream.ReceiveNext();
            latestPropRot = (Quaternion)stream.ReceiveNext();


            valuesReceived = true;

        }
    }

    private void Update() {
        if (!pv.IsMine && valuesReceived) {
            // Let's make sure var don't come back null.
            if (rb == null) {
                ResetRB();
            }
            if (propHolder == null) {
                ResetPropHolder();
            }
            // We move the position and modify rotation under Update.
            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, latestRot, Time.deltaTime * lerpSpeed);

            //Child Prop. We update this rotation over Update as well as it is not a physics change.
            if (netRotLocked) {
                if (propHolder.transform.childCount > 0) {
                    Transform child = propHolder.transform.GetChild(0);
                    if (child != null) {
                        child.transform.rotation = Quaternion.Slerp(child.transform.rotation, latestPropRot, Time.deltaTime * lerpSpeed);
                    }
                }
            }
        }
    }


    void FixedUpdate()
    {
        if (!pv.IsMine && valuesReceived) {
            // Let's make sure var don't come back null.
            if (rb == null) {
                ResetRB();
            }
            if (propHolder == null) {
                ResetPropHolder();
            }
            // Update out physics changes under fixed update.
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;

            // Lerping player's rb values resulted in small amounts of rubber banding. This could likely be tweaked to fix if necessary.
            //rb.velocity = Vector3.Lerp(rb.velocity, velocity, Time.deltaTime * lerpSpeed);
            //rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocity, Time.deltaTime * lerpSpeed);
        }
    }
}
