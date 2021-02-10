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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            //We own this player: send the others our data
            if (rb == null) {
                ResetRB();
            }
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.angularVelocity);

        } else {
            //Network player, receive data
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();
            velocity = (Vector3)stream.ReceiveNext();
            angularVelocity = (Vector3)stream.ReceiveNext();

            valuesReceived = true;

        }
    }


    void FixedUpdate()
    {
        if (!pv.IsMine && valuesReceived) {
            //Update Object position and Rigidbody parameters
            if (rb == null) {
                ResetRB();
            }

            Debug.Log("Sending data:   " + latestPos + "   "  + latestRot + "   " + velocity + "   " + angularVelocity);

            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, latestRot, Time.deltaTime * lerpSpeed);
            rb.velocity = Vector3.Lerp(rb.velocity, velocity, Time.deltaTime * lerpSpeed);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocity, Time.deltaTime * lerpSpeed);
        }
    }
}
