using Photon.Pun;
using Photon.Realtime;
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
            } else if (propHolder == null) {
                ResetPropHolder();
            }
            //Player
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.angularVelocity);
            //Child Prop
            if (propHolder.transform.childCount > 0) {
                stream.SendNext(propHolder.transform.GetChild(0).rotation);
            }


        } else {
            //Network player, receive data
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();
            velocity = (Vector3)stream.ReceiveNext();
            angularVelocity = (Vector3)stream.ReceiveNext();
            //Child Prop
            latestPropRot = (Quaternion)stream.ReceiveNext();


            valuesReceived = true;

        }
    }


    void FixedUpdate()
    {
        if (!pv.IsMine && valuesReceived) {
            //Update Object position and Rigidbody parameters
            //Player
            if (rb == null) {
                ResetRB();
            } else if (propHolder == null) {
                ResetPropHolder();
            }
            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, latestRot, Time.deltaTime * lerpSpeed);
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
            //Prop
            //Child Prop
            if (propHolder.transform.childCount > 0) {
                propHolder.transform.GetChild(0).rotation = latestPropRot;
            }

            // Lerping player's rb values resulted in small amounts of rubber banding. This could likely be tweaked to fix if necessary.
            //rb.velocity = Vector3.Lerp(rb.velocity, velocity, Time.deltaTime * lerpSpeed);
            //rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocity, Time.deltaTime * lerpSpeed);
        }
    }
}
